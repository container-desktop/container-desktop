namespace ContainerDesktop.Abstractions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public class UIEditorAttribute : Attribute
{
    private static readonly Dictionary<Type, UIEditor> _defaultEditors = new Dictionary<Type, UIEditor>
    {
        [typeof(string)] = UIEditor.Text,
        [typeof(DateTime)] = UIEditor.DateTime,
        [typeof(DateTimeOffset)] = UIEditor.DateTime,
        [typeof(bool)] = UIEditor.Switch,
        [typeof(int)] = UIEditor.Numeric,
        [typeof(long)] = UIEditor.Numeric
    };

    public UIEditorAttribute(UIEditor editor)
    {
        Editor = editor;
    }

    public UIEditor Editor { get; }

    public static UIEditor GetDefaultEditorForType(Type type)
    {
        if (_defaultEditors.TryGetValue(type, out var editor))
        {
            return editor;
        }
        if(type.IsEnum)
        {
            return UIEditor.RadioList;
        }
        return UIEditor.Text;
    }
}
