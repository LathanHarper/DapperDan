using System.Runtime.CompilerServices;
using CodeCrafty.DapperDan.Controls;

namespace CodeCrafty.DapperDan.PanelBossKit.Views;

/// <summary>
/// Attached-property boundary that gives DataTemplate and deferred tap views
/// normal Loaded/Unloaded registration without making TapViewBase depend on
/// PanelBoss or scanning the host's visual tree.
/// </summary>
internal static class TapViewRegistrationBridge
{
    private sealed class RegistrationState
    {
        public WeakReference<PanelBossBody_DefaultView> Host { get; set; }
    }

    private static readonly ConditionalWeakTable<TapViewBase, RegistrationState> _states = new();

    public static void OnFeatureChanged(TapViewBase tapView)
    {
        var state = _states.GetValue(tapView, HookLifecycle);
        RefreshRegistration(tapView, state);

        if (WantsPanelBossCoordination(tapView))
            return;

        tapView.Loaded -= OnTapViewLoaded;
        tapView.Unloaded -= OnTapViewUnloaded;
        _states.Remove(tapView);
    }

    private static RegistrationState HookLifecycle(TapViewBase tapView)
    {
        tapView.Loaded += OnTapViewLoaded;
        tapView.Unloaded += OnTapViewUnloaded;
        return new RegistrationState();
    }

    private static void OnTapViewLoaded(object sender, EventArgs e)
    {
        if (sender is TapViewBase tapView && _states.TryGetValue(tapView, out var state))
            RefreshRegistration(tapView, state);
    }

    private static void OnTapViewUnloaded(object sender, EventArgs e)
    {
        if (sender is not TapViewBase tapView || tapView.IsLoaded)
            return;

        if (!_states.TryGetValue(tapView, out var state) ||
            !TryGetHost(state, out var host))
        {
            return;
        }

        host.UnregisterLateTapViewRegistration(tapView);
        state.Host = null;
    }

    private static void RefreshRegistration(
        TapViewBase tapView,
        RegistrationState state)
    {
        var hadHost = TryGetHost(state, out var previousHost);
        var wantsCoordination = WantsPanelBossCoordination(tapView);
        var nextHost = tapView.IsLoaded && wantsCoordination
            ? FindHost(tapView)
            : null;

        if (hadHost && nextHost is null)
        {
            if (wantsCoordination)
                previousHost.UnregisterLateTapViewRegistration(tapView);
            else
                previousHost.UpdateLateTapViewRegistration(tapView);

            state.Host = null;
            return;
        }

        if (hadHost && !ReferenceEquals(previousHost, nextHost))
            previousHost.DetachRehostedTapViewRegistration(tapView);

        if (nextHost is null)
            return;

        nextHost.UpdateLateTapViewRegistration(tapView);
        state.Host = new WeakReference<PanelBossBody_DefaultView>(nextHost);
    }

    private static PanelBossBody_DefaultView FindHost(Element element)
    {
        Element current = element;

        while (current is not null)
        {
            if (current is PanelBossBody_DefaultView host)
                return host;

            current = current.Parent;
        }

        return null;
    }

    private static bool WantsPanelBossCoordination(TapViewBase tapView) =>
        PanelBoss.GetIWantRichState(tapView) ||
        PanelBoss.GetTouchPointBloom(tapView);

    private static bool TryGetHost(
        RegistrationState state,
        out PanelBossBody_DefaultView host)
    {
        host = null;
        return state.Host?.TryGetTarget(out host) == true;
    }
}
