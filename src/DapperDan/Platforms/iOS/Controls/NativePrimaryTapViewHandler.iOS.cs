using Microsoft.Maui.Handlers;
using MauiContentView = Microsoft.Maui.Platform.ContentView;

namespace CodeCrafty.DapperDan.Controls;

/// <summary>
/// Exact iOS handler seam used by the core RichButton native primary-touch bridge.
/// </summary>
public sealed class NativePrimaryTapViewHandler : ContentViewHandler
{
    protected override MauiContentView CreatePlatformView()
    {
        _ = VirtualView
            ?? throw new InvalidOperationException(
                $"{nameof(VirtualView)} must be set before creating the platform view.");
        _ = MauiContext
            ?? throw new InvalidOperationException(
                $"{nameof(MauiContext)} must be set before creating the platform view.");

        return new NativePrimaryTapPlatformView
        {
            CrossPlatformLayout = VirtualView
        };
    }
}

internal sealed class NativePrimaryTapPlatformView : MauiContentView
{
    internal Func<bool> AccessibilityActivateCallback { get; set; }

    public override bool AccessibilityActivate()
    {
        if (AccessibilityActivateCallback is { } callback)
            return callback();

        return base.AccessibilityActivate();
    }
}
