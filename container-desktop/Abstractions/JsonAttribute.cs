using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.Abstractions;

public sealed class JsonAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        var valid = false;
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            try
            {
                _ = JToken.Parse(s);
                valid = true;
            }
            catch
            {
                // Do nothing
            }
        }
        return valid;
    }
}
