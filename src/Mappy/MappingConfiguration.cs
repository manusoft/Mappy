using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Mappy;

/// <summary>
/// Optional per-map configuration. Convention mapping does not require configuration.
/// </summary>
public sealed class MappingConfiguration
{
    internal ConcurrentDictionary<(Type Source, Type Destination), ConcurrentDictionary<string, string>> PropertyMappings { get; } = new();
    internal ConcurrentDictionary<(Type Source, Type Destination, string Property), byte> ExcludedProperties { get; } = new();
    internal ConcurrentDictionary<(Type Source, Type Destination), Func<object, object?>> Converters { get; } = new();

    /// <summary>
    /// Adds a converter function for mapping from TSource to TDestination. This converter will be used instead of the default mapping logic for these types.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="converter"></param>
    /// <returns></returns>
    public MappingConfiguration AddConverter<TSource, TDestination>(Func<TSource, TDestination> converter)
    {
        if (converter is null) throw new ArgumentNullException(nameof(converter));
        Converters[(typeof(TSource), typeof(TDestination))] = value =>
        {
            if (value is null)
            {
                if (default(TSource) is not null)
                {
                    throw new MappingException(
                        $"Cannot invoke converter from '{typeof(TSource).FullName}' with a null value.",
                        typeof(TSource),
                        typeof(TDestination));
                }

                return converter(default!);
            }

            return converter((TSource)value);
        };
        return this;
    }

    internal bool HasConverter(Type sourceType, Type destinationType) => Converters.ContainsKey((sourceType, destinationType));

    /// <summary>
    /// Adds a property mapping from a source property to a destination property for the specified source and destination types. This mapping will be used instead of the default property name matching logic.
    /// </summary>
    /// <param name="sourceType"></param>
    /// <param name="destinationType"></param>
    /// <param name="sourceProperty"></param>
    /// <param name="destinationProperty"></param>
    public void AddPropertyMapping(Type sourceType, Type destinationType, string sourceProperty, string destinationProperty)
    {
        if (sourceType is null) throw new ArgumentNullException(nameof(sourceType));
        if (destinationType is null) throw new ArgumentNullException(nameof(destinationType));
        if (string.IsNullOrWhiteSpace(sourceProperty)) throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(sourceProperty));
        if (string.IsNullOrWhiteSpace(destinationProperty)) throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(destinationProperty));

        var map = PropertyMappings.GetOrAdd(
            (sourceType, destinationType),
            _ => new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        map[sourceProperty] = destinationProperty;
    }

    /// <summary>
    /// Excludes a property from being mapped for the specified source and destination types. This can be used to ignore properties that should not be mapped.
    /// </summary>
    /// <param name="sourceType"></param>
    /// <param name="destinationType"></param>
    /// <param name="propertyName"></param>
    public void ExcludeProperty(Type sourceType, Type destinationType, string propertyName)
    {
        if (sourceType is null) throw new ArgumentNullException(nameof(sourceType));
        if (destinationType is null) throw new ArgumentNullException(nameof(destinationType));
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(propertyName));
        ExcludedProperties[(sourceType, destinationType, propertyName.ToLowerInvariant())] = 0;
    }

    internal bool IsPropertyExcluded(Type sourceType, Type destinationType, string propertyName) =>
        ExcludedProperties.ContainsKey((sourceType, destinationType, propertyName.ToLowerInvariant()));

    /// <summary>
    ///  Adds a property mapping from a source property to a destination property for the specified source and destination types. This mapping will be used instead of the default property name matching logic.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    public MappingConfiguration Map<TSource, TDestination>(Expression<Func<TSource, object?>> source,
                                                           Expression<Func<TDestination, object?>> destination)
    {
        AddPropertyMapping(
            typeof(TSource),
            typeof(TDestination),
            GetMemberName(source),
            GetMemberName(destination));
        return this;
    }

    /// <summary>
    /// Backward-compatible exclusion helper targeting a source property name.
    /// </summary>
    public MappingConfiguration Ignore<TSource, TDestination>(Expression<Func<TSource, object?>> source)
    {
        ExcludeProperty(typeof(TSource), typeof(TDestination), GetMemberName(source));
        return this;
    }

    /// <summary>
    /// Excludes a destination property from automatic member mapping.
    /// </summary>
    public MappingConfiguration IgnoreDestination<TSource, TDestination>(Expression<Func<TDestination, object?>> destination)
    {
        ExcludeProperty(typeof(TSource), typeof(TDestination), GetMemberName(destination));
        return this;
    }

    private static string GetMemberName<T>(Expression<Func<T, object?>> expression)
    {
        Expression body = expression.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            body = unary.Operand;

        if (body is MemberExpression { Member: PropertyInfo property })
            return property.Name;

        throw new ArgumentException("Expression must select a property.", nameof(expression));
    }
}
