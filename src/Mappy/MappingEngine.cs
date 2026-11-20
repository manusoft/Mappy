using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Mappy;

internal static class MappingEngine
{
    private static readonly ConcurrentDictionary<Type, bool> SimpleTypeCache = new();

    public static TDestination Map<TDestination>(
        object source,
        MappingConfiguration? config,
        MappingOptions? options)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        var context = new MappingContext(options ?? new MappingOptions());
        var result = MapValue(source, typeof(TDestination), context, config);

        if (result is null)
        {
            if (typeof(TDestination).IsValueType && Nullable.GetUnderlyingType(typeof(TDestination)) is null)
            {
                throw new MappingException(
                    $"Cannot map null to non-nullable destination type '{typeof(TDestination).FullName}'.",
                    source.GetType(),
                    typeof(TDestination));
            }

            return default!;
        }

        return (TDestination)result;
    }

    public static object? MapValue(
        object? source,
        Type destinationType,
        MappingContext context,
        MappingConfiguration? config)
    {
        if (source is null)
        {
            if (IsNullableType(destinationType))
                return null;

            throw new MappingException(
                $"Cannot map null to non-nullable destination type '{destinationType.FullName}'.",
                destinationType: destinationType);
        }

        if (context.Options.AllowConversions &&
            config?.Converters.TryGetValue((source.GetType(), destinationType), out var converter) == true)
        {
            try
            {
                return converter(source);
            }
            catch (MappingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MappingException(
                    $"Custom converter failed from {source.GetType().FullName} to {destinationType.FullName}.",
                    source.GetType(),
                    destinationType,
                    innerException: ex);
            }
        }

        if (context.Options.AllowConversions && TryConvert(source, destinationType, out var converted))
            return converted;

        if (context.Options.PreserveReferences && !source.GetType().IsValueType &&
            context.TryGet(source, out var existing))
            return existing;

        if (TryGetCollectionElementType(destinationType, out var elementType))
            return MapCollection((IEnumerable)source, destinationType, elementType!, context, config);

        if (IsSimpleType(destinationType) || IsSimpleType(source.GetType()))
        {
            throw new MappingException(
                $"Cannot map '{source.GetType().FullName}' to '{destinationType.FullName}'. " +
                "The types are not directly assignable and no converter is registered.",
                source.GetType(),
                destinationType);
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

        if (!IsSimpleType(sourceUnderlying) || !IsSimpleType(destinationUnderlying))
            return !IsSimpleType(sourceUnderlying) && !IsSimpleType(destinationUnderlying);

        if (sourceUnderlying == destinationUnderlying)
            return true;

        if (sourceUnderlying.IsEnum && (destinationUnderlying == typeof(string) || destinationUnderlying.IsPrimitive))
            return true;

        if (destinationUnderlying.IsEnum && (sourceUnderlying == typeof(string) || sourceUnderlying.IsPrimitive))
            return true;

        if (IsNumericType(sourceUnderlying) && IsNumericType(destinationUnderlying))
            return true;

        return false;
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(byte) ||
        type == typeof(sbyte) ||
        type == typeof(short) ||
        type == typeof(ushort) ||
        type == typeof(int) ||
        type == typeof(uint) ||
        type == typeof(long) ||
        type == typeof(ulong) ||
        type == typeof(float) ||
        type == typeof(double) ||
        type == typeof(decimal);

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
            var set = Activator.CreateInstance(destinationType)!;
            var add = destinationType.GetMethod("Add", [elementType])!;
            foreach (var item in list)
                add.Invoke(set, [item]);
            return set;
        }

        if (destinationType.IsAssignableFrom(listType))
            return list;

        if (destinationType.IsInterface)
        {
            var setType = typeof(HashSet<>).MakeGenericType(elementType);
            if (destinationType.IsAssignableFrom(setType))
            {
                var set = Activator.CreateInstance(setType)!;
                var add = setType.GetMethod("Add", [elementType])!;
                foreach (var item in list)
                    add.Invoke(set, [item]);
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

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
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
        var targetType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

        if (destinationType.IsInstanceOfType(source) && IsSimpleType(destinationType))
        {
            result = source;
            return true;
        }

        if (sourceType == targetType && IsSimpleType(targetType))
        {
            result = source;
            return true;
        }

        if (sourceType.IsEnum && targetType == typeof(string))
        {
            result = source.ToString();
            return true;
        }

        if (targetType.IsEnum && source is string text &&
            TryParseEnum(targetType, text, ignoreCase: true, out var parsed))
        {
            result = parsed;
            return true;
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
                // Fall through
            }
        }

        if (sourceType.IsEnum && targetType.IsPrimitive)
        {
            try
            {
                result = Convert.ChangeType(source, targetType);
                return true;
            }
            catch
            {
                // Fall through
            }
        }

        if (IsNumericType(sourceType) && IsNumericType(targetType))
        {
            try
            {
                result = Convert.ChangeType(source, targetType);
                return true;
            }
            catch
            {
                // Fall through
            }
        }

        result = null;
        return false;
    }

    internal static bool IsSimpleType(Type type) =>
        SimpleTypeCache.GetOrAdd(type, static t =>
        {
            var underlying = Nullable.GetUnderlyingType(t) ?? t;
            return underlying.IsPrimitive ||
                   underlying.IsEnum ||
                   underlying == typeof(string) ||
                   underlying == typeof(decimal) ||
                   underlying == typeof(Guid) ||
                   underlying == typeof(DateTime) ||
                   underlying == typeof(DateTimeOffset) ||
                   underlying == typeof(TimeSpan)
#if NET6_0_OR_GREATER
                   || underlying == typeof(DateOnly) 
                   || underlying == typeof(TimeOnly)
#endif
                   ;
        });

    internal static bool IsNullableType(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static readonly ConcurrentDictionary<Type, Func<string, bool, (bool Success, object? Value)>> EnumParsers = new();

    private static bool TryParseEnum(Type enumType, string text, bool ignoreCase, out object? result)
    {
        var parser = EnumParsers.GetOrAdd(enumType, static t =>
        {
            var method = typeof(Enum)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(Enum.TryParse) &&
                             m.IsGenericMethodDefinition &&
                             m.GetParameters().Length == 3 &&
                             m.GetParameters()[1].ParameterType == typeof(bool));

            var genericMethod = method.MakeGenericMethod(t);

            return (input, ignore) =>
            {
                object?[] args = [input, ignore, null];
                var ok = (bool)genericMethod.Invoke(null, args)!;
                return (ok, args[2]);
            };
        });

        var outcome = parser(text, ignoreCase);
        result = outcome.Value;
        return outcome.Success;
    }

    internal static TDestination MapWithContext<TDestination>(
        object source,
        MappingContext context,
        MappingConfiguration? config)
    {
        if(source is null) throw new ArgumentNullException(nameof(source));

        var result = MapValue(source, typeof(TDestination), context, config);

        if (result is null)
        {
            if (typeof(TDestination).IsValueType && Nullable.GetUnderlyingType(typeof(TDestination)) is null)
            {
                throw new MappingException(
                    $"Cannot map null to non-nullable destination type '{typeof(TDestination).FullName}'.",
                    source.GetType(),
                    typeof(TDestination));
            }

            return default!;
        }

        return (TDestination)result;
    }
}
