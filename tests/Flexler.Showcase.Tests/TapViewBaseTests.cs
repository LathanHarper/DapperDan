using System.Windows.Input;
using Flexler.Controls;
using Microsoft.Maui.Dispatching;
using Prism.Commands;

namespace Flexler.Tests;

public sealed class TapViewBaseTests
{
    public TapViewBaseTests() => DispatcherProvider.SetCurrent(new ImmediateDispatcherProvider());

    [Fact]
    public async Task NativeDownSnapshotsCommandAndParameter()
    {
        var first = new RecordingCommand();
        var second = new RecordingCommand();
        var button = new Harness { Command = first, CommandParameter = "original" };
        Assert.True(button.Down());
        button.Command = second;
        button.CommandParameter = "replacement";
        await button.UpAsync();
        Assert.Equal("original", Assert.Single(first.Parameters));
        Assert.Empty(second.Parameters);
    }

    [Fact]
    public async Task NativeActivationSetsBusyBeforeCommandAndRejectsRepeats()
    {
        var button = new Harness();
        var sawBusy = false;
        var command = new RecordingCommand(() => sawBusy = button.IsBusy);
        button.Command = command;
        Assert.True(button.Down());
        await button.UpAsync();
        Assert.True(sawBusy);
        Assert.True(button.IsBusy);
        Assert.False(button.IsEnabled);
        Assert.False(button.Down());
        await button.UpAsync();
        Assert.Single(command.Parameters);
        button.IsBusy = false;
        Assert.True(button.IsEnabled);
        Assert.Equal(TapViewBase.WaitingForTouchState, button.RichVisualState);
    }

    [Fact]
    public void CanExecuteRefreshPreservesExplicitEnabledAndTransparentValues()
    {
        var command = new RecordingCommand();
        var button = new Harness { Command = command };
        Assert.True(button.IsEnabled);
        command.Available = false;
        command.RaiseAvailability();
        Assert.False(button.IsEnabled);
        Assert.False(button.Down());
        command.Available = true;
        command.RaiseAvailability();
        Assert.True(button.IsEnabled);
        button.IsEnabled = false;
        command.RaiseAvailability();
        Assert.False(button.IsEnabled);
        button.IsEnabled = true;
        button.InputTransparent = true;
        button.IsBusy = true;
        button.IsBusy = false;
        Assert.True(button.InputTransparent);
        Assert.False(button.Down());
    }

    [Fact]
    public async Task CancelledTouchDoesNotInvokeOrOwnBusy()
    {
        var command = new RecordingCommand();
        var button = new Harness { Command = command };
        button.TapStarting += (_, args) => args.Cancel = true;
        Assert.False(button.Down());
        await button.UpAsync();
        Assert.Empty(command.Parameters);
        Assert.False(button.IsBusy);
    }

    [Fact]
    public async Task NativeCancellationCannotBeRestartedByLateRelease()
    {
        var command = new RecordingCommand();
        var button = new Harness { Command = command };
        Assert.True(button.Down());
        button.Cancel();
        await button.UpAsync();
        Assert.Empty(command.Parameters);
        Assert.False(button.IsBusy);
        Assert.True(button.Down());
        await button.UpAsync();
        Assert.Single(command.Parameters);
    }

    [Fact]
    public async Task ExplicitDebounceReturnsToAvailableAfterItsDeadline()
    {
        var button = new Harness { Command = new RecordingCommand(), AutoResetIsBusyMilliseconds = 30 };
        var reset = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        button.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(button.IsBusy) && !button.IsBusy)
                reset.TrySetResult();
        };
        Assert.True(button.Down());
        var activation = button.UpAsync();
        Assert.True(button.IsBusy);
        await activation;
        await reset.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(button.IsBusy);
        Assert.True(button.IsEnabled);
        Assert.False(button.IsTapping);
    }

    [Fact]
    public async Task RejectedCommandDoesNotInvokeOrOwnBusy()
    {
        var command = new RecordingCommand();
        var button = new Harness { Command = command };
        Assert.True(button.Down());
        command.Available = false;
        await button.UpAsync();
        Assert.Empty(command.Parameters);
        Assert.False(button.IsBusy);
    }

    [Fact]
    public async Task CommandFaultClearsBusyAndRestoresAvailability()
    {
        var button = new Harness { Command = new RecordingCommand(() => throw new InvalidOperationException("fault")) };
        Assert.True(button.Down());
        await button.UpAsync();
        Assert.False(button.IsBusy);
        Assert.False(button.IsTapping);
        Assert.True(button.IsEnabled);
    }

    [Fact]
    public async Task FeedbackFaultDoesNotPreventCommand()
    {
        var command = new RecordingCommand();
        var button = new Harness { Command = command };
        button.FeedbackRequested += (_, _) => throw new InvalidOperationException("feedback unavailable");
        Assert.True(button.Down());
        await button.UpAsync();
        Assert.Single(command.Parameters);
    }

    [Fact]
    public async Task TouchedCancellationLeavesCommandUntouched()
    {
        var command = new RecordingCommand();
        var button = new Harness { Command = command };
        button.Touched += (_, args) => args.Cancel = true;
        Assert.True(button.Down());
        await button.UpAsync();
        Assert.Empty(command.Parameters);
        Assert.False(button.IsBusy);
    }

    [Fact]
    public async Task AvailabilityChangedByTouchObserverRejectsCommand()
    {
        var command = new RecordingCommand();
        var button = new Harness { Command = command };
        button.Touched += (_, _) => button.InputTransparent = true;
        Assert.True(button.Down());
        await button.UpAsync();
        Assert.Empty(command.Parameters);
        Assert.False(button.IsBusy);
    }

    [Fact]
    public async Task InvocationLifetimeIncludesPrismUnwindAfterWorkflowClearsBusy()
    {
        var button = new Harness();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        button.Command = new AsyncDelegateCommand(async () =>
        {
            button.IsBusy = false;
            entered.SetResult();
            await finish.Task;
        });

        Assert.True(button.Down());
        var activation = button.UpAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(button.IsBusy);
        Assert.True(button.IsCommandInvocationActive);
        Assert.False(button.IsCommandAvailable);
        finish.SetResult();
        await activation;
        Assert.False(button.IsCommandInvocationActive);
        Assert.True(button.IsCommandAvailable);
        Assert.True(button.IsEnabled);
    }

    private sealed class Harness : TapViewBase
    {
        public Harness() => FeedbackMode = RichButtonFeedbackMode.None;
        public bool Down() => BeginNativeTouchSequence();
        public void Cancel() => CancelNativeTouchSequence();
        public Task UpAsync() => ActivateNativeTouchSequenceAsync();
        protected override void ApplyRichVisualStateCore(string state) { }
    }

    private sealed class RecordingCommand(Action? action = null) : ICommand
    {
        public bool Available { get; set; } = true;
        public List<object?> Parameters { get; } = new();
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Available;
        public void Execute(object? parameter) { Parameters.Add(parameter); action?.Invoke(); }
        public void RaiseAvailability() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class ImmediateDispatcherProvider : IDispatcherProvider
    {
        public IDispatcher GetForCurrentThread() => new ImmediateDispatcher();
    }

    private sealed class ImmediateDispatcher : IDispatcher
    {
        public bool IsDispatchRequired => false;
        public bool Dispatch(Action action) { action(); return true; }
        public bool DispatchDelayed(TimeSpan delay, Action action) => throw new NotSupportedException();
        public IDispatcherTimer CreateTimer() => throw new NotSupportedException();
    }
}
