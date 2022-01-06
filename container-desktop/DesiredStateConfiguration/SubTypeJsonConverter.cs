namespace ContainerDesktop.DesiredStateConfiguration;

using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Can be used to serialize a class hierarchy without using the default type serialization.
/// Types should specify the SubtypeAttribute and have a Subtype property on the base class.
/// </summary>
public class SubTypeJsonConverter<TBase> : JsonConverter
{
    private static readonly Dictionary<Type, Dictionary<string, Type>> _subTypeCache = new();
    private readonly string _typePropertyName;
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<Type, string> _typeNameFormatter;

    public SubTypeJsonConverter(Expression<Func<TBase, string>> typePropertyAccessor, Func<Type, string> typeNameFormatter, IServiceProvider serviceProvider)
    {
        _typePropertyName = ((MemberExpression)typePropertyAccessor.Body).Member.Name;
        _serviceProvider = serviceProvider;
        _typeNameFormatter = typeNameFormatter ?? (t => t.Name);
    }

    /// <summary>
    /// Always returns false. This converter is meant to be used with the JsonConverterAttribute.
    /// </summary>
    /// <param name="objectType">The object to convert to.</param>
    /// <returns>A bool indicating if the json can be converted to the provided type</returns>
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TBase);
    }

    /// <summary>
    /// Deserializes the JSON. Derives the sub type from the Subtype property and SubtypeAttribute.
    /// </summary>
    /// <param name="reader">The reader to read the json with.</param>
    /// <param name="objectType">The type of the object to get from the json.</param>
    /// <param name="existingValue">The existing object.</param>
    /// <param name="serializer">The serializer to use</param>
    /// <returns>The deserialized object</returns>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }
        if (objectType == null)
        {
            throw new ArgumentNullException(nameof(objectType));
        }
        if (serializer == null)
        {
            throw new ArgumentNullException(nameof(serializer));
        }

        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var subTypes = GetSubTypes(objectType);
        var jObject = JObject.Load(reader);
        var resolver = serializer.ContractResolver as DefaultContractResolver;
        var propertyName = resolver.GetResolvedPropertyName(_typePropertyName);
        var propertyToken = jObject[_typePropertyName] ?? jObject[propertyName];

        if (propertyToken is JValue jValue &&
            jValue.Value is string type)
        {
            if (subTypes.TryGetValue(type, out var realType))
            {
                var obj = ActivatorUtilities.CreateInstance(_serviceProvider, realType);
                serializer.Populate(jObject.CreateReader(), obj);
                return obj;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Type '{type}', specified in the {_typePropertyName} property, can't be found in the inheritance tree of '{objectType.FullName}'.");
            }
        }
        else
        {
            throw new InvalidOperationException($"{typeof(SubTypeJsonConverter<TBase>).Name} requires a {_typePropertyName} property of type string.");
        }
    }

    /// <summary>
    /// Not implemented. Default serialization is used.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns false because WriteJson is not implemented.
    /// </summary>
    public override bool CanWrite => false;

    private Dictionary<string, Type> GetSubTypes(Type baseType)
    {
        Dictionary<string, Type> subTypes;
        lock (_subTypeCache)
        {
            if (!_subTypeCache.TryGetValue(baseType, out subTypes))
            {
                subTypes = baseType.GetTypeInfo().Assembly
                    .ExportedTypes
                    .Where(type => baseType.IsAssignableFrom(type))
                    .ToDictionary(t => _typeNameFormatter(t), t => t);

                _subTypeCache.Add(baseType, subTypes);
            }
        }
        return subTypes;
    }
}
