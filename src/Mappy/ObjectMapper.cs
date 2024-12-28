using System.Collections;
using System.Reflection;

namespace Mappy;


public static class ObjectMapper
{
    /// <summary>
    /// Maps an object to a destination type, handling complex properties and collections.
    /// </summary>
    public static TDestination Map<TDestination>(this object source, Action<TDestination> customMapping = null, bool handleCircularReferences = true) 
        where TDestination : new()
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var destination = new TDestination();
        if (handleCircularReferences)
            MapProperties(source, destination, new HashSet<object>());
        else
            MapProperties(source, destination);

        // Apply any custom mapping provided by the user
        customMapping?.Invoke(destination);

        return destination;
    }

    /// <summary>
    /// Maps an object to a destination type asynchronously, allowing for custom async transformations.
    /// </summary>
    public static async Task<TDestination> MapAsync<TDestination>(this object source, Func<TDestination, Task> customMapping = null, bool handleCircularReferences = true) 
        where TDestination : new()
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var destination = new TDestination();
        if (handleCircularReferences)
            MapProperties(source, destination, new HashSet<object>());
        else
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
            var mappedItem = item.Map<TDestination>();
            destinationList.Add(mappedItem);
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
            var mappedItem = await item.MapAsync(customMapping);
            destinationList.Add(mappedItem);
        }
        return destinationList;
    }

    /// <summary>
    /// Core mapping logic for properties, including nested and collection handling.
    /// </summary>
    private static void MapProperties(object source, object destination, HashSet<object> visited = null)
    {
        if (visited == null)
            visited = new HashSet<object>();

        // Prevent circular reference mapping
        if (visited.Contains(source))
            return;

        visited.Add(source);

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
                if (!sourceProp.PropertyType.IsAssignableFrom(destProp.PropertyType))
                {
                    throw new InvalidOperationException($"Type mismatch: cannot map from {sourceProp.PropertyType} to {destProp.PropertyType}");
                }
                destProp.SetValue(destination, sourceValue);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(destProp.PropertyType) && destProp.PropertyType != typeof(string))
            {
                var collection = MapCollection(sourceValue as IEnumerable, destProp.PropertyType, visited);
                destProp.SetValue(destination, collection);
            }
            else
            {
                // Handle nested complex objects
                var nestedObject = Activator.CreateInstance(destProp.PropertyType);
                MapProperties(sourceValue, nestedObject, visited);
                destProp.SetValue(destination, nestedObject);
            }
        }

        visited.Remove(source); // Clean up the visited set after processing
    }

    /// <summary>
    /// Maps a source collection to a destination collection.
    /// </summary>
    private static object MapCollection(IEnumerable source, Type destinationType, HashSet<object> visited)
    {
        if (source == null) return null;

        var itemType = destinationType.IsGenericType
            ? destinationType.GetGenericArguments()[0]
            : destinationType.GetElementType();

        var destinationList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
        foreach (var item in source)
        {
            if (item == null) continue;

            // Check if the item has already been visited (for circular reference detection)
            if (visited.Contains(item))
            {
                destinationList.Add(null); // Add null for circular reference
                continue;
            }

            // Mark the current item as visited before mapping
            visited.Add(item);

            // Map the item and add it to the destination collection
            var mappedItem = Activator.CreateInstance(itemType);
            MapProperties(item, mappedItem, visited);
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




