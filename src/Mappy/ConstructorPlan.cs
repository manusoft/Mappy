using System.Linq;
using System.Reflection;

namespace Mappy;

internal sealed class ConstructorPlan
{
    private readonly ConstructorInfo _constructor;
    private readonly IReadOnlyList<(ParameterInfo Parameter, MemberAccessor? Source)> _parameters;

    private ConstructorPlan(
        ConstructorInfo constructor,
        IReadOnlyList<(ParameterInfo Parameter, MemberAccessor? Source)> parameters)
    {
        _constructor = constructor;
        _parameters = parameters;
    }

    public HashSet<string> ParameterNames =>
    new HashSet<string>(
        _parameters
            .Select(x => x.Parameter.Name!)
            .Where(name => !string.IsNullOrEmpty(name)),
        StringComparer.OrdinalIgnoreCase);

    public static ConstructorPlan? TryCreate(
        IReadOnlyDictionary<string, MemberAccessor> sourceMembers,
        Type destinationType)
    {
        // Prefer parameterless constructor when publicly available and type is not a record-style DTO
        // that requires constructor-injected members.
        var constructors = destinationType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(c => c.GetParameters().Length > 0)
            .OrderByDescending(c => c.GetParameters().Length);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var entries = new List<(ParameterInfo, MemberAccessor?)>(parameters.Length);
            var matchedCount = 0;
            var compatible = true;

            foreach (var parameter in parameters)
            {
                if (parameter.Name is not null &&
                    sourceMembers.TryGetValue(parameter.Name, out var source))
                {
                    if (!MappingEngine.CanMapType(source.Property.PropertyType, parameter.ParameterType))
                    {
                        compatible = false;
                        break;
                    }

                    entries.Add((parameter, source));
                    matchedCount++;
                }
                else
                {
                    // Allow unmatched parameters so extra/computed fields (like AvailableQuantity)
                    // can default and be enriched via `with` or `customMapping`.
                    entries.Add((parameter, null));
                }
            }

            if (compatible && matchedCount > 0)
                return new ConstructorPlan(constructor, entries);
        }

        return null;
    }

    public object Create(object source, MappingContext context, MappingConfiguration? config)
    {
        var parameters = _constructor.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < _parameters.Count; i++)
        {
            var (parameter, sourceMember) = _parameters[i];

            if (sourceMember is null)
            {
                args[i] = parameter.HasDefaultValue
                    ? EnsureValidValue(parameter.DefaultValue, parameter.ParameterType)
                    : GetDefault(parameter.ParameterType);
                continue;
            }

            object? rawValue;
            try
            {
                rawValue = sourceMember.Getter(source);
            }
            catch (Exception ex)
            {
                throw new MappingException(
                    $"Unable to read constructor parameter '{parameter.Name}' while mapping " +
                    $"{source.GetType().FullName} to {_constructor.DeclaringType?.FullName}.",
                    source.GetType(),
                    _constructor.DeclaringType,
                    parameter.Name,
                    ex);
            }

            if (rawValue is null)
            {
                if (!MappingEngine.IsNullableType(parameter.ParameterType))
                {
                    throw new MappingException(
                        $"Source member '{sourceMember.Property.Name}' is null but constructor parameter " +
                        $"'{parameter.Name}' of '{_constructor.DeclaringType?.FullName}' is non-nullable.",
                        source.GetType(),
                        _constructor.DeclaringType,
                        parameter.Name);
                }

                args[i] = null;
                continue;
            }

            var mapped = MappingEngine.MapValue(rawValue, parameter.ParameterType, context, config);
            args[i] = EnsureValidValue(mapped, parameter.ParameterType);
        }

        try
        {
            return _constructor.Invoke(args);
        }
        catch (Exception ex)
        {
            var inner = ex is TargetInvocationException tie ? tie.InnerException ?? tie : ex;
            throw new MappingException(
                $"Constructor mapping failed for '{_constructor.DeclaringType?.FullName}'.",
                source.GetType(),
                _constructor.DeclaringType,
                innerException: inner);
        }
    }

    private static object? EnsureValidValue(object? value, Type targetType)
    {
        if (value is null && targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null)
            return Activator.CreateInstance(targetType);

        return value;
    }

    private static object? GetDefault(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;
}