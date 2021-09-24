namespace ContainerDesktop.Common.UI;

using System.Drawing;
using System.Windows.Forms;

public sealed class SystemTrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly IntPtr _icon;

    public SystemTrayIcon(string iconPath, ContextMenuBuilder contextMenuBuilder)
    {
        _notifyIcon = new NotifyIcon();
        _icon = PInvoke.User32.LoadImage(IntPtr.Zero, iconPath, PInvoke.User32.ImageType.IMAGE_ICON, 16, 16, PInvoke.User32.LoadImageFlags.LR_LOADFROMFILE);
        _notifyIcon.Icon = Icon.FromHandle(_icon);
        _notifyIcon.Visible = false;
        _notifyIcon.MouseClick += NotifyIconMouseClick;
        _notifyIcon.ContextMenuStrip = contextMenuBuilder?.Build();
    }

    public EventHandler Activate;

    public IntPtr IconHandle => _icon;

    public void Show()
    {
        _notifyIcon.Visible = true;
    }

    public void Hide()
    {
        _notifyIcon.Visible = false;
    }

    private void NotifyIconMouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Activate != null)
        {
            Activate(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
