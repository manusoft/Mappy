using System.Collections;
using System.Collections.Concurrent;

namespace Mappy;

internal static class MappingEngine
{
    private static readonly ConcurrentDictionary<Type, bool> SimpleTypeCache = new();

    public static TDestination Map<TDestination>(
        object source,
        MappingConfiguration? config,
        MappingOptions? options)
    {
        ArgumentNullException.ThrowIfNull(source);

        var context = new MappingContext(options ?? new MappingOptions());
        return (TDestination)MapValue(source, typeof(TDestination), context, config)!;
    }

    public static object? MapValue(
        object? source,
        Type destinationType,
        MappingContext context,
        MappingConfiguration? config)
    {
        if (source is null)
            return null;

        if ((IsSimpleType(destinationType) || destinationType == typeof(object) ||
             destinationType.IsInterface) &&
            destinationType.IsInstanceOfType(source))
            return source;

        if (context.Options.PreserveReferences && !source.GetType().IsValueType &&
            context.TryGet(source, out var existing))
            return existing;

        if (config?.Converters.TryGetValue((source.GetType(), destinationType), out var converter) == true)
        {
            try
            {
                return converter(source);
            }
            catch (Exception ex)
            {
                throw new MappingException(
                    $"Custom converter failed from {source.GetType().FullName} to {destinationType.FullName}.",
                    source.GetType(), destinationType, innerException: ex);
            }
        }

        if (TryConvert(source, destinationType, out var converted))
            return converted;

        if (TryGetCollectionElementType(destinationType, out var elementType))
            return MapCollection((IEnumerable)source, destinationType, elementType!, context, config);

        if (IsSimpleType(destinationType) || IsSimpleType(source.GetType()))
        {
            throw new MappingException(
                $"Cannot map '{source.GetType().FullName}' to '{destinationType.FullName}'. " +
                "The types are not directly assignable and no converter is registered.",
                source.GetType(), destinationType);
        }

        var plan = MappingPlan.Get(
            source.GetType(),
            destinationType,
            context.Options.IncludeNonPublicProperties);

        return plan.Create(source, context, config);
    }

    public static bool CanMapType(Type sourceType, Type destinationType)
    {
        if (destinationType.IsAssignableFrom(sourceType))
            return true;

        var sourceUnderlying = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var destinationUnderlying = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

        if (destinationUnderlying.IsAssignableFrom(sourceUnderlying))
            return true;

        if (IsSimpleType(sourceType) && IsSimpleType(destinationType) &&
            sourceUnderlying == destinationUnderlying)
            return true;

        // Complex objects can be recursively mapped through a cached plan.
        return !IsSimpleType(sourceType) && !IsSimpleType(destinationType);
    }

    private static object MapCollection(
        IEnumerable source,
        Type destinationType,
        Type elementType,
        MappingContext context,
        MappingConfiguration? config)
    {
        if (destinationType.IsArray)
        {
            var items = new List<object?>();
            foreach (var item in source)
                items.Add(item is null ? null : MapValue(item, elementType, context, config));

            var array = Array.CreateInstance(elementType, items.Count);
            for (var i = 0; i < items.Count; i++)
                array.SetValue(items[i], i);
            return array;
        }

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)(Activator.CreateInstance(listType)
            ?? throw new MappingException($"Unable to create collection '{destinationType.FullName}'."));

        foreach (var item in source)
            list.Add(item is null ? null : MapValue(item, elementType, context, config));

        if (destinationType.IsGenericType &&
            destinationType.GetGenericTypeDefinition() == typeof(HashSet<>))
        {
            var set = Activator.CreateInstance(destinationType);
            var add = destinationType.GetMethod("Add", new[] { elementType })!;
            foreach (var item in list)
                add.Invoke(set, new[] { item });
            return set!;
        }

        // ICollection<T>, IReadOnlyCollection<T>, IEnumerable<T>, IList<T> etc.
        if (destinationType.IsAssignableFrom(listType))
            return list;

        if (destinationType.IsInterface)
        {
            var setType = typeof(HashSet<>).MakeGenericType(elementType);
            if (destinationType.IsAssignableFrom(setType))
            {
                var set = Activator.CreateInstance(setType)!;
                var add = setType.GetMethod("Add", new[] { elementType })!;
                foreach (var item in list)
                    add.Invoke(set, new[] { item });
                return set;
            }
        }

        throw new MappingException(
            $"Collection destination '{destinationType.FullName}' is not supported. " +
            "Use an array, List<T>, HashSet<T>, or a compatible collection interface.",
            destinationType: destinationType);
    }

    private static bool TryGetCollectionElementType(Type type, out Type? elementType)
    {
        if (type == typeof(string))
        {
            elementType = null;
            return false;
        }

        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return elementType is not null;
        }

        if (type.IsGenericType &&
            typeof(IEnumerable).IsAssignableFrom(type))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        var enumerable = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        elementType = enumerable?.GetGenericArguments()[0];
        return elementType is not null;
    }

    private static bool TryConvert(object source, Type destinationType, out object? result)
    {
        var sourceType = source.GetType();

        // Direct assignment is only appropriate for simple values.
        // Complex objects must continue through the mapping plan so
        // nested objects are mapped to new destination instances.
        if (IsSimpleType(destinationType) &&
            destinationType.IsInstanceOfType(source))
        {
            result = source;
            return true;
        }

        var nullableDestination = Nullable.GetUnderlyingType(destinationType);
        var targetType = nullableDestination ?? destinationType;

        if (targetType.IsEnum && source is string text)
        {
            if (Enum.TryParse(
                    targetType,
                    text,
                    ignoreCase: true,
                    out var parsed))
            {
                result = parsed;
                return true;
            }
        }

        if (targetType.IsEnum && sourceType.IsPrimitive)
        {
            try
            {
                result = Enum.ToObject(targetType, source);
                return true;
            }
            catch
            {
                // Let normal mapping/conversion handling continue.
            }
        }

        // Same underlying simple type, e.g.:
        // int -> int?
        if (sourceType == targetType &&
            (IsSimpleType(sourceType) || IsSimpleType(targetType)))
        {
            result = source;
            return true;
        }

        result = null;
        return false;
    }

    private static bool IsSimpleType(Type type) =>
        SimpleTypeCache.GetOrAdd(type, static t =>
        {
            var underlying = Nullable.GetUnderlyingType(t) ?? t;
            return underlying.IsPrimitive ||
                   underlying.IsEnum ||
                   underlying == typeof(string) ||
                   underlying == typeof(decimal) ||
                   underlying == typeof(DateTime) ||
                   underlying == typeof(DateTimeOffset) ||
                   underlying == typeof(TimeSpan) ||
                   underlying == typeof(Guid);
        });
}
