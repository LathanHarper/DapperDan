using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

using System;
using System.Threading.Tasks;

namespace CodeCrafty.DapperDan.PanelBossKit.Helpers
{
    // ????? Animates a Color value and calls back with each frame
    public static class ColorAnimationHelper
    {
        public static async Task AnimateColor(View animationOwner, Color fromColor, Color toColor, Action<Color> callback, Easing easingOption, uint length = 500)
        {
            var animation = new Animation(v =>
            {
                var r = (float)(fromColor.Red + (toColor.Red - fromColor.Red) * v);
                var g = (float)(fromColor.Green + (toColor.Green - fromColor.Green) * v);
                var b = (float)(fromColor.Blue + (toColor.Blue - fromColor.Blue) * v);
                var a = (float)(fromColor.Alpha + (toColor.Alpha - fromColor.Alpha) * v);
                var newColor = new Color(r, g, b, a);
                callback(newColor);
            }, 0, 1);
            animation.Commit(animationOwner, "ColorAnim", 16, length, easingOption);
            await Task.Delay((int)length);
        }
    }
}
