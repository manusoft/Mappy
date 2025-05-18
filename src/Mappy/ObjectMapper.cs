using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Mappy;

public static class ObjectMapper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();
    private static readonly MappingConfiguration _defaultConfig = new();

    /// <summary>
    /// Maps an object to a destination type, allowing for custom transformations.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="customMapping"></param>
    /// <param name="config"></param>
    /// <param name="handleCircularReferences"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static TDestination Map<TDestination>(
         this object source,
         Action<TDestination> customMapping = null,
         MappingConfiguration config = null,
         bool handleCircularReferences = true)
         where TDestination : new()
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var destination = new TDestination();
        MapProperties(source, destination, config ?? _defaultConfig, handleCircularReferences ? new HashSet<object>(ReferenceEqualityComparer.Instance) : null);
        customMapping?.Invoke(destination);
        return destination;
    }

    /// <summary>
    /// Maps an object to a destination type asynchronously, allowing for custom async transformations.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="customMapping"></param>
    /// <param name="config"></param>
    /// <param name="handleCircularReferences"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task<TDestination> MapAsync<TDestination>(
        this object source,
        Func<TDestination, Task> customMapping = null,
        MappingConfiguration config = null,
        bool handleCircularReferences = true)
        where TDestination : new()
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var destination = new TDestination();
        MapProperties(source, destination, config ?? _defaultConfig, handleCircularReferences ? new HashSet<object>(ReferenceEqualityComparer.Instance) : null);
        if (customMapping != null) await customMapping(destination);
        return destination;
    }

    /// <summary>
    /// Maps an enumerable collection to a list of the destination type.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static List<TDestination> MapCollection<TDestination>(
            this IEnumerable source,
            MappingConfiguration config = null)
            where TDestination : new()
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var destinationList = new List<TDestination>();
        foreach (var item in source)
        {
            destinationList.Add(item.Map<TDestination>(config: config));
        }
        return destinationList;
    }

    /// <summary>
    /// Maps an enumerable collection to a list of the destination type asynchronously.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="customMapping"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task<List<TDestination>> MapCollectionAsync<TDestination>(
        this IEnumerable source,
        Func<TDestination, Task> customMapping = null,
        MappingConfiguration config = null)
        where TDestination : new()
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var destinationList = new List<TDestination>();
        foreach (var item in source)
        {
            destinationList.Add(await item.MapAsync(customMapping, config));
        }
        return destinationList;
    }

    /// <summary>
    /// Maps a property from source to destination, handling collections and simple types.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="config"></param>
    /// <param name="visited"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void MapProperties(
        object source,
        object destination,
        MappingConfiguration config,
        HashSet<object> visited)
    {
        if (source == null || destination == null) return;
        if (visited?.Contains(source) == true) return;

        visited?.Add(source);

        var sourceType = source.GetType();
        var destType = destination.GetType();
        var sourceProps = GetCachedProperties(sourceType);
        var destProps = GetCachedProperties(destType).ToDictionary(p => p.Name, p => p);
        var mappings = config.PropertyMappings.GetValueOrDefault((sourceType, destType)) ?? new Dictionary<string, string>();

        foreach (var sourceProp in sourceProps)
        {
            var sourcePropName = sourceProp.Name;
            if (config.ExcludedProperties.Contains((sourceType, destType, sourcePropName))) continue;

            var destPropName = mappings.GetValueOrDefault(sourcePropName, sourcePropName);
            if (!destProps.TryGetValue(destPropName, out var destProp) || !destProp.CanWrite) continue;

            var sourceValue = sourceProp.GetValue(source);
            if (sourceValue == null)
            {
                destProp.SetValue(destination, null);
                continue;
            }

            try
            {
                MapProperty(sourceProp, destProp, sourceValue, destination, config, visited);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to map property '{sourcePropName}' from {sourceType.Name} to {destType.Name}: {ex.Message}",
                    ex);
            }
        }

        visited?.Remove(source);
    } 

    /// <summary>
    /// Checks if a type is simple (primitive or string).
    /// </summary>
    /// <param name="sourceProp"></param>
    /// <param name="destProp"></param>
    /// <param name="sourceValue"></param>
    /// <param name="destination"></param>
    /// <param name="config"></param>
    /// <param name="visited"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void MapProperty(
        PropertyInfo sourceProp,
        PropertyInfo destProp,
        object sourceValue,
        object destination,
        MappingConfiguration config,
        HashSet<object> visited)
    {
        if (IsSimpleType(destProp.PropertyType))
        {
            if (!sourceProp.PropertyType.IsAssignableTo(destProp.PropertyType))
            {
                throw new InvalidOperationException(
                    $"Type mismatch: cannot map from {sourceProp.PropertyType.Name} to {destProp.PropertyType.Name}");
            }
            destProp.SetValue(destination, sourceValue);
        }
        else if (typeof(IEnumerable).IsAssignableFrom(destProp.PropertyType) && destProp.PropertyType != typeof(string))
        {
            var collection = MapCollection(sourceValue as IEnumerable, destProp.PropertyType, config, visited);
            destProp.SetValue(destination, collection);
        }
        else
        {
            var nestedObject = Activator.CreateInstance(destProp.PropertyType)
                ?? throw new InvalidOperationException($"Cannot create instance of {destProp.PropertyType.Name}");
            MapProperties(sourceValue, nestedObject, config, visited);
            destProp.SetValue(destination, nestedObject);
        }
    }

    /// <summary>
    /// Checks if a type is simple (primitive or string).
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destinationType"></param>
    /// <param name="config"></param>
    /// <param name="visited"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static object MapCollection(
        IEnumerable source,
        Type destinationType,
        MappingConfiguration config,
        HashSet<object> visited)
    {
        if (source == null) return null;

        var itemType = destinationType.IsGenericType
            ? destinationType.GetGenericArguments()[0]
            : destinationType.GetElementType() ?? typeof(object);

        var listType = typeof(List<>).MakeGenericType(itemType);
        var destinationList = (IList)Activator.CreateInstance(listType)
            ?? throw new InvalidOperationException($"Cannot create collection of type {listType.Name}");

        foreach (var item in source)
        {
            if (item == null || visited?.Contains(item) == true)
            {
                destinationList.Add(null);
                continue;
            }

            visited?.Add(item);
            var mappedItem = Activator.CreateInstance(itemType)
                ?? throw new InvalidOperationException($"Cannot create instance of {itemType.Name}");
            MapProperties(item, mappedItem, config, visited);
            destinationList.Add(mappedItem);
        }

        if (destinationType.IsArray)
        {
            var array = Array.CreateInstance(itemType, destinationList.Count);
            destinationList.CopyTo(array, 0);
            return array;
        }

        return destinationList;
    }

    /// <summary>
    /// Caches the properties of a type to avoid reflection overhead.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static PropertyInfo[] GetCachedProperties(Type type)
    {
        return _propertyCache.GetOrAdd(
            type,
            t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic));
    }

    /// <summary>
    /// Checks if a type is simple (primitive or string).
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || type.IsValueType || type == typeof(string) || type == typeof(DateTime);
    }


}




