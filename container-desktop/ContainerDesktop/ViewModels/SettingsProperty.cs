using ContainerDesktop.Abstractions;
using ContainerDesktop.Common;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows;

namespace ContainerDesktop.ViewModels;

public class SettingsProperty : NotifyObject, IDataErrorInfo
{
    private Visibility _visibility;

    private SettingsProperty(IConfigurationObject settingsObject, PropertyInfo propertyInfo)
    {
        SettingsObject = settingsObject;
        PropertyChangedEventManager.AddHandler(settingsObject, SettingsObjectPropertyChanged, propertyInfo.Name);
        PropertyInfo = propertyInfo;
        (DisplayName, Tooltip, Order, GroupName) = GetDisplayAttributes(propertyInfo);
        Category = GetCategory(propertyInfo);
        UIEditor = GetUIEditor(propertyInfo);
        Items = GetItems(propertyInfo);
    }

    public event EventHandler ValueChanged;

    public IConfigurationObject SettingsObject { get; }
    protected PropertyInfo PropertyInfo { get; }
    public string DisplayName { get; }

    public string GroupName { get; }

    public string Category { get; }

    public int Order { get; }

    public string Tooltip { get; }

    public UIEditor UIEditor { get; }

    public IEnumerable Items { get; } 

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

    public static IEnumerable<SettingsProperty> CreateSettingsProperties(IConfigurationObject settingsObject)
    {
        if(settingsObject == null)
        {
            return Enumerable.Empty<SettingsProperty>();
        }
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

    private static string GetCategory(PropertyInfo property)
    {
        return property.GetCustomAttribute<CategoryAttribute>()?.Category ?? ConfigurationCategories.Basic;
    }

    private static UIEditor GetUIEditor(PropertyInfo property)
    {
        return property.GetCustomAttribute<UIEditorAttribute>()?.Editor ?? UIEditorAttribute.GetDefaultEditorForType(property.PropertyType);
    }

    private IEnumerable GetItems(PropertyInfo property)
    {
        var methodName = property.GetCustomAttribute<ItemsSourceAttribute>()?.MethodName;
        if (methodName == null)
        {
            yield break;
        }
        var method = SettingsObject?.GetType().GetMethod(methodName);
        if(method == null || !typeof(IEnumerable).IsAssignableFrom(method.ReturnType) || method.GetParameters().Length > 0)
        {
            yield break;
        }
        foreach(var item in (IEnumerable) method.Invoke(SettingsObject, null))
        {
            yield return item;
        }
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
