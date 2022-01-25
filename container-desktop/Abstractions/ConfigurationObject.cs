using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;


namespace ContainerDesktop.Abstractions
{
    public abstract class ConfigurationObject : NotifyObject, IDataErrorInfo, IConfigurationObject
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.OrdinalIgnoreCase);

        [JsonIgnore]
        [Hide]
        public bool IsValid => ((IDataErrorInfo)this).Error == null;

        protected bool SetValueAndNotify<T>(T value, [CallerMemberName] string propertyName = null)
        {
            return SetValueAndNotify<T>(() => GetValue<T>(propertyName), valueToSet => _values[propertyName] = valueToSet, value, propertyName);
        }

        protected T GetValue<T>([CallerMemberName] string propertyName = null)
        {
            if(_values.TryGetValue(propertyName, out var objectValue))
            {
                return (T)objectValue;
            }
            return default;
        }

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
