using System.Collections;

namespace Mappy;

/// <summary>
/// Static mapping API. Reflection metadata is cached and property accessors are compiled once per type pair.
/// </summary>
public static class ObjectMapper
{
    /// <summary>
    /// Maps values from the source object into a new instance of the destination type.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="customMapping"></param>
    /// <param name="config"></param>
    /// <param name="handleCircularReferences"></param>
    /// <returns></returns>
    public static TDestination Map<TDestination>(
        this object source,
        Action<TDestination>? customMapping = null,
        MappingConfiguration? config = null,
        bool handleCircularReferences = true)
    {
        ArgumentNullException.ThrowIfNull(source);

        var options = new MappingOptions
        {
            PreserveReferences = handleCircularReferences
        };

        var destination = MappingEngine.Map<TDestination>(source, config, options);
        customMapping?.Invoke(destination);
        return destination;
    }

    /// <summary>
    /// Maps values from the source object into a new instance of the destination type asynchronously.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="customMapping"></param>
    /// <param name="config"></param>
    /// <param name="handleCircularReferences"></param>
    /// <returns></returns>
    public static async Task<TDestination> MapAsync<TDestination>(
        this object source,
        Func<TDestination, Task>? customMapping = null,
        MappingConfiguration? config = null,
        bool handleCircularReferences = true)
    {
        var destination = source.Map<TDestination>(
            customMapping: null,
            config: config,
            handleCircularReferences: handleCircularReferences);

        if (customMapping is not null)
            await customMapping(destination).ConfigureAwait(false);

        return destination;
    }

    /// <summary>
    /// Maps values from the source collection into a new list of the destination type.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static List<TDestination> MapCollection<TDestination>(
        this IEnumerable source,
        MappingConfiguration? config = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = source is ICollection collection
            ? new List<TDestination>(collection.Count)
            : new List<TDestination>();

        foreach (var item in source)
        {
            if (item is null)
            {
                result.Add(default!);
                continue;
            }

            result.Add(item.Map<TDestination>(config: config));
        }

        return result;
    }

    /// <summary>
    /// Maps values from the source collection into a new list of the destination type asynchronously.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="customMapping"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static async Task<List<TDestination>> MapCollectionAsync<TDestination>(
        this IEnumerable source,
        Func<TDestination, Task>? customMapping = null,
        MappingConfiguration? config = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = source is ICollection collection
            ? new List<TDestination>(collection.Count)
            : new List<TDestination>();

        foreach (var item in source)
        {
            if (item is null)
            {
                result.Add(default!);
                continue;
            }

            var destination = await item.MapAsync(customMapping, config)
                .ConfigureAwait(false);
            result.Add(destination);
        }

        return result;
    }

    /// <summary>
    /// Maps values from the source object into an existing destination instance.
    /// </summary>
    public static void MapTo<TSource, TDestination>(
        this TSource source,
        TDestination destination,
        MappingConfiguration? config = null,
        MappingOptions? options = null)
        where TSource : notnull
        where TDestination : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        var context = new MappingContext(options ?? new MappingOptions());
        var plan = MappingPlan.Get(
            source.GetType(),
            destination.GetType(),
            context.Options.IncludeNonPublicProperties);

        plan.MapInto(source, destination, context, config);
    }
}
