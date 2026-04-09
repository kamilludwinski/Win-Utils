using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace WinUtil.SystemTray;

/// <summary>Rounded dark tray menu with smooth hover pills (Win11-adjacent).</summary>
internal sealed class ModernTrayMenuRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color MenuBg = Color.FromArgb(255, 43, 43, 43);
    private static readonly Color MenuBorder = Color.FromArgb(255, 72, 72, 72);
    private static readonly Color HoverBg = Color.FromArgb(255, 62, 64, 68);
    private static readonly Color TextColor = Color.FromArgb(255, 243, 243, 243);
    private static readonly Color TextMuted = Color.FromArgb(255, 160, 160, 160);

    public ModernTrayMenuRenderer()
        : base(new ModernTrayColorTable())
    {
        RoundedEdges = true;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var bounds = e.AffectedBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        using var bgPath = TrayMenuGraphics.RoundedRectangle(bounds, 8);
        using var fill = new SolidBrush(MenuBg);
        g.FillPath(fill, bgPath);
        using var edge = new Pen(MenuBorder, 1f);
        g.DrawPath(edge, bgPath);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item is ToolStripSeparator)
        {
            base.OnRenderMenuItemBackground(e);
            return;
        }

        if (e.Item is not ToolStripMenuItem { Enabled: true })
        {
            base.OnRenderMenuItemBackground(e);
            return;
        }

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var r = new Rectangle(Point.Empty, e.Item.Size);
        r.Inflate(-4, -2);

        if (e.Item.Selected)
        {
            using var path = TrayMenuGraphics.RoundedRectangle(r, 4);
            using var b = new SolidBrush(HoverBg);
            g.FillPath(b, path);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var color = e.Item.Enabled ? TextColor : TextMuted;
        e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        // Use full item area in local coordinates (ContentRectangle is left-biased for default text).
        var bounds = new Rectangle(Point.Empty, e.Item.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            base.OnRenderItemText(e);
            return;
        }

        TextRenderer.DrawText(
            e.Graphics,
            e.Text,
            e.TextFont,
            bounds,
            color,
            TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPrefix
                | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        // Border drawn with background.
    }

    private sealed class ModernTrayColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => MenuBg;
        public override Color ImageMarginGradientBegin => MenuBg;
        public override Color ImageMarginGradientMiddle => MenuBg;
        public override Color ImageMarginGradientEnd => MenuBg;
        public override Color MenuBorder => Color.Transparent;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => HoverBg;
        public override Color MenuItemSelectedGradientBegin => HoverBg;
        public override Color MenuItemSelectedGradientEnd => HoverBg;
        public override Color SeparatorDark => Color.FromArgb(60, 60, 60);
        public override Color SeparatorLight => Color.FromArgb(60, 60, 60);
    }
}
