using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.Abstractions;

public class RequiredIfAttribute : RequiredAttribute
{
    public RequiredIfAttribute(string propertyName, object propertyValue)
    {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        PropertyValue = propertyValue;
    }

    public string PropertyName { get; init; }
    public object PropertyValue { get; init; }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var result = base.IsValid(value, validationContext);
        var property = validationContext.ObjectType.GetProperty(PropertyName);
        if (property == null)
        {
            return new ValidationResult($"Could not find property '{PropertyName}' on type '{validationContext.ObjectType}", new[] { validationContext.MemberName });
        }
        var currentPropValue = property.GetValue(validationContext.ObjectInstance);
        if (Equals(currentPropValue, PropertyValue))
        {
            return result;
        }
        return ValidationResult.Success;
    }
}
