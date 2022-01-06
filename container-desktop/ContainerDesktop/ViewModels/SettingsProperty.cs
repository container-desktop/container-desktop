using ContainerDesktop.Abstractions;
using ContainerDesktop.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows;

namespace ContainerDesktop.ViewModels;

public class SettingsProperty : NotifyObject, IDataErrorInfo
{
    private Visibility _visibility;

    private SettingsProperty(ConfigurationObject settingsObject, PropertyInfo propertyInfo)
    {
        SettingsObject = settingsObject;
        PropertyChangedEventManager.AddHandler(settingsObject, SettingsObjectPropertyChanged, propertyInfo.Name);
        PropertyInfo = propertyInfo;
        (DisplayName, Tooltip, Order, Category) = GetDisplayAttributes(propertyInfo);
        UIEditor = GetUIEditor(propertyInfo);
    }

    public event EventHandler ValueChanged;

    public ConfigurationObject SettingsObject { get; }
    protected PropertyInfo PropertyInfo { get; }
    public string DisplayName { get; }

    public string Category { get; }

    public int Order { get; }

    public string Tooltip { get; }

    public UIEditor UIEditor { get; }

    public object Value
    {
        get => PropertyInfo.GetValue(SettingsObject);
        set
        {
            if (SetValueAndNotify(() => Value, v => PropertyInfo.SetValue(SettingsObject, v), value))
            {
                NotifyValueChanged();
            }
        }
    }

    public IEnumerable<object> EnumValues => Value == null ? Enumerable.Empty<object>() : Enum.GetValues(Value.GetType()).Cast<object>();

    public Visibility Visibility
    {
        get => _visibility;
        set => SetValueAndNotify(ref _visibility, value);
    }

    string IDataErrorInfo.Error => (SettingsObject as IDataErrorInfo)?.Error;

    string IDataErrorInfo.this[string columnName]
    {
        get
        {
            if(SettingsObject is IDataErrorInfo dei)
            {
                return dei[PropertyInfo.Name];
            }
            return null;
        }
    }

    private void Initialize(List<SettingsProperty> properties)
    {
        var visAttr = PropertyInfo.GetCustomAttribute<VisibilityAttribute>();
        if (visAttr != null)
        {
            var otherProp = properties.FirstOrDefault(x => x.PropertyInfo.Name.Equals(visAttr.PropertyName, StringComparison.OrdinalIgnoreCase));
            if (otherProp != null)
            {
                void handler(object sender, EventArgs e)
                {
                    if (sender is SettingsProperty sp)
                    {
                        var b = (visAttr.Value ?? true).Equals(sp.Value);
                        Visibility = b && visAttr.Show || !b && !visAttr.Show ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                otherProp.ValueChanged += handler;
                handler(otherProp, EventArgs.Empty);
            }
            else
            {
                Visibility = visAttr.Show ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    public static IEnumerable<SettingsProperty> CreateSettingsProperties(ConfigurationObject settingsObject)
    {
        var properties = settingsObject.GetType().GetProperties().Select(
            x => new SettingsProperty(settingsObject, x)).ToList();
        foreach (var property in properties)
        {
            property.Initialize(properties);
        }
        return properties;
    }

    private static (string name, string description, int order, string groupName) GetDisplayAttributes(PropertyInfo property)
    {
        var attr = property.GetCustomAttribute<DisplayAttribute>();
        return (attr?.GetName() ?? property.Name, attr?.GetDescription(), attr?.GetOrder() ?? 0, attr?.GetGroupName());
    }

    private static UIEditor GetUIEditor(PropertyInfo property)
    {
        return property.GetCustomAttribute<UIEditorAttribute>()?.Editor ?? UIEditorAttribute.GetDefaultEditorForType(property.PropertyType);
    }

    private void NotifyValueChanged()
    {
        ValueChanged?.Invoke(this, EventArgs.Empty);
        if (Value?.GetType().IsEnum == true)
        {
            NotifyPropertyChanged(nameof(EnumValues));
        }
    }

    private void SettingsObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == PropertyInfo.Name)
        {
            NotifyPropertyChanged(nameof(Value));
            NotifyValueChanged();
        }
    }
}
