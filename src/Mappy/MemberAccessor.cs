using System.Linq.Expressions;
using System.Reflection;

namespace Mappy;

internal sealed class MemberAccessor
{
    private MemberAccessor(PropertyInfo property, Func<object, object?> getter, Action<object, object?>? setter)
    {
        Property = property;
        Getter = getter;
        Setter = setter;
    }

    public PropertyInfo Property { get; }
    public Func<object, object?> Getter { get; }
    public Action<object, object?>? Setter { get; }
    public bool CanWrite => Setter is not null;

    public static MemberAccessor Create(PropertyInfo property, bool allowNonPublicSetter = true)
    {
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var targetType = property.DeclaringType!;
        var typedInstance = Expression.Convert(instanceParam, targetType);
        var propertyExpr = Expression.Property(typedInstance, property);

        var getter = Expression.Lambda<Func<object, object?>>(
            Expression.Convert(propertyExpr, typeof(object)),
            instanceParam).Compile();

        Action<object, object?>? setter = null;
        var setMethod = property.SetMethod;
        if (setMethod is not null && (allowNonPublicSetter || setMethod.IsPublic))
        {
            var valueParam = Expression.Parameter(typeof(object), "value");
            var convertedValue = Expression.Convert(valueParam, property.PropertyType);
            var callSetter = Expression.Call(typedInstance, setMethod, convertedValue);
            setter = Expression.Lambda<Action<object, object?>>(
                callSetter,
                instanceParam,
                valueParam).Compile();
        }

        return new MemberAccessor(property, getter, setter);
    }
}