using CodeCrafty.DapperDan.Controls;

namespace CodeCrafty.DapperDan.PanelBossKit.Views;

public partial class PanelBossBody_DefaultView
{
    [Flags]
    private enum TapViewFeatures
    {
        None = 0,
        RichState = 1,
        TouchPointBloom = 2,
    }

    private enum TapViewRegistrationSource
    {
        StaticRoster,
        LateLifecycle,
        LegacyScan,
    }

    private sealed class TapViewRegistrationEntry
    {
        public TapViewFeatures StaticRoster { get; set; }
        public TapViewFeatures LateLifecycle { get; set; }
        public TapViewFeatures LegacyScan { get; set; }
        public TapViewFeatures Applied { get; set; }

        public TapViewFeatures Effective =>
            StaticRoster | LateLifecycle | LegacyScan;

        public TapViewFeatures Get(TapViewRegistrationSource source) =>
            source switch
            {
                TapViewRegistrationSource.StaticRoster => StaticRoster,
                TapViewRegistrationSource.LateLifecycle => LateLifecycle,
                _ => LegacyScan,
            };

        public void Set(TapViewRegistrationSource source, TapViewFeatures features)
        {
            switch (source)
            {
                case TapViewRegistrationSource.StaticRoster:
                    StaticRoster = features;
                    break;
                case TapViewRegistrationSource.LateLifecycle:
                    LateLifecycle = features;
                    break;
                default:
                    LegacyScan = features;
                    break;
            }
        }
    }

    private readonly Dictionary<TapViewBase, TapViewRegistrationEntry> _tapViewRegistrations =
        new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<TapViewBase> _staticTapViewRoster =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Declares the page's stable, named tap surfaces after InitializeComponent.
    /// Feature ownership still comes from PanelBoss attached properties.
    /// Calling this method again replaces the host's previous static roster.
    /// </summary>
    public void SetStaticTapViewRoster(params TapViewBase[] tapViews)
    {
        var nextRoster = new HashSet<TapViewBase>(ReferenceEqualityComparer.Instance);

        if (tapViews is not null)
        {
            foreach (var tapView in tapViews)
            {
                if (tapView is not null)
                    nextRoster.Add(tapView);
            }
        }

        foreach (var tapView in _staticTapViewRoster)
        {
            if (!nextRoster.Contains(tapView))
            {
                SetTapViewSourceFeatures(
                    tapView,
                    TapViewRegistrationSource.StaticRoster,
                    TapViewFeatures.None);
            }
        }

        _staticTapViewRoster.Clear();

        foreach (var tapView in nextRoster)
        {
            _staticTapViewRoster.Add(tapView);
            SetTapViewSourceFeatures(
                tapView,
                TapViewRegistrationSource.StaticRoster,
                GetRequestedTapViewFeatures(tapView));
        }

        Console.WriteLine(
            $"TAP_VIEW_REGISTRY|static-roster|count={_staticTapViewRoster.Count}|page={FindHostPage()?.GetType().Name}");
    }

    internal void UpdateLateTapViewRegistration(TapViewBase tapView)
    {
        var requestedFeatures = GetRequestedTapViewFeatures(tapView);

        if (_staticTapViewRoster.Contains(tapView))
        {
            SetTapViewSourceFeatures(
                tapView,
                TapViewRegistrationSource.StaticRoster,
                requestedFeatures);
        }

        SetTapViewSourceFeatures(
            tapView,
            TapViewRegistrationSource.LateLifecycle,
            requestedFeatures);

        if (_tapViewRegistrations.TryGetValue(tapView, out var entry))
        {
            SetTapViewSourceFeatures(
                tapView,
                TapViewRegistrationSource.LegacyScan,
                entry.LegacyScan & requestedFeatures);
        }
    }

    internal void UnregisterLateTapViewRegistration(TapViewBase tapView)
    {
        SetTapViewSourceFeatures(
            tapView,
            TapViewRegistrationSource.LateLifecycle,
            TapViewFeatures.None);

        if (_staticTapViewRoster.Contains(tapView))
        {
            SetTapViewSourceFeatures(
                tapView,
                TapViewRegistrationSource.StaticRoster,
                GetRequestedTapViewFeatures(tapView));
        }
    }

