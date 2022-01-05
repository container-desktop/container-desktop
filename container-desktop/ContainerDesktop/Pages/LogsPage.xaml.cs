using ContainerDesktop.Services;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Reactive.Linq;
using System.Windows.Controls;

namespace ContainerDesktop.Pages;

/// <summary>
/// Interaction logic for LogsPage.xaml
/// </summary>
public partial class LogsPage : Page, ILogObserver, IDisposable
{
    private IDisposable _subscription;
    private static readonly JsonFormatter _formatter = new JsonFormatter(renderMessage: true);

    public LogsPage()
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

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
