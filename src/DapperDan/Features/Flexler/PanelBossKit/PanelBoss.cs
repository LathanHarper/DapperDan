using Flexler.PanelBossKit.Views;
using Prism.Mvvm;
using CodeCrafty.DapperDan.Diagnostics;

namespace Flexler.PanelBossKit;

public sealed class PanelBoss : BindableBase
{
    private readonly SemaphoreSlim _transitionGate = new(1, 1);
    private WeakReference<PanelBossBody_DefaultView>? _hostReference;
    private bool _useSidePanelLayout;

    public PanelBoss()
    {
        CrashJournal.Checkpoint(CrashPoint.FlexlerPanelBossCreated);
    }

    public event EventHandler<View>? PanelShown;

    public static readonly BindableProperty PanelNameProperty = BindableProperty.CreateAttached(
        "PanelName",
        typeof(string),
        typeof(PanelBoss),
        string.Empty);

    public static readonly BindableProperty PanelPriorityProperty = BindableProperty.CreateAttached(
        "PanelPriority",
        typeof(int),
        typeof(PanelBoss),
        0);

    public static readonly BindableProperty PanelIsVisibleProperty = BindableProperty.CreateAttached(
        "PanelIsVisible",
        typeof(bool),
        typeof(PanelBoss),
        true,
        propertyChanged: OnPanelIsVisibleChanged);

    public static readonly BindableProperty PanelTransitionInProperty = BindableProperty.CreateAttached(
        "PanelTransitionIn",
        typeof(PanelTransition),
        typeof(PanelBoss),
        PanelTransition.None);

    public static readonly BindableProperty PanelTransitionOutProperty = BindableProperty.CreateAttached(
        "PanelTransitionOut",
        typeof(PanelTransition),
        typeof(PanelBoss),
        PanelTransition.None);

    public static string GetPanelName(BindableObject bindable) =>
        (string)bindable.GetValue(PanelNameProperty);

    public static void SetPanelName(BindableObject bindable, string value) =>
        bindable.SetValue(PanelNameProperty, value);

    public static int GetPanelPriority(BindableObject bindable) =>
        (int)bindable.GetValue(PanelPriorityProperty);

    public static void SetPanelPriority(BindableObject bindable, int value) =>
        bindable.SetValue(PanelPriorityProperty, value);

    public static bool GetPanelIsVisible(BindableObject bindable) =>
        (bool)bindable.GetValue(PanelIsVisibleProperty);

    public static void SetPanelIsVisible(BindableObject bindable, bool value) =>
        bindable.SetValue(PanelIsVisibleProperty, value);

    public static PanelTransition GetPanelTransitionIn(BindableObject bindable) =>
        (PanelTransition)bindable.GetValue(PanelTransitionInProperty);

    public static void SetPanelTransitionIn(BindableObject bindable, PanelTransition value) =>
        bindable.SetValue(PanelTransitionInProperty, value);

    public static PanelTransition GetPanelTransitionOut(BindableObject bindable) =>
        (PanelTransition)bindable.GetValue(PanelTransitionOutProperty);

    public static void SetPanelTransitionOut(BindableObject bindable, PanelTransition value) =>
        bindable.SetValue(PanelTransitionOutProperty, value);

    public bool IsUsingSidePanelLayout => _useSidePanelLayout;

    public Task TopHeaderPanels_TogglePanelByName(string panelName) =>
        TogglePanelAsync(PanelLane.TopHeader, panelName);

    public Task TopHeaderPanels_DeActivatePanelByName(string panelName) =>
        DeactivatePanelAsync(PanelLane.TopHeader, panelName);

    public Task BottomInputPanels_ActivatePanelByName(string panelName) =>
        ActivatePanelAsync(PanelLane.BottomInput, panelName);

    public Task BottomInputPanels_DeActivatePanelByName(string panelName) =>
        DeactivatePanelAsync(PanelLane.BottomInput, panelName);

    public Task LeftSelectorPanels_TogglePanelByName(string panelName) =>
        TogglePanelAsync(PanelLane.LeftSelector, panelName);

