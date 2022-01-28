using ICSharpCode.AvalonEdit;
using System.Windows;

namespace ContainerDesktop;

public class AvalonEditorProperties : DependencyObject
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached("Text", typeof(string), typeof(AvalonEditorProperties), new PropertyMetadata(TextPropertyChanged));
    public static readonly DependencyProperty IsAttachedProperty = DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(AvalonEditorProperties), new PropertyMetadata { DefaultValue = false, PropertyChangedCallback = IsAttachedPropertyChanged });
    public static string GetText(DependencyObject d) => (string)d.GetValue(TextProperty);
    public static void SetText(DependencyObject d, string value) => d.SetValue(TextProperty, value);
    public static bool GetIsAttached(DependencyObject d) => (bool)d.GetValue(IsAttachedProperty);
    public static void SetIsAttached(DependencyObject d, bool value) => d.SetValue(IsAttachedProperty, value);
    private static void TextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextEditor t && e.NewValue is string s && t.Text != s)
        {
            t.Text = s;
        }
    }

    private static void IsAttachedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextEditor t)
        {
            if ((bool)e.NewValue)
            {
                t.Text = GetText(t);
                t.TextChanged += TextChanged;
            }
            else
            {
                t.TextChanged -= TextChanged;
            }
        }
    }

    private static void TextChanged(object sender, EventArgs e)
    {
        if (sender is TextEditor t)
        {
            SetText(t, t.Text);
        }
    }
}