    internal void DetachRehostedTapViewRegistration(TapViewBase tapView)
    {
        SetTapViewSourceFeatures(
            tapView,
            TapViewRegistrationSource.LateLifecycle,
            TapViewFeatures.None);
        SetTapViewSourceFeatures(
            tapView,
            TapViewRegistrationSource.LegacyScan,
            TapViewFeatures.None);
    }

    private void ReconcileTrackedTapViewRegistrations()
    {
        foreach (var registration in _tapViewRegistrations.ToArray())
            ReconcileTapViewRegistration(registration.Key, registration.Value);
    }

    private void RefreshStaticTapViewRosterFeatures()
    {
        foreach (var tapView in _staticTapViewRoster)
        {
            SetTapViewSourceFeatures(
                tapView,
                TapViewRegistrationSource.StaticRoster,
                GetRequestedTapViewFeatures(tapView));
        }
    }

    private void RecordLegacyTapViewRegistration(
        TapViewBase tapView,
        TapViewFeatures feature)
    {
        var entry = GetOrCreateTapViewRegistration(tapView, out var wasCreated);
        var previousFeatures = entry.LegacyScan;
        var nextFeatures = previousFeatures | feature;

        entry.LegacyScan = nextFeatures;
        ReportTapViewRegistrationChange(
            tapView,
            TapViewRegistrationSource.LegacyScan,
            wasCreated,
            previousFeatures,
            nextFeatures,
            entry);
    }

    private void MarkLegacyTapViewFeatureApplied(
        TapViewBase tapView,
        TapViewFeatures feature)
    {
        if (_tapViewRegistrations.TryGetValue(tapView, out var entry))
            entry.Applied |= feature;
    }

    private void ForgetLegacyTapViewFeature(
        TapViewBase tapView,
        TapViewFeatures feature)
    {
        if (!_tapViewRegistrations.TryGetValue(tapView, out var entry))
            return;

        entry.Applied &= ~feature;
        SetTapViewSourceFeatures(
            tapView,
            TapViewRegistrationSource.LegacyScan,
            entry.LegacyScan & ~feature);
    }

    private void ResetAppliedTapViewFeatures()
    {
        foreach (var entry in _tapViewRegistrations.Values)
            entry.Applied = TapViewFeatures.None;
    }

    private void ClearLegacyTapViewSources()
    {
        foreach (var registration in _tapViewRegistrations.ToArray())
        {
            registration.Value.LegacyScan = TapViewFeatures.None;
            RemoveEmptyTapViewRegistration(registration.Key, registration.Value);
        }
    }

    private void SetTapViewSourceFeatures(
        TapViewBase tapView,
        TapViewRegistrationSource source,
        TapViewFeatures features)
    {
        if (features == TapViewFeatures.None &&
            !_tapViewRegistrations.ContainsKey(tapView))
        {
            return;
        }

        var entry = GetOrCreateTapViewRegistration(tapView, out var wasCreated);
        var previousFeatures = entry.Get(source);

        if (previousFeatures == features)
        {
            ReportTapViewRegistrationChange(
                tapView,
                source,
                wasCreated,
                previousFeatures,
                features,
                entry);
            ReconcileTapViewRegistration(tapView, entry);
            RemoveEmptyTapViewRegistration(tapView, entry);
            return;
        }

        entry.Set(source, features);
        ReportTapViewRegistrationChange(
            tapView,
            source,
            wasCreated,
            previousFeatures,
            features,
            entry);
        ReconcileTapViewRegistration(tapView, entry);
        RemoveEmptyTapViewRegistration(tapView, entry);
    }

    private TapViewRegistrationEntry GetOrCreateTapViewRegistration(
        TapViewBase tapView,
        out bool wasCreated)
    {
        if (_tapViewRegistrations.TryGetValue(tapView, out var entry))
        {
            wasCreated = false;
            return entry;
        }

        entry = new TapViewRegistrationEntry();
        _tapViewRegistrations.Add(tapView, entry);
        wasCreated = true;
        return entry;
    }

