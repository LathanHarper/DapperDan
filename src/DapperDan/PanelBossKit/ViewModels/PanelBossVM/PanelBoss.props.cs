using CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers;
using CodeCrafty.DapperDan.PanelBossKit.Helpers;
using CodeCrafty.DapperDan.PanelBossKit.Views;
using CodeCrafty.DapperDan.Semantics;

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CodeCrafty.DapperDan.PanelBossKit
{
    //Props
    public partial class PanelBoss : BindableBase
    {

        public static readonly BindableProperty PanelClearanceForProperty =
            BindableProperty.CreateAttached(
                "PanelClearanceFor",
                typeof(string),
                typeof(PanelBoss),
                defaultValue: string.Empty);

        public static void SetPanelClearanceFor(BindableObject rowDefinition, string value)
        {
            rowDefinition.SetValue(PanelClearanceForProperty, value);
        }

        public static string GetPanelClearanceFor(BindableObject rowDefinition)
        {
            return (string)rowDefinition.GetValue(PanelClearanceForProperty);
        }



        public static void SetPanelVerticalOptions(BindableObject view, LayoutOptions value)
        {
            var pbv = view as IPanelBoss_View;
            if (pbv != null)
            {
                pbv.VerticalOptionsBinding = value;
            }
        }


        // Attached property for panel Visibility
        //◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙
        public static readonly BindableProperty PanelIsVisibleProperty =
            BindableProperty.CreateAttached(
                "PanelIsVisible",
                typeof(bool?),
                typeof(View),
                defaultValue: true,
                defaultValueCreator: bindable => true,
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: OnPanelIsVisiblePropertyChanged,
                propertyChanging: OnPanelIsVisiblePropertyChanging
                );

        public static void SetPanelIsVisible(BindableObject view, bool value)
        {
            view.SetValue(PanelIsVisibleProperty, value);
        }



        public static bool GetPanelIsVisible(BindableObject view)
        {
            return CodeCrafty.DapperDan.Controls.BindablePropertyValue.GetBool(view, PanelIsVisibleProperty, true);
        }
        static Task AnimateDoubleAsync(
            VisualElement host,
            string name,
            double from,
            double to,
            uint length,
            Easing easing,
            Action<double> setter)
        {
            var tcs = new TaskCompletionSource<bool>();
            double Lerp(double a, double b, double p) => a + (b - a) * p;

            host.Animate(
                name,
                callback: p => setter(Lerp(from, to, p)),
                length: length,
                easing: easing,
                finished: (v, canceled) =>
                {
                    if (canceled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        tcs.TrySetResult(true);
                    }
                },
                repeat: () => false);

            return tcs.Task;
        }

        static Task AnimatePanelFactorAsync(
            VisualElement host,
            string marker,
            double from,
            double to,
            uint duration,
            Easing easing)
        {
            var panelBoss = FindOwningPanelBoss(host);

            if (panelBoss is null)
            {
                return Task.CompletedTask;
            }

            static double Sanitize(double d)
                => (double.IsNaN(d) || double.IsInfinity(d) || d < 0) ? 0d : d;

            Action<double> setValue = marker switch
            {
                "BottomInputPanelsArea" => v => panelBoss.BottomSafeAreaHeight_InputPanelsFactor = Sanitize(v),
                "BottomStatusPanelsArea" => v => panelBoss.BottomSafeAreaHeight_StatusPanelsFactor = Sanitize(v),
                _ => _ => { }
            };

            // also sanitize endpoints to avoid NaN propagation
            var safeFrom = Sanitize(from);
            var safeTo = Sanitize(to);

            return AnimateDoubleAsync(host, $"PanelFactor:{marker}", safeFrom, safeTo, duration, easing, setValue);
        }
        static async Task EnsureSizedAsync(View v)
        {
            if (v.Height > 0) return;
            var tcs = new TaskCompletionSource();
            void handler(object? s, EventArgs e)
            {
                if (v.Height > 0) { v.SizeChanged -= handler; tcs.TrySetResult(); }
            }
            v.SizeChanged += handler;
            await tcs.Task.ConfigureAwait(false);
        }

        static int NextPanelTransitionRunId(View view)
        {
            var nextRunId = GetPanelTransitionRunId(view) + 1;
            SetPanelTransitionRunId(view, nextRunId);
            return nextRunId;
        }

        static bool IsPanelTransitionCurrent(View view, int runId)
            => GetPanelTransitionRunId(view) == runId;

        private sealed class PanelTransitionLayout
        {
            public View MotionTarget { get; set; }
            public double SlideHeight { get; set; }
            public double SlideWidth { get; set; }
        }

        static bool IsPositiveFinite(double value)
            => value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);

        static double FirstPositive(params double[] values)
        {
            foreach (var value in values)
            {
                if (IsPositiveFinite(value))
                {
                    return value;
                }
            }

            return 0;
        }

        static View FindPanelTargetByName(Element startElement, string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            if (startElement is View startView &&
                (startView.AutomationId == targetName || startView.StyleId == targetName))
            {
                return startView;
            }

            foreach (var child in startElement.LogicalChildren)
            {
                var match = FindPanelTargetByName(child, targetName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        static PanelBoss? FindOwningPanelBoss(Element startElement)
        {
            Element? current = startElement;

            while (current is not null)
            {
                if (current is IPanelBoss_View host)
                {
                    return host.PanelBossInstance;
                }

                current = current.Parent;
            }

            return null;
        }

        static double GetRelativeY(View target, View ancestor)
        {
            double y = target.Y;
            Element current = target.Parent;

            while (current != null && current != ancestor)
            {
                if (current is VisualElement currentView)
                {
                    y += currentView.Y;
                }

                current = current.Parent;
            }

            return current == ancestor ? y : 0;
        }

        static double GetPanelViewportHeight(View panel, View motionTarget, View viewportTarget, double motionHeight)
        {
            var reservedHeight = GetPanelViewportReservedHeight(panel);
            if (IsPositiveFinite(reservedHeight))
            {
                return Math.Max(1, motionHeight - reservedHeight);
            }

            var viewportOffset = GetRelativeY(viewportTarget, motionTarget);
            var bottomPadding = motionTarget is Border border ? border.Padding.Bottom : 0;

            return Math.Max(1, motionHeight - viewportOffset - bottomPadding);
        }

        static async Task<PanelTransitionLayout> PreparePanelTransitionLayoutAsync(View view)
        {
            await EnsureSizedAsync(view).ConfigureAwait(false);

            var layout = new PanelTransitionLayout
            {
                MotionTarget = view,
                SlideHeight = FirstPositive(view.HeightRequest, view.Height, 100),
                SlideWidth = FirstPositive(view.WidthRequest, view.Width, 100)
            };

            if (!string.Equals(GetPanelSizingMode(view), "Viewport", StringComparison.OrdinalIgnoreCase))
            {
                return layout;
            }

            var slot = FindPanelAreaSlotWithMarker(view, out var slotMarker);
            var motionTarget = FindPanelTargetByName(view, GetPanelMotionTargetName(view)) ?? view;
            var viewportTarget = FindPanelTargetByName(view, GetPanelViewportTargetName(view));
            var availableHeight = FirstPositive(slot?.Height ?? 0, view.Height);
            if (string.Equals(slotMarker, "BottomInputPanelsArea", StringComparison.OrdinalIgnoreCase))
            {
                var bottomClearance = Math.Max(
                    0,
                    FindOwningPanelBoss(view)?.PlatformBottomClearance ??
                    PanelMetrics.BottomDrawerClearance);
                availableHeight = Math.Max(1, availableHeight - bottomClearance);
            }

            var motionHeight = IsPositiveFinite(availableHeight)
                ? availableHeight
                : FirstPositive(motionTarget.HeightRequest, motionTarget.Height, 100);

            if (IsPositiveFinite(motionHeight))
            {
                motionTarget.HeightRequest = motionHeight;
            }

            if (viewportTarget != null && IsPositiveFinite(motionHeight))
            {
                viewportTarget.HeightRequest = GetPanelViewportHeight(view, motionTarget, viewportTarget, motionHeight);
            }

            layout.MotionTarget = motionTarget;
            layout.SlideHeight = FirstPositive(motionTarget.HeightRequest, motionTarget.Height, view.Height, 100);
            layout.SlideWidth = FirstPositive(motionTarget.WidthRequest, motionTarget.Width, view.Width, 100);

            return layout;
        }

        // Tracks whether we already initialized a panel that starts visible so we don't animate on first layout
        public static readonly BindableProperty HasInitializedVisibleStateProperty =
            BindableProperty.CreateAttached(
                "HasInitializedVisibleState",
                typeof(bool?),
                typeof(View),
                defaultValue: false,
                defaultValueCreator: bindable => false,
                defaultBindingMode: BindingMode.TwoWay);

        public static bool GetHasInitializedVisibleState(BindableObject view)
            => CodeCrafty.DapperDan.Controls.BindablePropertyValue.GetBool(view, HasInitializedVisibleStateProperty, false);
        public static void SetHasInitializedVisibleState(BindableObject view, bool value)
            => view.SetValue(HasInitializedVisibleStateProperty, value);

        public static readonly BindableProperty PanelTransitionRunIdProperty =
            BindableProperty.CreateAttached(
                "PanelTransitionRunId",
                typeof(int),
                typeof(View),
                defaultValue: 0,
                defaultValueCreator: bindable => 0,
                defaultBindingMode: BindingMode.TwoWay);

        public static int GetPanelTransitionRunId(BindableObject view)
            => (int)view.GetValue(PanelTransitionRunIdProperty);
        public static void SetPanelTransitionRunId(BindableObject view, int value)
            => view.SetValue(PanelTransitionRunIdProperty, value);

        static bool IsHexString(string value)
            => !string.IsNullOrWhiteSpace(value) && value.All(Uri.IsHexDigit);

        static Color GetSmokyTargetColor(string smokyValue)
        {
            const string fallbackColor = "#33333333";

            if (string.IsNullOrWhiteSpace(smokyValue) || string.Equals(smokyValue, "True", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(fallbackColor);

            string trimmed = smokyValue.Trim();

            try
            {
                if (trimmed.StartsWith("#", StringComparison.Ordinal))
                    return Color.FromArgb(trimmed);

                if (trimmed.Length == 8 && IsHexString(trimmed))
                    return Color.FromArgb($"#{trimmed}");

                if (trimmed.Length == 2 && IsHexString(trimmed))
                    return Color.FromArgb($"#{trimmed}333333");
            }
            catch (FormatException)
            {
                return Color.FromArgb(fallbackColor);
            }

            return Color.FromArgb(fallbackColor);
        }

        static Color GetSmokyClearColor(Color targetColor)
            => Color.FromRgba(targetColor.Red, targetColor.Green, targetColor.Blue, 0f);

        static async Task InitializeVisiblePanelFirstTimeAsync(View view)
        {
            // ensure we have size for math
            var layout = await PreparePanelTransitionLayoutAsync(view).ConfigureAwait(false);
            var motionTarget = layout.MotionTarget;
            var slot = FindPanelAreaSlotWithMarker(view, out var marker);
            var smoky = PanelBoss.GetDoThatSmokyBackgroundThing(view);
            double h = layout.SlideHeight;

            // snap smoky bg to final color without anim
            if (!string.IsNullOrEmpty(smoky) && slot != null)
            {
                // use the same target color used by show animations
                slot.BackgroundColor = GetSmokyTargetColor(smoky);
            }

            // put the panel in its final shown state (no animation on first render)
            var transition = GetPanelTransitionIn(view);
            try
            {
                switch (transition)
                {
                    case "None":
                        motionTarget.TranslationX = 0;
                        motionTarget.TranslationY = 0;
                        view.Opacity = 1;
                        motionTarget.Opacity = 1;
                        break;

                    case "SlideInToRight":
                    case "SlideInToLeft":
                    case "SlideInDownward":
                        motionTarget.TranslationX = 0;
                        motionTarget.TranslationY = 0;
                        break;

                    case "SlideInUpward":
                        motionTarget.TranslationY = 0;
                        if (PanelBoss.GetPartOfSafeArea(view) != "False")
                        {
                            // instantly expand safe-area factor to the panel height
                            await AnimatePanelFactorAsync(view, marker, 0, h, 0, Easing.Default).ConfigureAwait(false);
                        }
                        break;

                    case "FadeInDownward":
                        view.Opacity = 1;
                        break;

                    default:
                        // default to a neutral, shown state
                        motionTarget.TranslationX = 0;
                        motionTarget.TranslationY = 0;
                        view.Opacity = 1;
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeVisiblePanelFirstTimeAsync error: {ex}");
            }
        }

        // Fred: Panel visibility property changes run through async void animation code; layout/disposal races here surface as hard-to-trace native-looking crashes.
        private static async void OnPanelIsVisiblePropertyChanging(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is View view)
            {
                var isVisible = newValue switch
                {
                    bool visibleValue => visibleValue,
                    _ => true
                };

                if (view.IsVisible == false && isVisible == false)
                {
                    // already not visible, no exit animation needed
                    return;
                }
                if (isVisible)
                {
                    // Handle showing animation in the changed event
                }
                else
                {
                    // Panel being set to hidden before it was ever shown (initial XAML state)
                    // - no exit animation needed on something never rendered
                    var transitionRunId = NextPanelTransitionRunId(view);

                    if (!GetHasInitializedVisibleState(view))
                    {
                        view.IsVisible = false;
                        return;
                    }

                    // 🏄‍♂️ Find the panel slot with marker

                    // 🏄‍♂️ Fade out smoky background
                    var slot = FindPanelAreaSlotWithMarker(view, out var marker);
                    var DoTheSmokyLayerBackgroundThingValue = PanelBoss.GetDoThatSmokyBackgroundThing(view);
                    //Save the first time hidden thing value and set it to false so we only init once
                    var DoTheFirstTimeHiddenThingValue = PanelBoss.GetDoThatFirstTimeHiddenThing(view);
                    PanelBoss.SetDoThatFirstTimeHiddenThing(view, "False");
                    if (DoTheSmokyLayerBackgroundThingValue != "")
                    {
                        if (slot != null)
                        {
                            var startColor = GetSmokyTargetColor(DoTheSmokyLayerBackgroundThingValue);
                            var endColor = GetSmokyClearColor(startColor);
                            await ColorAnimationHelper.AnimateColor(
                                slot,
                                startColor, endColor,
                                c => slot.BackgroundColor = c,
                                Easing.CubicInOut,
                                200
                                );
                        }
                    }
                    if (!IsPanelTransitionCurrent(view, transitionRunId))
                    {
                        return;
                    }

                    // ensure size before anim math
                    var layout = await PreparePanelTransitionLayoutAsync(view);
                    if (!IsPanelTransitionCurrent(view, transitionRunId))
                    {
                        return;
                    }

                    var motionTarget = layout.MotionTarget;
                    double h = layout.SlideHeight;
                    double w = layout.SlideWidth;
                    var TransitionChoice = GetPanelTransitionOut(view);
                    view.CancelAnimations();
                    motionTarget.CancelAnimations();
                    if (DoTheFirstTimeHiddenThingValue != "True")
                    {
                        switch (TransitionChoice)
                        {
                            case "None":
                                motionTarget.TranslationX = 0;
                                motionTarget.TranslationY = 0;
                                view.Opacity = 1;
                                motionTarget.Opacity = 1;
                                if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                view.IsVisible = false;
                                break;

                            case "SlideOutToRight":
                                motionTarget.TranslationX = 0;
                                await motionTarget.TranslateTo(w, 0, 500, Easing.CubicInOut);
                                if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                view.IsVisible = false;
                                break;

                            case "SlideOutUpward":
                                motionTarget.TranslationY = 0;
                                await motionTarget.TranslateTo(0, h * -1, 500, Easing.CubicInOut);
                                if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                view.IsVisible = false;
                                break;

                            case "SlideOutToLeft":
                                motionTarget.TranslationX = 0;
                                await motionTarget.TranslateTo(w * -1, 0, 500, Easing.CubicInOut);
                                if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                view.IsVisible = false;
                                break;

                            //case "SlideOutDownward":
                            //    view.TranslationY = 0;
                            //    await Task.WhenAll(
                            //        AnimatePanelFactorAsync(false,marker, panelHeight, 0, 1000),
                            //        view.TranslateTo(0, view.Height * 2, 1000, Easing.CubicInOut)
                            //    );
                            //    view.IsVisible = false;
                            //    break;
                            case "SlideOutDownward":
                                {
                                    motionTarget.TranslationY = 0;

                                    var tasks = new List<Task>
                    {
                        // push down by its own height (clean exit)
                        motionTarget.TranslateTo(0, h, 500, Easing.CubicIn),
                    };
                                    if (PanelBoss.GetPartOfSafeArea(view) != "False")
                                    {
                                        tasks.Add(AnimatePanelFactorAsync(view, marker, h, 0, 500, Easing.CubicIn));
                                    }

                                    if (!string.IsNullOrEmpty(DoTheSmokyLayerBackgroundThingValue) && slot != null)
                                    {
                                        var startColor = GetSmokyTargetColor(DoTheSmokyLayerBackgroundThingValue);
                                        var endColor = GetSmokyClearColor(startColor);
                                        tasks.Add(ColorAnimationHelper.AnimateColor(
                                            slot,
                                            startColor,
                                            endColor,
                                            c => slot.BackgroundColor = c,
                                            Easing.CubicIn,
                                            300));
                                    }

                                    try
                                    {
                                        await Task.WhenAll(tasks);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.Write(ex);
                                    }
                                    if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                    view.IsVisible = false;
                                    break;
                                }


                            case "FadeOutUpward":
                                view.Opacity = 1;
                                await view.FadeTo(0, 400, Easing.CubicInOut);
                                if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                view.IsVisible = false;
                                break;

                            default:
                                motionTarget.TranslationX = 0;
                                await motionTarget.TranslateTo(w * -1, 0, 500, Easing.CubicInOut);
                                if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                view.IsVisible = false;
                                break;
                        }

                    }
                    else
                    {
                        switch (TransitionChoice)
                        {
                            case "None":
                                view.Opacity = 0;
                                motionTarget.TranslationX = 0;
                                motionTarget.TranslationY = 0;
                                if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                view.IsVisible = false;
                                view.Opacity = 1;
                                motionTarget.Opacity = 1;
                                break;

                            case "SlideOutToRight":
                                view.Opacity = 0;
                                motionTarget.TranslationX = 0;
                                await motionTarget.TranslateTo(w, 0, 0);
                                if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                view.IsVisible = false;
                                view.Opacity = 1;
                                break;

                            case "SlideOutUpward":
                                view.Opacity = 0;
                                motionTarget.TranslationY = 0;
                                await motionTarget.TranslateTo(0, h * -1, 0);
                                if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                view.IsVisible = false;
                                view.Opacity = 1;
                                break;

                            case "SlideOutToLeft":
                                view.Opacity = 0;
                                motionTarget.TranslationX = 0;
                                await motionTarget.TranslateTo(w * -1, 0, 0);
                                if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                view.IsVisible = false;
                                view.Opacity = 1;
                                break;

                            //case "SlideOutDownward":
                            //    view.TranslationY = 0;
                            //    await Task.WhenAll(
                            //        AnimatePanelFactorAsync(false,marker, panelHeight, 0, 1000),
                            //        view.TranslateTo(0, view.Height * 2, 1000, Easing.CubicInOut)
                            //    );
                            //    view.IsVisible = false;
                            //    break;
                            case "SlideOutDownward":
                                {
                                    view.Opacity = 0;
                                    motionTarget.TranslationY = 0;

                                    var tasks = new List<Task>
                                    {
                                        // push down by its own height (clean exit)
                                        motionTarget.TranslateTo(0, h, 0),
                                    };
                                    if (PanelBoss.GetPartOfSafeArea(view) != "False")
                                    {
                                        tasks.Add(AnimatePanelFactorAsync(view, marker, h, 0, 0, Easing.Default));
                                    }

                                    if (!string.IsNullOrEmpty(DoTheSmokyLayerBackgroundThingValue) && slot != null)
                                    {
                                        var startColor = GetSmokyTargetColor(DoTheSmokyLayerBackgroundThingValue);
                                        var endColor = GetSmokyClearColor(startColor);
                                        tasks.Add(ColorAnimationHelper.AnimateColor(
                                            slot,
                                            startColor,
                                            endColor,
                                            c => slot.BackgroundColor = c,
                                            Easing.Default,
                                            0));
                                    }

                                    try
                                    {
                                        await Task.WhenAll(tasks);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.Write(ex);
                                    }
                                    if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                    view.IsVisible = false;
                                    view.Opacity = 1;
                                    break;
                                }


                            case "FadeOutUpward":
                                view.Opacity = 0;
                                //await view.FadeTo(0, 0);
                                view.IsVisible = false;

                                break;

                            default:
                                Debug.WriteLine("No valid transition out animation found." + TransitionChoice);
                                throw new NotImplementedException();
                                break;
                        }

                    }


                }
            }
        }
        // Fred: This second async void transition path must stay in lockstep with the changing handler or panels can reopen, cloak, or measure from stale state.
        private static async void OnPanelIsVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is View view)
            {
                PanelBossBody_DefaultView.RefreshPanelClearancesFor(view);
                var isVisible = GetPanelIsVisible(view);

                if (isVisible)
                {
                    // Cloak panel to prevent first-frame flash while awaiting layout
                    view.Opacity = 0;
                    view.IsVisible = true;
                    var transitionRunId = NextPanelTransitionRunId(view);
                    // 🏄‍♂️ Find the panel slot with marker
                    var slot = FindPanelAreaSlotWithMarker(view, out var marker);
                    var DoTheSmokyLayerBackgroundThingValue = PanelBoss.GetDoThatSmokyBackgroundThing(view);

                    // First-time startup path: if this panel hasn't been initialized yet and starts visible, snap to final state without anim
                    if (!GetHasInitializedVisibleState(view))
                    {
                        SetHasInitializedVisibleState(view, true);
                        try
                        {
                            if (!string.IsNullOrEmpty(DoTheSmokyLayerBackgroundThingValue) && slot != null)
                            {
                                slot.BackgroundColor = GetSmokyTargetColor(DoTheSmokyLayerBackgroundThingValue);
                            }
                            await InitializeVisiblePanelFirstTimeAsync(view);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Panel first-time visible init failed: {ex}");
                        }
                        view.Opacity = 1;
                        return; // don't run the animated path on first paint
                    }

                    if (DoTheSmokyLayerBackgroundThingValue != "")
                    {
                        if (slot != null)
                        {
                            // Fade in smoky background
                            var endColor = GetSmokyTargetColor(DoTheSmokyLayerBackgroundThingValue);
                            var startColor = GetSmokyClearColor(endColor);
                            await ColorAnimationHelper.AnimateColor(
                                slot,
                                startColor, endColor,
                                c => slot.BackgroundColor = c,
                                Easing.CubicInOut,
                                400
                                );
                        }
                    }




                    if (!IsPanelTransitionCurrent(view, transitionRunId))
                    {
                        return;
                    }

                    // ensure size before anim math
                    var layout = await PreparePanelTransitionLayoutAsync(view);
                    if (!IsPanelTransitionCurrent(view, transitionRunId))
                    {
                        return;
                    }

                    var motionTarget = layout.MotionTarget;
                    double h = layout.SlideHeight;
                    double w = layout.SlideWidth;


                    var TransitionChoice = GetPanelTransitionIn(view);
                    view.CancelAnimations();
                    motionTarget.CancelAnimations();
                    switch (TransitionChoice)
                    {
                        case "None":
                            motionTarget.TranslationX = 0;
                            motionTarget.TranslationY = 0;
                            view.Opacity = 1;
                            motionTarget.Opacity = 1;
                            break;

                        case "SlideInToRight":
                            motionTarget.TranslationX = w * -1;
                            view.Opacity = 1;
                            motionTarget.Opacity = 1;
                            await motionTarget.TranslateTo(0, 0, 500, Easing.CubicInOut);
                            if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                            break;

                        //case "SlideInUpward":
                        //    view.TranslationY = view.Height * 2;
                        //    await Task.WhenAll(
                        //        view.TranslateTo(0, 0, 500, Easing.CubicInOut),
                        //        AnimatePanelFactorAsync(false, marker, 0, panelHeight, 500)
                        //    );
                        //    break;
                        case "SlideInUpward":
                            {
                                // start off-screen below
                                motionTarget.TranslationY = h;
                                view.Opacity = 1;
                                motionTarget.Opacity = 1;

                                var tasks = new List<Task>
                    {
                        motionTarget.TranslateTo(0, 0, 500, Easing.CubicOut),
                    };
                                if (PanelBoss.GetPartOfSafeArea(view) != "False")
                                {
                                    tasks.Add(AnimatePanelFactorAsync(view, marker, 0, h, 500, Easing.CubicOut));
                                }
                                if (!string.IsNullOrEmpty(DoTheSmokyLayerBackgroundThingValue) && slot != null)
                                {
                                    var endColor = GetSmokyTargetColor(DoTheSmokyLayerBackgroundThingValue);
                                    var startColor = GetSmokyClearColor(endColor);
                                    tasks.Add(ColorAnimationHelper.AnimateColor(
                                        slot,
                                        startColor,
                                        endColor,
                                        c => slot.BackgroundColor = c,
                                        Easing.CubicOut,
                                        400));
                                }

                                await Task.WhenAll(tasks);
                                if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                                break;
                            }
                        case "SlideInToLeft":
                            motionTarget.TranslationX = w;
                            view.Opacity = 1;
                            motionTarget.Opacity = 1;
                            await motionTarget.TranslateTo(0, 0, 500, Easing.CubicInOut);
                            if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                            break;

                        case "SlideInDownward":
                            motionTarget.TranslationY = h * -1;
                            view.Opacity = 1;
                            motionTarget.Opacity = 1;
                            await motionTarget.TranslateTo(0, 0, 500, Easing.CubicInOut);
                            if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                            break;

                        case "FadeInDownward":
                            // opacity already 0 from cloak - animate to visible
                            await view.FadeTo(1, 400, Easing.CubicInOut);
                            if (!IsPanelTransitionCurrent(view, transitionRunId)) return;
                            break;

                        default:
                            view.Opacity = 1;
                            break;
                    }

                }
                else
                {
                    // Panel starts hidden (e.g., XAML PanelIsVisible="False") - mark as
                    // initialized so the first show uses the animated path, not the snap path
                    if (!GetHasInitializedVisibleState(view))
                    {
                        SetHasInitializedVisibleState(view, true);
                        view.IsVisible = false;
                    }
                    // Exit animation is handled in the Changing event
                }
            }
        }


        // Attached property for panel Name
        //◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙
        public static readonly BindableProperty PanelNameProperty =
            BindableProperty.CreateAttached(
                "PanelName",
                typeof(string),
                typeof(View),
                defaultValue: "",
                defaultValueCreator: bindable => "",
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: OnPanelNamePropertyChanged);

        public static void SetPanelName(BindableObject view, string value)
        {
            view.SetValue(PanelNameProperty, value);
        }

        public static string GetPanelName(BindableObject view)
        {
            return (string)view.GetValue(PanelNameProperty);
        }

        private static void OnPanelNamePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is View view && newValue is string panelName)
            {

            }
        }

        // Attached property for panel Priority Order
        //◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙
        public static readonly BindableProperty PanelPriorityProperty =
            BindableProperty.CreateAttached(
                "PanelPriority",
                typeof(decimal),
                typeof(View),
                defaultValue: 0M,
                defaultValueCreator: bindable => 0M,
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: OnPanelPriorityPropertyChanged);

        public static void SetPanelPriority(BindableObject view, decimal value)
        {
            view.SetValue(PanelPriorityProperty, value);
        }

        public static decimal GetPanelPriority(BindableObject view)
        {
            return (decimal)view.GetValue(PanelPriorityProperty);
        }

        private static void OnPanelPriorityPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is View view && newValue is decimal isVisible)
            {

            }
        }

        // Attached property for panel Entrance animation
        //◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙
        public static readonly BindableProperty PanelTransitionInProperty =
            BindableProperty.CreateAttached(
                "PanelTransitionIn",
                typeof(string),
                typeof(View),
                defaultValue: "SlideInToRight",
                defaultValueCreator: bindable => "SlideInToRight",
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: OnPanelTransitionInPropertyChanged,
                propertyChanging: OnPanelTransitionInPropertyChanging
                );

        public static void SetPanelTransitionIn(BindableObject view, string value)
        {
            view.SetValue(PanelTransitionInProperty, value);
        }

        public static string GetPanelTransitionIn(BindableObject view)
        {
            return (string)view.GetValue(PanelTransitionInProperty);
        }

        // Fred: Transition-in changed is an async void bindable-property hook; future animation work here will surface exceptions outside any caller-owned Task flow.
        private static async void OnPanelTransitionInPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            //if (bindable is View view && newValue is string transitionIn)
            //{

            //}
        }

        // Fred: Transition-in changing is a second async void hook for the same panel state, so ordering drift can split the animation contract across two unobserved paths.
        private static async void OnPanelTransitionInPropertyChanging(BindableObject bindable, object oldValue, object newValue)
        {
            //if (bindable is View view && newValue is string transitionIn)
            //{

            //}
        }
        // Attached property for panel Exit Transition

        //◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙◙
        public static readonly BindableProperty PanelTransitionOutProperty =
            BindableProperty.CreateAttached(
                "PanelTransitionOut",
                typeof(string),
                typeof(View),
                defaultValue: "SlideOutToLeft",
                defaultValueCreator: bindable => "SlideOutToLeft",
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: OnPanelTransitionOutPropertyChanged,
                propertyChanging: OnPanelTransitionOutPropertyChanging
                );

        public static void SetPanelTransitionOut(BindableObject view, string value)
        {
            view.SetValue(PanelTransitionOutProperty, value);
        }

        public static string GetPanelTransitionOut(BindableObject view)
        {
            return (string)view.GetValue(PanelTransitionOutProperty);
        }

        // Fred: Transition-out changed is async void and tied to attached-property mutation; bad transition values or future awaits will not report through PanelBoss callers.
        private static async void OnPanelTransitionOutPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            //if (bindable is View view && newValue is string transitionOut)
            //{

            //}
        }

        // Fred: Transition-out changing can run before the visible-state path settles, and async void makes any sequencing failure hard to attribute at runtime.
        private static async void OnPanelTransitionOutPropertyChanging(BindableObject bindable, object oldValue, object newValue)
        {
            //if (bindable is View view && newValue is string transitionOut)
            //{

            //}
        }

        public static readonly BindableProperty PartOfSafeAreaProperty =
    BindableProperty.CreateAttached(
        "PartOfSafeArea",
        typeof(string),
        typeof(View),
        defaultValue: "",
        defaultValueCreator: bindable => "",
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: OnPartOfSafeAreaPropertyChanged,
        propertyChanging: OnPartOfSafeAreaPropertyChanging
    );

        public static void SetPartOfSafeArea(BindableObject view, string value)
        {
            view.SetValue(PartOfSafeAreaProperty, value);
        }

        public static string GetPartOfSafeArea(BindableObject view)
        {
            return (string)view.GetValue(PartOfSafeAreaProperty);
        }

        private static void OnPartOfSafeAreaPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            // 🏄‍♂️ No aggro here, just update as needed when the safe area part changes!
            // If you need to react, drop your code in this barrel.
        }
        private static void OnPartOfSafeAreaPropertyChanging(BindableObject bindable, object oldValue, object newValue)
        {
            // 🏄‍♂️ Paddle out if you need to handle the change before it happens!
        }

        public static readonly BindableProperty PanelSizingModeProperty =
            BindableProperty.CreateAttached(
                "PanelSizingMode",
                typeof(string),
                typeof(View),
                defaultValue: "",
                defaultValueCreator: bindable => "",
                defaultBindingMode: BindingMode.TwoWay);

        public static void SetPanelSizingMode(BindableObject view, string value)
        {
            view.SetValue(PanelSizingModeProperty, value);
        }

        public static string GetPanelSizingMode(BindableObject view)
        {
            return (string)view.GetValue(PanelSizingModeProperty);
        }

        public static readonly BindableProperty PanelMotionTargetNameProperty =
            BindableProperty.CreateAttached(
                "PanelMotionTargetName",
                typeof(string),
                typeof(View),
                defaultValue: "",
                defaultValueCreator: bindable => "",
                defaultBindingMode: BindingMode.TwoWay);

        public static void SetPanelMotionTargetName(BindableObject view, string value)
        {
            view.SetValue(PanelMotionTargetNameProperty, value);
        }

        public static string GetPanelMotionTargetName(BindableObject view)
        {
            return (string)view.GetValue(PanelMotionTargetNameProperty);
        }

        public static readonly BindableProperty PanelViewportTargetNameProperty =
            BindableProperty.CreateAttached(
                "PanelViewportTargetName",
                typeof(string),
                typeof(View),
                defaultValue: "",
                defaultValueCreator: bindable => "",
                defaultBindingMode: BindingMode.TwoWay);

        public static void SetPanelViewportTargetName(BindableObject view, string value)
        {
            view.SetValue(PanelViewportTargetNameProperty, value);
        }

        public static string GetPanelViewportTargetName(BindableObject view)
        {
            return (string)view.GetValue(PanelViewportTargetNameProperty);
        }

        public static readonly BindableProperty PanelViewportReservedHeightProperty =
            BindableProperty.CreateAttached(
                "PanelViewportReservedHeight",
                typeof(double),
                typeof(View),
                defaultValue: -1d,
                defaultValueCreator: bindable => -1d,
                defaultBindingMode: BindingMode.TwoWay);

        public static void SetPanelViewportReservedHeight(BindableObject view, double value)
        {
            view.SetValue(PanelViewportReservedHeightProperty, value);
        }

        public static double GetPanelViewportReservedHeight(BindableObject view)
        {
            return (double)view.GetValue(PanelViewportReservedHeightProperty);
        }

        public static readonly BindableProperty PanelZIndexProperty =
    BindableProperty.CreateAttached(
        "PanelZIndex",
        typeof(int),
        typeof(View),
        defaultValue: 0,
        defaultValueCreator: bindable => 0,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: OnPanelZIndexPropertyChanged,
        propertyChanging: OnPanelZIndexPropertyChanging
    );

        public static void SetPanelZIndex(BindableObject view, int value)
        {
            view.SetValue(PanelZIndexProperty, value);
        }

        public static int GetPanelZIndex(BindableObject view)
        {
            return (int)view.GetValue(PanelZIndexProperty);
        }

        private static void OnPanelZIndexPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            // 🏄‍♂️ No aggro here, just update as needed when the panel Z index changes!
            // If you need to react, drop your code in this barrel.
        }
        private static void OnPanelZIndexPropertyChanging(BindableObject bindable, object oldValue, object newValue)
        {
            // 🏄‍♂️ Paddle out if you need to handle the change before it happens!
        }

        // 🏄‍♂️ New fin: lets you set the max alpha for smoky background animation!
        public static readonly BindableProperty DoThatSmokyBackgroundThingProperty =
            BindableProperty.CreateAttached(
                "DoThatSmokyBackgroundThing",
                typeof(string),
                typeof(View),
                defaultValue: "",
                defaultValueCreator: bindable => "",
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: OnDoThatSmokyBackgroundThingChanged
            );

        public static void SetDoThatSmokyBackgroundThing(BindableObject view, string value)
        {
            view.SetValue(DoThatSmokyBackgroundThingProperty, value);
        }

        public static string GetDoThatSmokyBackgroundThing(BindableObject view)
        {
            return (string)view.GetValue(DoThatSmokyBackgroundThingProperty);
        }

        // Fred: Smoky-background changes are wired as async void even while the animation body is parked; re-enabling it would bypass normal exception/reporting flow.
        private static async void OnDoThatSmokyBackgroundThingChanged(BindableObject bindable, object oldValue, object newValue)
        {
            //if (bindable is View view && newValue is string alphaByte)
            //{
            //    // Animate from #00333333 to #{alphaByte}333333
            //    var startColor = Color.FromArgb("#00333333");
            //    var endColor = Color.FromArgb($"#{alphaByte}333333");
            //    await ColorAnimationHelper.AnimateColor(view, startColor, endColor, c => view.BackgroundColor = c, 500);
            //}
        }

        // 🏄‍♂️ New fin: lets you set the max alpha for smoky background animation!
        public static readonly BindableProperty DoThatFirstTimeHiddenThingProperty =
            BindableProperty.CreateAttached(
                "DoThatFirstTimeHiddenThing",
                typeof(string),
                typeof(View),
                defaultValue: "No",
                defaultValueCreator: bindable => "No",
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: OnDoThatFirstTimeHiddenThingChanged
            );

        public static void SetDoThatFirstTimeHiddenThing(BindableObject view, string value)
        {
            view.SetValue(DoThatFirstTimeHiddenThingProperty, value);
        }

        public static string GetDoThatFirstTimeHiddenThing(BindableObject view)
        {
            return (string)view.GetValue(DoThatFirstTimeHiddenThingProperty);
        }

        // Fred: First-time hidden changes are another async void animation hook, so startup visibility races will be hard to connect back to the triggering property.
        private static async void OnDoThatFirstTimeHiddenThingChanged(BindableObject bindable, object oldValue, object newValue)
        {
            //if (bindable is View view && newValue is string alphaByte)
            //{
            //    // Animate from #00333333 to #{alphaByte}333333
            //    var startColor = Color.FromArgb("#00333333");
            //    var endColor = Color.FromArgb($"#{alphaByte}333333");
            //    await ColorAnimationHelper.AnimateColor(view, startColor, endColor, c => view.BackgroundColor = c, 500);
            //}
        }

        // 🏄‍♂️ PanelAreaLookupMarker: lets you tag a panel slot for lookup/animation
        public static readonly BindableProperty PanelAreaLookupMarkerProperty =
            BindableProperty.CreateAttached(
                "PanelAreaLookupMarker",
                typeof(string),
                typeof(View),
                defaultValue: "",
                defaultValueCreator: bindable => "",
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: OnPanelAreaLookupMarkerChanged
            );

        public static void SetPanelAreaLookupMarker(BindableObject view, string value)
        {
            view.SetValue(PanelAreaLookupMarkerProperty, value);
        }

        public static string GetPanelAreaLookupMarker(BindableObject view)
        {
            return (string)view.GetValue(PanelAreaLookupMarkerProperty);
        }

        private static void OnPanelAreaLookupMarkerChanged(BindableObject bindable, object oldValue, object newValue)
        {
            // 🏄‍♂️ No aggro-just a marker for lookup, no action needed here.
        }

        // 🏄‍♂️ Helper: climb parent stack to find first PanelAreaLookupMarker
        private static View FindPanelAreaSlotWithMarker(View startView, out string marker)
        {
            marker = "";
            Element current = startView;
            while (current != null)
            {
                if (current is View v)
                {
                    var m = GetPanelAreaLookupMarker(v);
                    if (!string.IsNullOrEmpty(m))
                    {
                        marker = m;
                        return v;
                    }
                }
                current = current.Parent;
            }
            return null;
        }


    }
}
