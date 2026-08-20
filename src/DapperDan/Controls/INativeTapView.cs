namespace CodeCrafty.DapperDan.Controls;

/// <summary>
/// Observable contract shared by native-input TapViewBase implementations.
/// Platform bridge state and lifecycle remain concrete-control ownership.
/// </summary>
public interface INativeTapView : ITapViewBase
{
    event EventHandler NativeTouchDown;
}
