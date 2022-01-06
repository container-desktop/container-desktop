using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace ContainerDesktop;

class JsonColorizingTransformer : DocumentColorizingTransformer
{
    private readonly Func<FoldingManager> _foldingManagerAccessor;

    public JsonColorizingTransformer(Func<FoldingManager> foldingManagerAccessor)
    {
        _foldingManagerAccessor = foldingManagerAccessor;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        var foldingManager = _foldingManagerAccessor();
        if (foldingManager == null)
        {
            return;
        }
        var foldings = foldingManager.GetFoldingsContaining(line.Offset);
        if (foldings.Count == 0)
        {
            return;
        }
        var folding = foldings[0];
        var s = folding.TextContent;
        var data = Encoding.UTF8.GetBytes(s);
        var reader = new Utf8JsonReader(data);
        try
        {
            while (reader.Read())
            {
                var offset = folding.StartOffset + (int)reader.TokenStartIndex;
                if (offset >= line.Offset && offset <= line.EndOffset)
                {
                    var brush = reader.TokenType switch
                    {
                        JsonTokenType.StartObject => Brushes.Black,
                        JsonTokenType.EndObject => Brushes.Black,
                        JsonTokenType.StartArray => Brushes.Black,
                        JsonTokenType.EndArray => Brushes.Black,
                        JsonTokenType.False => Brushes.DodgerBlue,
                        JsonTokenType.True => Brushes.DodgerBlue,
                        JsonTokenType.String => Brushes.Peru,
                        JsonTokenType.PropertyName => Brushes.Teal,
                        JsonTokenType.Number => Brushes.Gray,
                        JsonTokenType.Null => Brushes.DodgerBlue,
                        _ => Brushes.Black
                    };
                    var startOffset = folding.StartOffset + (int)reader.TokenStartIndex;
                    var endOffset = folding.StartOffset + (int)reader.TokenStartIndex + reader.ValueSpan.Length;
                    if (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.String)
                    {
                        // Need to incorporate the double quotes.
                        endOffset += 2;
                    }

                    ChangeLinePart(startOffset, endOffset, ve =>
                    {
                        ve.TextRunProperties.SetForegroundBrush(brush);
                    });
                }
            }
        }
        catch (JsonException)
        {
            // Ignore exceptions
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }
}
