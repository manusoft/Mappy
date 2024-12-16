using System.Collections;
using System.Reflection;

namespace Mappy;

public static class ObjectMapper
{
    /// <summary>
    /// Maps an object to a destination type, handling complex properties and collections.
    /// </summary>
    public static TDestination Map<TDestination>(this object source, Action<TDestination> customMapping = null) where TDestination : new()
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var destination = new TDestination();
        MapProperties(source, destination);

        // Apply any custom mapping provided by the user
        customMapping?.Invoke(destination);

        return destination;
    }

    /// <summary>
    /// Maps an object to a destination type asynchronously, allowing for custom async transformations.
    /// </summary>
    public static async Task<TDestination> MapAsync<TDestination>(this object source, Func<TDestination, Task> customMapping = null) where TDestination : new()
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var destination = new TDestination();
        MapProperties(source, destination);

        // Apply any custom async mapping provided by the user
        if (customMapping != null)
        {
            await customMapping(destination);
        }

        return destination;
    }

    /// <summary>
    /// Maps an enumerable collection to a list of the destination type.
    /// </summary>
    public static List<TDestination> MapCollection<TDestination>(this IEnumerable source) where TDestination : new()
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var destinationList = new List<TDestination>();
        foreach (var item in source)
        {
            destinationList.Add(item.Map<TDestination>());
        }
        return destinationList;
    }

    /// <summary>
    /// Maps an enumerable collection to a list of the destination type asynchronously.
    /// </summary>
    public static async Task<List<TDestination>> MapCollectionAsync<TDestination>(this IEnumerable source, Func<TDestination, Task> customMapping = null) where TDestination : new()
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var destinationList = new List<TDestination>();
        foreach (var item in source)
        {
            var destination = await item.MapAsync(customMapping);
            destinationList.Add(destination);
        }
        return destinationList;
    }

    /// <summary>
    /// Core mapping logic for properties, including nested and collection handling.
    /// </summary>
    private static void MapProperties(object source, object destination)
    {
        var sourceType = source.GetType();
        var destinationType = destination.GetType();
        var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        var destinationProperties = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (var sourceProp in sourceProperties)
        {
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == sourceProp.Name && p.CanWrite);

            if (destProp == null) continue;

            var sourceValue = sourceProp.GetValue(source);
            if (sourceValue == null)
            {
                destProp.SetValue(destination, null);
                continue;
            }

            if (IsSimpleType(destProp.PropertyType))
            {
                destProp.SetValue(destination, sourceValue);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(destProp.PropertyType) && destProp.PropertyType != typeof(string))
            {
                var collection = MapCollection(sourceValue as IEnumerable, destProp.PropertyType);
                destProp.SetValue(destination, collection);
            }
            else
            {
                var nestedObject = Activator.CreateInstance(destProp.PropertyType);
                MapProperties(sourceValue, nestedObject);
                destProp.SetValue(destination, nestedObject);
            }
        }
    }


    /// <summary>
    /// Maps a source collection to a destination collection.
    /// </summary>
    private static object MapCollection(IEnumerable source, Type destinationType)
    {
        if (source == null) return null;

        // Get the item type of the destination collection
        var itemType = destinationType.IsGenericType
            ? destinationType.GetGenericArguments()[0]
            : destinationType.GetElementType();

        var destinationList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
        foreach (var item in source)
        {
            var mappedItem = Activator.CreateInstance(itemType);
            MapProperties(item, mappedItem);
            destinationList.Add(mappedItem);
        }

        return destinationList;
    }

    /// <summary>
    /// Determines if a type is a simple type (value types, strings, etc.).
    /// </summary>
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || type.IsValueType || type == typeof(string) || type == typeof(DateTime);
    }
}
