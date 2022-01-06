using ContainerDesktop.Abstractions;
using System.Windows;
using System.Windows.Controls;
using Separator = ContainerDesktop.Abstractions.Separator;

namespace ContainerDesktop;

public class MenuItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate ItemTemplate { get; set; }
    public DataTemplate HeaderTemplate { get; set; }
    public DataTemplate SeperatorTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            Separator => SeperatorTemplate,
            Header => HeaderTemplate,
            _ => ItemTemplate
        };
    }
}
