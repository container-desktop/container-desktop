using ContainerDesktop.Services;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace ContainerDesktop.Pages;

/// <summary>
/// Interaction logic for LogsPage.xaml
/// </summary>
public sealed partial class LogsPage : Page, ILogObserver, IDisposable
{
    private IDisposable _subscription;
    private static readonly JsonFormatter _formatter = new(renderMessage: true);
    private readonly FoldingManager _foldingManager;
    private readonly JsonColorizingTransformer _jsonColorizingTransformer;

    public LogsPage()
    {
        InitializeComponent();
        SearchPanel.Install(editor.TextArea);
        _jsonColorizingTransformer = new JsonColorizingTransformer(() => _foldingManager);
        _foldingManager = FoldingManager.Install(editor.TextArea);
        editor.TextArea.TextView.LineTransformers.Add(_jsonColorizingTransformer);
        editor.TextArea.TextView.ElementGenerators.RemoveAt(0);
        editor.TextArea.TextView.ElementGenerators.Insert(0, new NoBorderFoldingElementGenerator() {  FoldingManager = _foldingManager });
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
            var elem = JsonSerializer.Deserialize<JsonElement>(writer.ToString());
            var json = JsonSerializer.Serialize(elem, new JsonSerializerOptions(JsonSerializerDefaults.General) {  WriteIndented = true });
            Dispatcher.Invoke(() =>
            {
                var startOffset = editor.Document.TextLength;
                editor.AppendText($"{json}");
                var endOffset = editor.Document.TextLength;
                editor.AppendText(Environment.NewLine);
                var foldingSection = _foldingManager.CreateFolding(startOffset, endOffset);
                foldingSection.Title = $"[{logEvent.Timestamp}][{logEvent.Level}] {logEvent.RenderMessage()}";
                foldingSection.IsFolded = true;
            });
        }
        catch
        {
        }
    }

    //private void UpdateFoldings()
    //{
    //    try
    //    {
    //        var doc = editor.Document;
    //        int offset = _foldings.Count == 0 ? 0 : _foldings[_foldings.Count - 1].EndOffset + 2;
    //        int oldOffset = -1;
    //        var newFoldings = new List<NewFolding>();
    //        while (offset < doc.TextLength && offset != oldOffset)
    //        {
    //            oldOffset = offset;
    //            var line = doc.GetLineByOffset(offset);
    //            var text = doc.GetText(line);

    //            if (TryGetJsonElement(text, out var jsonElem) && Reformat(ref text))
    //            {
    //                var newFolding = new NewFolding(line.Offset, line.Offset + text.Length)
    //                {
    //                    DefaultClosed = true,
    //                    Name = GetName(jsonElem) ?? text
    //                };
    //                doc.Replace(line, text);
    //                newFoldings.Add(newFolding);
    //                offset = newFolding.EndOffset + 2;

    //            }
    //            else
    //            {
    //                offset = line.EndOffset + 2;
    //            }

    //        }
    //        if (newFoldings.Count > 0)
    //        {
    //            _foldings.AddRange(newFoldings);
    //            _foldingManager.UpdateFoldings(_foldings, -1);
    //            foreach (var fs in _foldingManager.AllFoldings.Where(x => newFoldings.Any(y => y.StartOffset == x.StartOffset && y.EndOffset == x.EndOffset)))
    //            {
    //                fs.IsFolded = true;
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    //    }
    //}

    //private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    //private bool TryGetJsonElement(string text, out JsonElement jsonElem)
    //{
    //    if (text.StartsWith('{') && text.EndsWith('}'))
    //    {
    //        try
    //        {
    //            jsonElem = JsonSerializer.Deserialize<JsonElement>(text);
    //            return true;
    //        }
    //        catch
    //        {
    //            // Invalid Json just ignore it.
    //        }
    //    }
    //    jsonElem = default;
    //    return false;
    //}

    //private string GetName(JsonElement jsonElem)
    //{
    //    var timestamp = GetStringValue(jsonElem, ("@timestamp", (_, e) => e.GetString()), ("ts", (_, e) => $"{_epoch + TimeSpan.FromSeconds(e.GetDouble()):O}"), ("Timestamp", (_, e) => e.GetString()), ("time", (_, e) => e.GetString()));
    //    var level = GetStringValue(jsonElem, ("level", (_, e) => e.GetString()), ("Level", (_, e) => e.GetString()));
    //    var msg = GetStringValue(jsonElem, ("message", (_, e) => e.GetString()), ("MessageTemplate", (o, e) => GetSerilogMessage(o, e)), ("msg", (_, e) => e.GetString()));
    //    if (timestamp != null)
    //    {
    //        return $"[{timestamp}][{level}] {msg}";
    //    }
    //    return null;
    //}

    //private string GetSerilogMessage(JsonElement objectElem, JsonElement valueElem)
    //{
    //    var f = new Serilog.Formatting.Display.MessageTemplateTextFormatter(valueElem.GetString());
    //    var logEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<LogEvent>(objectElem.ToString(), new LogEventJsonConverter());
    //    var writer = new StringWriter();
    //    f.Format(logEvent, writer);
    //    writer.Flush();
    //    return writer.ToString();
    //}

    //private string GetStringValue(JsonElement jsonElem, params (string propertyName, Func<JsonElement, JsonElement, string> convert)[] properties)
    //{
    //    foreach (var property in properties)
    //    {
    //        if (jsonElem.TryGetProperty(property.propertyName, out var value))
    //        {
    //            return property.convert(jsonElem, value);
    //        }
    //    }
    //    return null;
    //}

    //private bool Reformat(ref string text)
    //{
    //    try
    //    {
    //        var bytes = Encoding.UTF8.GetBytes(text);
    //        var reader = new Utf8JsonReader(bytes);
    //        using var ms = new MemoryStream();
    //        var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
    //        Reformat(reader, writer);
    //        writer.Flush();
    //        text = Encoding.UTF8.GetString(ms.ToArray());
    //        return true;
    //    }
    //    catch
    //    {
    //        return false;
    //    }


    //    void Reformat(Utf8JsonReader reader, Utf8JsonWriter writer)
    //    {
    //        while (reader.Read())
    //        {
    //            switch (reader.TokenType)
    //            {
    //                case JsonTokenType.EndArray:
    //                    writer.WriteEndArray();
    //                    break;
    //                case JsonTokenType.EndObject:
    //                    writer.WriteEndObject();
    //                    break;
    //                case JsonTokenType.False:
    //                    writer.WriteBooleanValue(false);
    //                    break;
    //                case JsonTokenType.Null:
    //                    writer.WriteNullValue();
    //                    break;
    //                case JsonTokenType.Number:
    //                    if (reader.TryGetInt64(out var l))
    //                    {
    //                        writer.WriteNumberValue(l);
    //                    }
    //                    else if (reader.TryGetDouble(out var d))
    //                    {
    //                        writer.WriteNumberValue(d);
    //                    }
    //                    break;
    //                case JsonTokenType.PropertyName:
    //                    var propertyName = reader.GetString();
    //                    writer.WritePropertyName(propertyName);
    //                    break;
    //                case JsonTokenType.StartArray:
    //                    writer.WriteStartArray();
    //                    break;
    //                case JsonTokenType.StartObject:
    //                    writer.WriteStartObject();
    //                    break;
    //                case JsonTokenType.String:
    //                    var s = reader.GetString();
    //                    if (s.StartsWith('{') && s.EndsWith('}'))
    //                    {
    //                        var sreader = new Utf8JsonReader(Encoding.UTF8.GetBytes(s));
    //                        try
    //                        {
    //                            if (JsonDocument.TryParseValue(ref sreader, out var doc))
    //                            {
    //                                doc.WriteTo(writer);
    //                            }
    //                            else
    //                            {
    //                                writer.WriteStringValue(s);
    //                            }
    //                        }
    //                        catch
    //                        {
    //                            writer.WriteStringValue(s);
    //                        }
    //                    }
    //                    else
    //                    {
    //                        writer.WriteStringValue(s);
    //                    }
    //                    break;
    //                case JsonTokenType.True:
    //                    writer.WriteBooleanValue(true);
    //                    break;
    //            }
    //        }
    //    }
    //}

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
