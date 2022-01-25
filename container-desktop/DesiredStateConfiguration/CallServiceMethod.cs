using ContainerDesktop.Common;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Reflection;

namespace ContainerDesktop.DesiredStateConfiguration;

public class CallServiceMethod : ResourceBase
{
    private readonly IServiceProvider _serviceProvider;

    public CallServiceMethod(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public string ServiceTypeName { get; set; }

    public CallMethodInfo SetMethod { get; set; }

    public CallMethodInfo TestMethod { get; set; }

    public CallMethodInfo UnsetMethod { get; set; }

    public override void Set(ConfigurationContext context) => ExecuteMethod(SetMethod);

    public override bool Test(ConfigurationContext context) => ExecuteMethod(TestMethod);

    public override void Unset(ConfigurationContext context) => ExecuteMethod(UnsetMethod);
    
    public Type ServiceType => ServiceTypeName == null ? null : Type.GetType(ServiceTypeName, false);

    private bool ExecuteMethod(CallMethodInfo method)
    {
        if (!string.IsNullOrWhiteSpace(method?.Name) && ServiceType != null)
        {
            var service = _serviceProvider.GetRequiredService(ServiceType);
            var methodInfo = ServiceType.GetMethod(method.Name);
            if (methodInfo != null)
            {
                var parsedParameters = GetParsedParameters(methodInfo, method.Parameters);
                var ret = methodInfo.Invoke(service, parsedParameters);
                if (ret != null && ret is bool b && b)
                {
                    return b;
                }
            }
            return false;
        }
        return false;
    }

    private static object[] GetParsedParameters(MethodInfo methodInfo, List<string> parameters)
    {
        var methodParams = methodInfo.GetParameters();
        var ret = new object[Math.Min(parameters.Count, methodParams.Length)];
        for (var i = 0; i < methodParams.Length && i < parameters.Count; i++)
        {
            var paramType = methodParams[i].ParameterType;
            var expandedStringValue = Environment.ExpandEnvironmentVariables(parameters[i]);
            ret[i] = ConvertValueHelper.ConvertFrom(paramType, expandedStringValue);
        }
        return ret;
    }
}

public class CallMethodInfo
{
    public string Name { get; set; }
    public List<string> Parameters { get; } = new List<string>();
}
