using Microsoft.Maui.Graphics;
using Prism.Commands;
using Prism.Mvvm;
using CodeCrafty.DapperDan.PanelBossKit;

namespace CodeCrafty.DapperDan.ViewModels;

public sealed class RotationCanaryViewModel : BindableBase
{
    private readonly AsyncDelegateCommand _runCrossFadeCommand;
    private readonly DelegateCommand<string> _setPresetCommand;
    private CancellationTokenSource? _crossFadeCancellationTokenSource;
    private bool _isCrossFadeBusy;
    private bool _isExplicitZOrder;
    private bool _isHostOpaque;
    private bool _isInnerClipped;
    private bool _isStackedLayers;
    private bool _isUnderlayVisible;
    private double _backOpacity;
    private double _frontOpacity = 1;
    private double _rotation;
    private double _rotationX;
    private double _rotationY;
    private string _status =
        "Start flat. Sweep one slider at a time and watch all four colored edges.";

    public RotationCanaryViewModel(PanelBoss activePanelBoss)
    {
        ActivePanelBoss = activePanelBoss;
        _runCrossFadeCommand = new AsyncDelegateCommand(RunCrossFadeAsync);
        _setPresetCommand = new DelegateCommand<string>(SetPreset);
    }

    public PanelBoss ActivePanelBoss { get; }

    public double BackOpacity
    {
        get => _backOpacity;
        private set => SetProperty(ref _backOpacity, value);
    }

    public double FrontOpacity
    {
        get => _frontOpacity;
        private set => SetProperty(ref _frontOpacity, value);
    }

    public Color HostBackgroundColor =>
        IsHostOpaque ? Color.FromArgb("#173E52") : Colors.Transparent;

    public bool IsCrossFadeBusy
    {
        get => _isCrossFadeBusy;
        set => SetProperty(ref _isCrossFadeBusy, value);
    }

    public bool IsExplicitZOrder
    {
        get => _isExplicitZOrder;
        set
        {
            if (SetProperty(ref _isExplicitZOrder, value))
            {
                RaisePropertyChanged(nameof(TargetZIndex));
            }
        }
    }

    public bool IsHostOpaque
    {
        get => _isHostOpaque;
        set
        {
            if (SetProperty(ref _isHostOpaque, value))
            {
                RaisePropertyChanged(nameof(HostBackgroundColor));
            }
        }
    }

    public bool IsInnerClipped
    {
        get => _isInnerClipped;
        set => SetProperty(ref _isInnerClipped, value);
    }

    public bool IsStackedLayers
    {
        get => _isStackedLayers;
        set => SetProperty(ref _isStackedLayers, value);
    }

    public bool IsUnderlayVisible
    {
        get => _isUnderlayVisible;
        set => SetProperty(ref _isUnderlayVisible, value);
    }

    public double Rotation
    {
        get => _rotation;
        set => SetProperty(ref _rotation, value);
    }

    public double RotationX
    {
        get => _rotationX;
        set => SetProperty(ref _rotationX, value);
    }

    public double RotationY
    {
        get => _rotationY;
        set => SetProperty(ref _rotationY, value);
    }

    public AsyncDelegateCommand RunCrossFadeCommand => _runCrossFadeCommand;

    public DelegateCommand<string> SetPresetCommand => _setPresetCommand;

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public int TargetZIndex => IsExplicitZOrder ? 1 : 0;

    public void CancelCrossFade() => _crossFadeCancellationTokenSource?.Cancel();

    private async Task RunCrossFadeAsync()
    {
        if (IsCrossFadeBusy)
        {
            return;
        }

        IsStackedLayers = true;
        IsCrossFadeBusy = true;
        Status = "Running 30 native opacity cross-fades…";
        using var cancellationTokenSource = new CancellationTokenSource();
        _crossFadeCancellationTokenSource = cancellationTokenSource;

        try
        {
            const int cycles = 30;
            const int stepsPerHalf = 5;

            for (var cycle = 0; cycle < cycles; cycle++)
            {
                for (var step = 1; step <= stepsPerHalf; step++)
                {
                    var progress = step / (double)stepsPerHalf;
                    FrontOpacity = 1 - progress;
                    BackOpacity = progress;
                    await Task.Delay(25, cancellationTokenSource.Token);
                }

                for (var step = 1; step <= stepsPerHalf; step++)
                {
                    var progress = step / (double)stepsPerHalf;
                    FrontOpacity = progress;
                    BackOpacity = 1 - progress;
                    await Task.Delay(25, cancellationTokenSource.Token);
                }
            }

            Status =
                "30 cycles complete. All red, green, blue, and amber edges should still be visible.";
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            Status = "Cross-fade stopped when the rotation page closed.";
        }
        finally
        {
            if (ReferenceEquals(_crossFadeCancellationTokenSource, cancellationTokenSource))
            {
                _crossFadeCancellationTokenSource = null;
            }

            FrontOpacity = 1;
            BackOpacity = 0;
            IsCrossFadeBusy = false;
        }
    }

    private void SetPreset(string? preset)
    {
        switch (preset)
        {
            case "Negative":
                Rotation = -0.4;
                RotationX = 1.2;
                RotationY = -3.5;
                Status = "Small negative-Y combination loaded.";
                break;
            case "Positive":
                Rotation = 1.2;
                RotationX = -1.4;
                RotationY = 4.5;
                Status = "Small positive-Y combination loaded.";
                break;
            default:
                Rotation = 0;
                RotationX = 0;
                RotationY = 0;
                IsHostOpaque = false;
                IsInnerClipped = false;
                IsExplicitZOrder = false;
                IsStackedLayers = false;
                IsUnderlayVisible = false;
                FrontOpacity = 1;
                BackOpacity = 0;
                Status =
                    "Flat control restored. Sweep one slider at a time before adding reef switches.";
                break;
        }
    }
}
