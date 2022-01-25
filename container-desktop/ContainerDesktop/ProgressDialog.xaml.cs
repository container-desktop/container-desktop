using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ContainerDesktop;

/// <summary>
/// Interaction logic for ProgressDialog.xaml
/// </summary>
public partial class ProgressDialog : Window
{
    public ProgressDialog()
    {
        InitializeComponent();
    }

    public static IDisposableProgress Show(string caption, int max)
    {
        var dlg = new ProgressDialog
        {
            Title = caption
        };
        dlg.txtMessage.Text = string.Empty;
        dlg.progressBar.Minimum = 0;
        dlg.progressBar.Maximum = max;
        dlg.Show();
        return new Progress(s => dlg.Dispatcher.Invoke(() =>
        {
            dlg.txtMessage.Text = s;
            dlg.progressBar.Value += 1;
        }), () => dlg.Dispatcher.Invoke(() => dlg.Close()));
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
    }

    private class Progress : IDisposableProgress
    {
        private readonly Action<string> _progress;
        private readonly Action _close;

        public Progress(Action<string> progress, Action close)
        {
            _progress = progress;
            _close = close;
        }

        public void Dispose()
        {
            _close();
        }

        public void Report(string value)
        {
            _progress(value);
        }
    }
}
