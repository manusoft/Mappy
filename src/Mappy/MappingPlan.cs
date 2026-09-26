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

        _sourceMembers = sourceMembers;
        _destinationMembers = destinationMembers;
        _constructor = ConstructorPlan.TryCreate(sourceMembers, destinationType);
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

            if (config?.ExcludedProperties.Contains(
                    (SourceType, DestinationType, sourceName)) == true)
            {
                continue;
            }

            var destinationName = mappings != null && mappings.TryGetValue(sourceName, out var mappedName)
                ? mappedName
                : sourceName;

            if (!_destinationMembers.TryGetValue(
                    destinationName,
                    out var destinationMember))
            {
                continue;
            }

            if (_constructor?.ParameterNames.Contains(
                    destinationMember.Property.Name) == true)
            {
                continue;
            }

            object? sourceValue;

            try
            {
                sourceValue = sourceMember.Getter(source);
            }
            catch (Exception ex)
            {
                throw new MappingException(
                    $"Unable to read member '{sourceName}' while mapping " +
                    $"{SourceType.Name} to {DestinationType.Name}.",
                    SourceType,
                    DestinationType,
                    sourceName,
                    ex);
            }

            if (sourceValue is null)
            {
                if (!context.Options.IgnoreNullValues &&
                    destinationMember.CanWrite &&
                    MappingEngine.IsNullableType(
                        destinationMember.Property.PropertyType))
                {
                    destinationMember.Setter!(destination, null);
                }

                continue;
            }

            if (!destinationMember.CanWrite)
            {
                continue;
            }

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
                    $"Failed to map member '{sourceName}' from " +
                    $"{SourceType.Name} to {DestinationType.Name}: {ex.Message}",
                    SourceType,
                    DestinationType,
                    sourceName,
                    ex);
            }
        }
    }

    private object CreateDefaultDestination()
    {
        if (DestinationType.IsValueType)
        {
            try
            {
                return Activator.CreateInstance(DestinationType)!;
            }
            catch (Exception ex)
            {
                throw new MappingException(
                    $"Destination type '{DestinationType.FullName}' could not be instantiated.",
                    SourceType, DestinationType, innerException: ex);
            }
        }

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
}
