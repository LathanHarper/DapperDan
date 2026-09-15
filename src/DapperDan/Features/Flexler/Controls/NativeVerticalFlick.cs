
namespace Flexler.Controls;

public enum NativeVerticalFlickDirection
{
    Up,
    Down
}

public sealed class NativeVerticalFlickEventArgs : EventArgs
{
    public NativeVerticalFlickEventArgs(NativeVerticalFlickDirection direction)
    {
        Direction = direction;
    }

    public NativeVerticalFlickDirection Direction { get; }
}
