using System.Drawing;
using System.Drawing.Drawing2D;

namespace WinUtil.SystemTray;

internal static class TrayMenuGraphics
{
    public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius) =>
        RoundedRectangle(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), radius);

    public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        var d = Math.Min(radius * 2f, Math.Min(bounds.Width, bounds.Height));
        if (d <= 0)
        {
            var p = new GraphicsPath();
            p.AddRectangle(bounds);
            return p;
        }

        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
