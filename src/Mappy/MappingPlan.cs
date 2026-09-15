using System.Collections.Concurrent;
using System.Reflection;

namespace Mappy;

internal sealed class MappingPlan
{
    private readonly IReadOnlyDictionary<string, MemberAccessor> _sourceMembers;
    private readonly IReadOnlyDictionary<string, MemberAccessor> _destinationMembers;
    private readonly ConstructorPlan? _constructor;

    private MappingPlan(Type sourceType, Type destinationType, bool includeNonPublic)
    {
        SourceType = sourceType;
        DestinationType = destinationType;

        var flags = BindingFlags.Instance | BindingFlags.Public;
        if (includeNonPublic)
            flags |= BindingFlags.NonPublic;

        var sourceMembers = sourceType
            .GetProperties(flags)
            .Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod is not null)
            .Select(MemberAccessor.Create)
            .ToDictionary(x => x.Property.Name, StringComparer.OrdinalIgnoreCase);

        var destinationMembers = destinationType
            .GetProperties(flags)
            .Where(p => p.GetIndexParameters().Length == 0 && p.SetMethod is not null)
            .Select(MemberAccessor.Create)
            .ToDictionary(x => x.Property.Name, StringComparer.OrdinalIgnoreCase);

        _constructor = ConstructorPlan.TryCreate(sourceMembers, destinationType);

        var constructorMembers = _constructor?.ParameterNames ??
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _sourceMembers = sourceMembers;
        _destinationMembers = destinationMembers;

    }

    public Type SourceType { get; }
    public Type DestinationType { get; }

    private static readonly ConcurrentDictionary<(Type, Type, bool), MappingPlan> Cache = new();

    public static MappingPlan Get(Type sourceType, Type destinationType, bool includeNonPublic) =>
        Cache.GetOrAdd((sourceType, destinationType, includeNonPublic),
            static key => new MappingPlan(key.Item1, key.Item2, key.Item3));

    public object Create(object source, MappingContext context, MappingConfiguration? config)
    {
        try
        {
            object destination = _constructor is not null
                ? _constructor.Create(source, context, config)
                : CreateDefaultDestination();

            context.Track(source, destination);

            MapMembers(source, destination, context, config);
            return destination;
        }
        catch (MappingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new MappingException(
                $"Failed to map {SourceType.FullName} to {DestinationType.FullName}.",
                SourceType, DestinationType, innerException: ex);
        }
    }

    public void MapInto(object source, object destination, MappingContext context, MappingConfiguration? config)
    {
        context.Track(source, destination);
        MapMembers(source, destination, context, config);
    }

    private void MapMembers(object source, object destination, MappingContext context, MappingConfiguration? config)
    {
        var mappingKey = (SourceType, DestinationType);

        Dictionary<string, string>? mappings = null;

        if (config is not null)
        {
            config.PropertyMappings.TryGetValue(mappingKey, out mappings);
        }

        foreach (var sourceMember in _sourceMembers.Values)
        {
            var sourceName = sourceMember.Property.Name;

            if (config?.ExcludedProperties.Contains((SourceType, DestinationType, sourceName)) == true)
                continue;

            var destinationName = mappings?.GetValueOrDefault(sourceName) ?? sourceName;
            if (!_destinationMembers.TryGetValue(destinationName, out var destinationMember))
                continue;

            // Constructor-bound members have already been populated.
            if (_constructor?.ParameterNames.Contains(destinationMember.Property.Name) == true)
                continue;

            object? sourceValue;
            try
            {
                sourceValue = sourceMember.Getter(source);
            }
            catch (Exception ex)
            {
                throw new MappingException(
                    $"Unable to read member '{sourceName}' while mapping {SourceType.Name} to {DestinationType.Name}.",
                    SourceType, DestinationType, sourceName, ex);
            }

            if (sourceValue is null)
            {
                if (!context.Options.IgnoreNullValues && destinationMember.CanWrite &&
                    IsNullable(destinationMember.Property.PropertyType))
                    destinationMember.Setter!(destination, null);
                continue;
            }

            if (!destinationMember.CanWrite)
                continue;

            try
            {
                var value = MappingEngine.MapValue(
                    sourceValue,
                    destinationMember.Property.PropertyType,
                    context,
                    config);

                destinationMember.Setter!(destination, value);
            }
            catch (MappingException ex)
            {
                throw new MappingException(
                    $"Failed to map member '{sourceName}' from {SourceType.Name} to {DestinationType.Name}: {ex.Message}",
                    SourceType, DestinationType, sourceName, ex);
            }
        }
    }

    private object CreateDefaultDestination()
    {
        try
        {
            return Activator.CreateInstance(DestinationType, nonPublic: true)
                ?? throw new MappingException(
                    $"Destination type '{DestinationType.FullName}' could not be instantiated.",
                    SourceType, DestinationType);
        }
        catch (MissingMethodException ex)
        {
            throw new MappingException(
                $"Destination type '{DestinationType.FullName}' has no parameterless constructor. " +
                "Use a mappable constructor or provide a custom mapping.",
                SourceType, DestinationType, innerException: ex);
        }
    }

    private static bool IsNullable(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}

internal sealed class ConstructorPlan
{
    private readonly ConstructorInfo _constructor;
    private readonly IReadOnlyList<(ParameterInfo Parameter, MemberAccessor Source)> _parameters;

    private ConstructorPlan(ConstructorInfo constructor,
        IReadOnlyList<(ParameterInfo Parameter, MemberAccessor Source)> parameters)
    {
        _constructor = constructor;
        _parameters = parameters;
    }

    public HashSet<string> ParameterNames =>
        _parameters.Select(x => x.Parameter.Name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static ConstructorPlan? TryCreate(
        IReadOnlyDictionary<string, MemberAccessor> sourceMembers,
        Type destinationType)
    {
        if (destinationType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) is not null)
            return null;

        var constructors = destinationType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var matches = new List<(ParameterInfo, MemberAccessor)>();
            var valid = true;

            foreach (var parameter in parameters)
            {
                if (parameter.Name is null ||
                    !sourceMembers.TryGetValue(parameter.Name, out var source) ||
                    !MappingEngine.CanMapType(source.Property.PropertyType, parameter.ParameterType))
                {
                    valid = false;
                    break;
                }

                matches.Add((parameter, source));
            }

            if (valid)
                return new ConstructorPlan(constructor, matches);
        }

        return null;
    }

    public object Create(object source, MappingContext context, MappingConfiguration? config)
    {
        var args = new object?[_parameters.Count];

        for (var i = 0; i < _parameters.Count; i++)
        {
            var (parameter, sourceMember) = _parameters[i];
            var value = sourceMember.Getter(source);

            if (value is null)
            {
                args[i] = null;
                continue;
            }

            args[i] = MappingEngine.MapValue(value, parameter.ParameterType, context, config);
        }

        try
        {
            return _constructor.Invoke(args);
        }
        catch (TargetInvocationException ex)
        {
            throw new MappingException(
                $"Constructor mapping failed for '{_constructor.DeclaringType?.FullName}'.",
                innerException: ex.InnerException ?? ex);
        }
    }
}
