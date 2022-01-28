using ContainerDesktop.Abstractions;
using ContainerDesktop.ViewModels;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace ContainerDesktop;

public class SettingsPropertyTemplateSelector : DataTemplateSelector
{
    private static readonly Dictionary<UIEditor, PropertyInfo> _templateProperties = GetTemplateProperties();

    private static Dictionary<UIEditor, PropertyInfo> GetTemplateProperties()
    {
        return (from p in typeof(SettingsPropertyTemplateSelector).GetProperties()
                from a in p.GetCustomAttributes<UIEditorAttribute>()
                select new { p, a })
            .ToDictionary(x => x.a.Editor, x => x.p);
    }

    [UIEditor(UIEditor.Switch)]
    public DataTemplate BooleanTemplate { get; set; }

    [UIEditor(UIEditor.Text)]
    [UIEditor(UIEditor.Numeric)]
    public DataTemplate StringTemplate { get; set; }

    [UIEditor(UIEditor.Password)]
    public DataTemplate PasswordTemplate { get; set; }

    [UIEditor(UIEditor.File)]
    public DataTemplate FileTemplate { get; set; }

    [UIEditor(UIEditor.RadioList)]
    public DataTemplate EnumTemplate { get; set; }

    [UIEditor(UIEditor.Json)]
    public DataTemplate JsonTemplate { get; set; }

    [UIEditor(UIEditor.CheckboxList)]
    public DataTemplate CheckboxListTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is SettingsProperty settingsProperty &&
            _templateProperties.TryGetValue(settingsProperty.UIEditor, out var templateProperty))
        {
            return (DataTemplate)templateProperty.GetValue(this);
        }
        return null;
    }
}