    private void ReconcileTapViewRegistration(
        TapViewBase tapView,
        TapViewRegistrationEntry entry)
    {
        var requestedFeatures =
            _richButtonCoordinationEnabled &&
            IsLoaded &&
            tapView.IsLoaded &&
            HostsTapView(tapView)
                ? entry.Effective
                : TapViewFeatures.None;
        var removedFeatures = entry.Applied & ~requestedFeatures;
        var addedFeatures = requestedFeatures & ~entry.Applied;

        if ((removedFeatures & TapViewFeatures.TouchPointBloom) != 0)
            RemoveTrackedTouchPointBloom(tapView);

        if ((removedFeatures & TapViewFeatures.RichState) != 0)
            RemoveTrackedRichState(tapView);

        if ((addedFeatures & TapViewFeatures.RichState) != 0)
            ApplyTrackedRichState(tapView);

        if ((addedFeatures & TapViewFeatures.TouchPointBloom) != 0)
            ApplyTrackedTouchPointBloom(tapView);

        entry.Applied = requestedFeatures;
    }

    private bool HostsTapView(TapViewBase tapView)
    {
        Element current = tapView;

        while (current is not null)
        {
            if (ReferenceEquals(current, this))
                return true;

            current = current.Parent;
        }

        return false;
    }

    private void ApplyTrackedRichState(TapViewBase tapView)
    {
        if (!_richStateControls.Add(tapView))
            return;

        tapView.PropertyChanged += OnRichStateControlPropertyChanged;
        tapView.Unloaded += OnRichStateControlUnloaded;
        ApplyCurrentRichState(tapView);
    }

    private void RemoveTrackedRichState(TapViewBase tapView)
    {
        if (!_richStateControls.Remove(tapView))
            return;

        if (tapView is IVisualTreeElement root)
        {
            foreach (var child in root.GetVisualChildren())
                ApplyRichState(child, TapViewBase.WaitingForTouchState);
        }

        UnregisterRichStateControl(tapView);
    }

    private void ApplyTrackedTouchPointBloom(TapViewBase tapView)
    {
        if (!_touchPointBloomButtons.Add(tapView))
            return;

        _touchPointBloomOriginalPresentationMilliseconds.Add(
            tapView,
            tapView.FeedbackPresentationMilliseconds);
        tapView.FeedbackPresentationMilliseconds = TouchPointBloomPresentationMilliseconds;
        tapView.Touching += OnTouchPointBloomButtonTouching;
        tapView.Unloaded += OnTouchPointBloomButtonUnloaded;
        EnsureTouchPointBloom();
    }

    private void RemoveTrackedTouchPointBloom(TapViewBase tapView)
    {
        if (!_touchPointBloomButtons.Remove(tapView))
            return;

        UnregisterTouchPointBloomButton(tapView);
    }

    private void RemoveEmptyTapViewRegistration(
        TapViewBase tapView,
        TapViewRegistrationEntry entry)
    {
        if (entry.Effective == TapViewFeatures.None && entry.Applied == TapViewFeatures.None)
            _tapViewRegistrations.Remove(tapView);
    }

    private static TapViewFeatures GetRequestedTapViewFeatures(TapViewBase tapView)
    {
        var features = TapViewFeatures.None;

        if (PanelBoss.GetIWantRichState(tapView))
            features |= TapViewFeatures.RichState;

        if (PanelBoss.GetTouchPointBloom(tapView))
            features |= TapViewFeatures.TouchPointBloom;

        return features;
    }

    private void ReportTapViewRegistrationChange(
        TapViewBase tapView,
        TapViewRegistrationSource source,
        bool wasCreated,
        TapViewFeatures previousFeatures,
        TapViewFeatures nextFeatures,
        TapViewRegistrationEntry entry)
    {
        var change = wasCreated
            ? "new"
            : previousFeatures == nextFeatures
                ? "duplicate"
                : "updated";
        var legacyOnly =
            source == TapViewRegistrationSource.LegacyScan &&
            entry.StaticRoster == TapViewFeatures.None &&
            entry.LateLifecycle == TapViewFeatures.None;

        Console.WriteLine(
            $"TAP_VIEW_REGISTRY|source={source}|change={change}|legacy-only={legacyOnly}|features={nextFeatures}|id={tapView.AutomationId}|total={_tapViewRegistrations.Count}");
    }
}