    public Task LeftSelectorPanels_DeActivatePanelByName(string panelName) =>
        DeactivatePanelAsync(PanelLane.LeftSelector, panelName);

    public Task RightSelectorPanels_ActivatePanelByName(string panelName) =>
        ActivatePanelAsync(PanelLane.RightSelector, panelName);

    public Task RightSelectorPanels_DeActivatePanelByName(string panelName) =>
        DeactivatePanelAsync(PanelLane.RightSelector, panelName);

    public Task HeaderPanels_TogglePanelByName(string panelName) =>
        ToggleResponsivePanelAsync(ResponsivePanelChannel.Header, panelName);

    public Task HeaderPanels_DeActivatePanelByName(string panelName) =>
        DeactivateResponsivePanelAsync(ResponsivePanelChannel.Header, panelName);

    public Task InputPanels_ActivatePanelByName(string panelName) =>
        ActivateResponsivePanelAsync(ResponsivePanelChannel.Input, panelName);

    public Task InputPanels_DeActivatePanelByName(string panelName) =>
        DeactivateResponsivePanelAsync(ResponsivePanelChannel.Input, panelName);

    public async Task SetUseSidePanelLayoutAsync(bool useSidePanels)
    {
        await _transitionGate.WaitAsync();
        try
        {
            SetUseSidePanelLayout(useSidePanels);

            var host = GetHost();
            if (host is not null && host.UseSidePanelLayout != useSidePanels)
            {
                host.UseSidePanelLayout = useSidePanels;
            }
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    public async Task RestoreDefaultPanelChromeAsync()
    {
        await _transitionGate.WaitAsync();
        try
        {
            await RestoreDefaultAsync(GetPanels(GetResponsiveHeaderLane()));
            await RestoreDefaultAsync(GetPanels(GetResponsiveInputLane()));
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    internal void Attach(PanelBossBody_DefaultView host)
    {
        _hostReference = new WeakReference<PanelBossBody_DefaultView>(host);
    }

    internal void SetUseSidePanelLayout(bool useSidePanels)
    {
        if (_useSidePanelLayout == useSidePanels)
        {
            return;
        }

        var sourceHeaderLane = _useSidePanelLayout
            ? PanelLane.LeftSelector
            : PanelLane.TopHeader;
        var sourceInputLane = _useSidePanelLayout
            ? PanelLane.RightSelector
            : PanelLane.BottomInput;
        var targetHeaderLane = useSidePanels
            ? PanelLane.LeftSelector
            : PanelLane.TopHeader;
        var targetInputLane = useSidePanels
            ? PanelLane.RightSelector
            : PanelLane.BottomInput;

        MirrorActivePanel(GetPanels(sourceHeaderLane), GetPanels(targetHeaderLane));
        MirrorActivePanel(GetPanels(sourceInputLane), GetPanels(targetInputLane));
        _useSidePanelLayout = useSidePanels;
    }

    private static void OnPanelIsVisibleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not View view || newValue is not bool isVisible)
        {
            return;
        }

        view.IsVisible = isVisible;
        view.InputTransparent = !isVisible;
    }

    private async Task TogglePanelAsync(PanelLane lane, string panelName)
    {
        await _transitionGate.WaitAsync();
        try
        {
            await ToggleNamedPanelCoreAsync(lane, panelName);
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    private async Task ToggleResponsivePanelAsync(
        ResponsivePanelChannel channel,
        string panelName)
    {
        await _transitionGate.WaitAsync();
        try
        {
            await ToggleNamedPanelCoreAsync(GetResponsiveLane(channel), panelName);
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    private async Task ActivatePanelAsync(PanelLane lane, string panelName)
    {
        await _transitionGate.WaitAsync();
        try
        {
            await ActivateNamedPanelCoreAsync(lane, panelName);
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    private async Task ActivateResponsivePanelAsync(
        ResponsivePanelChannel channel,
        string panelName)
    {
        await _transitionGate.WaitAsync();
        try
        {
            await ActivateNamedPanelCoreAsync(GetResponsiveLane(channel), panelName);
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    private async Task DeactivatePanelAsync(PanelLane lane, string panelName)
    {
        await _transitionGate.WaitAsync();
        try
        {
            await DeactivateNamedPanelCoreAsync(lane, panelName);
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    private async Task DeactivateResponsivePanelAsync(
        ResponsivePanelChannel channel,
        string panelName)
    {
        await _transitionGate.WaitAsync();
        try
        {
            await DeactivateNamedPanelCoreAsync(GetResponsiveLane(channel), panelName);
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    private async Task ToggleNamedPanelCoreAsync(PanelLane lane, string panelName)
    {
        var panels = GetPanels(lane);
        var target = FindPanel(panels, panelName);
        if (target is null)
        {
            return;
        }

        if (GetPanelIsVisible(target))
        {
            await DeactivateCoreAsync(panels, target);
        }
        else
        {
            await ActivateCoreAsync(panels, target);
            PanelShown?.Invoke(this, target);
        }
    }

    private async Task ActivateNamedPanelCoreAsync(PanelLane lane, string panelName)
    {
        var panels = GetPanels(lane);
        var target = FindPanel(panels, panelName);
        if (target is not null)
        {
            await ActivateCoreAsync(panels, target);
            PanelShown?.Invoke(this, target);
        }
    }

    private async Task DeactivateNamedPanelCoreAsync(PanelLane lane, string panelName)
    {
        var panels = GetPanels(lane);
        var target = FindPanel(panels, panelName);
        if (target is not null)
        {
            await DeactivateCoreAsync(panels, target);
        }
    }

    private static async Task ActivateCoreAsync(IReadOnlyList<View> panels, View target)
    {
        var visiblePanels = panels
            .Where(panel => panel != target && GetPanelIsVisible(panel))
            .ToArray();

        await Task.WhenAll(visiblePanels.Select(HideAsync));
        await ShowAsync(target);
    }

    private static async Task DeactivateCoreAsync(IReadOnlyList<View> panels, View target)
    {
        if (!GetPanelIsVisible(target))
        {
            return;
        }

        await HideAsync(target);

        var defaultPanel = panels
            .Where(panel => panel != target)
            .OrderByDescending(GetPanelPriority)
            .FirstOrDefault();

        if (defaultPanel is not null)
        {
            await ActivateCoreAsync(panels, defaultPanel);
        }
    }

    private static async Task RestoreDefaultAsync(IReadOnlyList<View> panels)
    {
        var defaultPanel = panels
            .OrderByDescending(GetPanelPriority)
            .FirstOrDefault();

        if (defaultPanel is null)
        {
            return;
        }

        await ActivateCoreAsync(panels, defaultPanel);
    }

    private IReadOnlyList<View> GetPanels(PanelLane lane)
    {
        var host = GetHost();
        if (host is null)
        {
            return [];
        }

        return lane switch
        {
            PanelLane.TopHeader => host.TopHeaderPanels,
            PanelLane.BottomInput => host.BottomInputPanels,
            PanelLane.LeftSelector => host.LeftSelectorPanels,
            PanelLane.RightSelector => host.RightSelectorPanels,
            _ => []
        };
    }

    private PanelLane GetResponsiveHeaderLane() =>
        _useSidePanelLayout
            ? PanelLane.LeftSelector
            : PanelLane.TopHeader;

    private PanelLane GetResponsiveInputLane() =>
        _useSidePanelLayout
            ? PanelLane.RightSelector
            : PanelLane.BottomInput;

    private PanelLane GetResponsiveLane(ResponsivePanelChannel channel) =>
        channel == ResponsivePanelChannel.Header
            ? GetResponsiveHeaderLane()
            : GetResponsiveInputLane();

    private PanelBossBody_DefaultView? GetHost()
    {
        if (_hostReference is not null && _hostReference.TryGetTarget(out var host))
        {
            return host;
        }

        return null;
    }

    private static View? FindPanel(IEnumerable<View> panels, string panelName) =>
        panels.SingleOrDefault(
            panel => string.Equals(GetPanelName(panel), panelName, StringComparison.Ordinal));

    private static void MirrorActivePanel(
        IReadOnlyList<View> sourcePanels,
        IReadOnlyList<View> targetPanels)
    {
        if (targetPanels.Count == 0)
        {
            return;
        }

        var activePanelName = sourcePanels
            .Where(GetPanelIsVisible)
            .OrderByDescending(GetPanelPriority)
            .Select(GetPanelName)
            .FirstOrDefault();
        var targetPanel = targetPanels.FirstOrDefault(
                panel => string.Equals(
                    GetPanelName(panel),
                    activePanelName,
                    StringComparison.Ordinal))
            ?? targetPanels.OrderByDescending(GetPanelPriority).First();

        foreach (var panel in targetPanels)
        {
            panel.Opacity = 1;
            panel.TranslationX = 0;
            panel.TranslationY = 0;
            SetPanelIsVisible(panel, ReferenceEquals(panel, targetPanel));
        }
    }

    private static async Task ShowAsync(View view)
    {
        if (GetPanelIsVisible(view))
        {
            return;
        }

        var transition = GetPanelTransitionIn(view);
        PrepareForEntrance(view, transition);
        SetPanelIsVisible(view, true);

        switch (transition)
        {
            case PanelTransition.FadeIn:
                await view.FadeToAsync(1, 180, Easing.CubicOut);
                break;
            case PanelTransition.SlideInDownward:
            case PanelTransition.SlideInUpward:
            case PanelTransition.SlideInRightward:
            case PanelTransition.SlideInLeftward:
                await view.TranslateToAsync(0, 0, 240, Easing.CubicOut);
                break;
        }

        view.Opacity = 1;
        view.TranslationX = 0;
        view.TranslationY = 0;
    }

    private static async Task HideAsync(View view)
    {
        if (!GetPanelIsVisible(view))
        {
            return;
        }

        switch (GetPanelTransitionOut(view))
        {
            case PanelTransition.FadeOut:
                await view.FadeToAsync(0, 150, Easing.CubicIn);
                break;
            case PanelTransition.SlideOutUpward:
                await view.TranslateToAsync(0, -GetVerticalTravelDistance(view), 190, Easing.CubicIn);
                break;
            case PanelTransition.SlideOutDownward:
                await view.TranslateToAsync(0, GetVerticalTravelDistance(view), 190, Easing.CubicIn);
                break;
            case PanelTransition.SlideOutLeftward:
                await view.TranslateToAsync(-GetHorizontalTravelDistance(view), 0, 190, Easing.CubicIn);
                break;
            case PanelTransition.SlideOutRightward:
                await view.TranslateToAsync(GetHorizontalTravelDistance(view), 0, 190, Easing.CubicIn);
                break;
        }

        SetPanelIsVisible(view, false);
        view.Opacity = 1;
        view.TranslationX = 0;
        view.TranslationY = 0;
    }

    private static void PrepareForEntrance(View view, PanelTransition transition)
    {
        view.Opacity = transition == PanelTransition.FadeIn ? 0 : 1;
        view.TranslationY = transition switch
        {
            PanelTransition.SlideInDownward => -GetVerticalTravelDistance(view),
            PanelTransition.SlideInUpward => GetVerticalTravelDistance(view),
            _ => 0
        };
        view.TranslationX = transition switch
        {
            PanelTransition.SlideInRightward => -GetHorizontalTravelDistance(view),
            PanelTransition.SlideInLeftward => GetHorizontalTravelDistance(view),
            _ => 0
        };
    }

    private static double GetVerticalTravelDistance(View view)
    {
        if (view.Height > 0)
        {
            return view.Height + 24;
        }

        if (view.HeightRequest > 0)
        {
            return view.HeightRequest + 24;
        }

        return 320;
    }

    private static double GetHorizontalTravelDistance(View view)
    {
        if (view.Width > 0)
        {
            return view.Width + 24;
        }

        if (view.WidthRequest > 0)
        {
            return view.WidthRequest + 24;
        }

        return 320;
    }

    private enum PanelLane
    {
        TopHeader,
        BottomInput,
        LeftSelector,
        RightSelector
    }

    private enum ResponsivePanelChannel
    {
        Header,
        Input
    }
}
