using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.Abstractions
{
    public abstract class ConfigurationObject : NotifyObject, IDataErrorInfo
    {
        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                var validationResults = new List<ValidationResult>();
                var property = GetType().GetProperty(columnName);
                if(property == null)
                {
                    return null;
                }
                var validationContext = new ValidationContext(this)
                {
                    MemberName = columnName
                };

                var isValid = Validator.TryValidateProperty(property.GetValue(this), validationContext, validationResults);
                if (isValid)
                {
                    return null;
                }

                return validationResults.First().ErrorMessage;
            }
        }

        string IDataErrorInfo.Error
        {
            get
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(this);
                var isValid = Validator.TryValidateObject(this, validationContext, validationResults);
                if(isValid)
                {
                    return null;
                }
                else
                {
                    return string.Join(Environment.NewLine, validationResults.Select(x => $"[{string.Join(",", x.MemberNames)}] {x.ErrorMessage}"));
                }
            }
        }
    }
}
