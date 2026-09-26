using System.Linq.Expressions;

namespace Mappy;

/// <summary>
/// Optional per-map configuration. Convention mapping does not require configuration.
/// </summary>
public sealed class MappingConfiguration
{
    internal Dictionary<(Type Source, Type Destination), Dictionary<string, string>> PropertyMappings { get; } = new();
    internal HashSet<(Type Source, Type Destination, string Property)> ExcludedProperties { get; } = new();

    internal Dictionary<(Type Source, Type Destination), Func<object, object?>> Converters { get; } = new();

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
        Converters[(typeof(TSource), typeof(TDestination))] =
            value => converter((TSource)value);
        return this;
    }

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

        var key = (sourceType, destinationType);
        if (!PropertyMappings.TryGetValue(key, out var mappings))
        {
            mappings = new Dictionary<string, string>(StringComparer.Ordinal);
            PropertyMappings[key] = mappings;
        }

        mappings[sourceProperty] = destinationProperty;
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
        ExcludedProperties.Add((sourceType, destinationType, propertyName));
    }

    /// <summary>
    ///  Adds a property mapping from a source property to a destination property for the specified source and destination types. This mapping will be used instead of the default property name matching logic.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    public MappingConfiguration Map<TSource, TDestination>(
        Expression<Func<TSource, object?>> source,
        Expression<Func<TDestination, object?>> destination)
    {
        AddPropertyMapping(typeof(TSource), typeof(TDestination),
            GetMemberName(source), GetMemberName(destination));
        return this;
    }

    /// <summary>
    /// Excludes a property from being mapped for the specified source and destination types. This can be used to ignore properties that should not be mapped.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public MappingConfiguration Ignore<TSource, TDestination>(
        Expression<Func<TSource, object?>> source)
    {
        ExcludeProperty(typeof(TSource), typeof(TDestination), GetMemberName(source));
        return this;
    }

    private static string GetMemberName<T>(Expression<Func<T, object?>> expression)
    {
        Expression body = expression.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            body = unary.Operand;

        if (body is MemberExpression { Member: System.Reflection.PropertyInfo property })
            return property.Name;

        throw new ArgumentException("Expression must select a property.", nameof(expression));
    }
}
