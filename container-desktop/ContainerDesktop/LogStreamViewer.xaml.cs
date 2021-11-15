using ContainerDesktop.Services;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Reactive.Linq;
using System.Windows;

namespace ContainerDesktop;

/// <summary>
/// Interaction logic for LogStreamViewer.xaml
/// </summary>
public partial class LogStreamViewer : Window, ILogObserver
{
    private IDisposable _subscription;
    private static readonly JsonFormatter _formatter = new JsonFormatter(renderMessage: true);

    public LogStreamViewer()
    {
        InitializeComponent();
        SearchPanel.Install(editor.TextArea);
        editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Json");
    }

    public void SubscribeTo(IObservable<LogEvent> observable)
    {
        _subscription = observable.Do(x => WriteEvent(x)).Subscribe();
    }

    private void WriteEvent(LogEvent logEvent)
    {
        try
        {
            using var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            writer.Flush();
            Dispatcher.Invoke(() => editor.AppendText(writer.ToString()));
        }
        catch
        {
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _subscription?.Dispose();
        base.OnClosed(e);
    }
}
