using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Input;
using WinButton = Microsoft.UI.Xaml.Controls.Button;

namespace Flexler.Controls;

/// <summary>
/// A single native button owns focus, keyboard activation, pointer capture and
/// accessibility Invoke. Its content is the actual owner-composed MAUI tree.
/// ContentPanel layout wiring follows MAUI 10.0.20, commit
/// 0d1705adc4a6b4ec531e316ec956755abbe059c5, ContentViewHandler.Windows.cs.
/// </summary>
public sealed class NativePrimaryTapViewHandler : ViewHandler<RichButton, NativePrimaryTapPlatformView>
{
    public static readonly IPropertyMapper<RichButton, NativePrimaryTapViewHandler> RichButtonMapper =
        new PropertyMapper<RichButton, NativePrimaryTapViewHandler>(ViewMapper)
        {
            [nameof(RichButton.Content)] = MapContent,
            [nameof(RichButton.Padding)] = MapContent,
            [nameof(RichButton.IsEnabled)] = MapAvailability,
            [nameof(RichButton.IsBusy)] = MapAvailability,
            [nameof(RichButton.IsCommandInvocationActive)] = MapAvailability,
        };

    public NativePrimaryTapViewHandler() : base(RichButtonMapper)
    {
    }

    protected override NativePrimaryTapPlatformView CreatePlatformView() => new()
    {
        ContentPanel = { CrossPlatformLayout = VirtualView },
    };

    public override void SetVirtualView(IView view)
    {
        base.SetVirtualView(view);
        PlatformView.ContentPanel.CrossPlatformLayout = VirtualView;
    }

    protected override void DisconnectHandler(NativePrimaryTapPlatformView platformView)
    {
        platformView.ContentPanel.CrossPlatformLayout = null;
        platformView.ContentPanel.Children.Clear();
        base.DisconnectHandler(platformView);
    }

    private static void MapContent(NativePrimaryTapViewHandler handler, RichButton view)
    {
        var panel = handler.PlatformView.ContentPanel;
        panel.Children.Clear();
        if (((IContentView)view).PresentedContent is IView content && handler.MauiContext is { } context)
            panel.Children.Add(content.ToPlatform(context));

        panel.InvalidateMeasure();
    }

    private static void MapAvailability(NativePrimaryTapViewHandler handler, RichButton view) =>
        handler.PlatformView.ApplyAvailability(view);
}

public sealed class NativePrimaryTapPlatformView : WinButton
{
    internal ContentPanel ContentPanel { get; } = new();
    internal Action<PointerRoutedEventArgs>? NativePointerPressed { get; set; }
    internal Action? NativePointerCompleted { get; set; }
    internal Action? NativePointerCancelled { get; set; }
    internal Action<KeyRoutedEventArgs>? NativeKeyPressed { get; set; }
    internal Action<KeyRoutedEventArgs>? NativeKeyCompleted { get; set; }
    private bool _releasingPointer;
    private bool _automationAvailable;
    internal TapViewBase? Source { get; private set; }

    public NativePrimaryTapPlatformView()
    {
        IsTabStop = true;
        UseSystemFocusVisuals = true;
        Padding = new Microsoft.UI.Xaml.Thickness(0);
        BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        MinWidth = 0;
        MinHeight = 0;
        HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
        VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
        HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
        VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;

        // This is the button's real content presenter, never a second action
        // overlay. All appearance and busy chrome remain in the owner's XAML.
        Template = (Microsoft.UI.Xaml.Controls.ControlTemplate)XamlReader.Load("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="Button">
                <ContentPresenter Content="{TemplateBinding Content}"
                    HorizontalContentAlignment="Stretch" VerticalContentAlignment="Stretch"
                    Background="Transparent" Padding="0" BorderThickness="0" />
            </ControlTemplate>
            """);
        Content = ContentPanel;
    }

    // The standard Button peer supplies Invoke, Button control type and focus
    // semantics, and invokes the very same native Click as pointer/keyboard.
    protected override AutomationPeer OnCreateAutomationPeer() => new NativePrimaryTapAutomationPeer(this);

    internal void ApplyAvailability(TapViewBase source)
    {
        Source = source;
        var available = source.IsEnabled && !source.InputTransparent;
        // Temporarily disabling a focused WinUI Button moves focus to another
        // action. Keep the accepted invocation's native host alive; its command
        // guard and automation peer still reject every unavailable activation.
        var retainDuringInvocation = IsEnabled &&
            (source.IsBusy || source.IsCommandInvocationActive);
        IsEnabled = available || retainDuringInvocation;

        if (_automationAvailable == available)
            return;
        var previous = _automationAvailable;
        _automationAvailable = available;
        FrameworkElementAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(
            AutomationElementIdentifiers.IsEnabledProperty, previous, available);
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs args)
    {
        NativePointerPressed?.Invoke(args);
        base.OnPointerPressed(args);
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs args)
    {
        // The native Button may release pointer capture before raising Click.
        // Preserve its down snapshot across that internal capture transition.
        _releasingPointer = true;
        try
        {
            base.OnPointerReleased(args);
        }
        finally
        {
            _releasingPointer = false;
            NativePointerCompleted?.Invoke();
        }
    }

    protected override void OnPointerCanceled(PointerRoutedEventArgs args)
    {
        NativePointerCancelled?.Invoke();
        base.OnPointerCanceled(args);
    }

    protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
    {
        if (!_releasingPointer)
            NativePointerCancelled?.Invoke();
        base.OnPointerCaptureLost(args);
    }

    protected override void OnKeyDown(KeyRoutedEventArgs args)
    {
        NativeKeyPressed?.Invoke(args);
        base.OnKeyDown(args);
    }

    protected override void OnKeyUp(KeyRoutedEventArgs args)
    {
        base.OnKeyUp(args);
        NativeKeyCompleted?.Invoke(args);
    }
}

internal sealed class NativePrimaryTapAutomationPeer : ButtonAutomationPeer, IInvokeProvider
{
    private readonly NativePrimaryTapPlatformView _button;

    public NativePrimaryTapAutomationPeer(NativePrimaryTapPlatformView button) : base(button) =>
        _button = button;

    protected override bool IsEnabledCore() => base.IsEnabledCore() &&
        _button.Source is { IsEnabled: true, InputTransparent: false, IsBusy: false };

    protected override object GetPatternCore(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.Invoke ? this : base.GetPatternCore(patternInterface);

    void IInvokeProvider.Invoke()
    {
        if (!IsEnabled())
            throw new ElementNotEnabledException();
        base.Invoke();
    }
}
