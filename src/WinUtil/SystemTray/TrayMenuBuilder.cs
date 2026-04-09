using System.Drawing;
using System.Windows.Forms;

namespace WinUtil.SystemTray;

internal static class TrayMenuBuilder
{
    public static ContextMenuStrip Create(Action openWindow, Action exitApp)
    {
        var menu = new ContextMenuStrip
        {
            DropShadowEnabled = true,
            ShowImageMargin = false,
            RenderMode = ToolStripRenderMode.Professional,
            Renderer = new ModernTrayMenuRenderer(),
            BackColor = Color.FromArgb(43, 43, 43),
            ForeColor = Color.FromArgb(243, 243, 243),
            Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
            Padding = new Padding(8, 10, 8, 10),
            AutoSize = true,
        };

        menu.Items.Add(MenuItem("Open", (_, _) => openWindow()));
        menu.Items.Add(MenuItem("Exit", (_, _) => exitApp()));

        return menu;
    }

    private static ToolStripMenuItem MenuItem(string text, EventHandler onClick)
    {
        var item = new ToolStripMenuItem(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ForeColor = Color.FromArgb(243, 243, 243),
            Padding = new Padding(20, 11, 20, 11),
        };
        item.Click += onClick;
        return item;
    }
}
