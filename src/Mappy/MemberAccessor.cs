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

    public static MemberAccessor Create(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var castInstance = Expression.Convert(instance, property.DeclaringType!);
        var value = Expression.Property(castInstance, property);
        var getter = Expression.Lambda<Func<object, object?>>(
            Expression.Convert(value, typeof(object)), instance).Compile();

        Action<object, object?>? setter = null;
        if (property.SetMethod is not null)
        {
            var valueParameter = Expression.Parameter(typeof(object), "value");
            var convertedValue = Expression.Convert(valueParameter, property.PropertyType);
            setter = Expression.Lambda<Action<object, object?>>(
                Expression.Assign(value, convertedValue), instance, valueParameter).Compile();
        }

        return new MemberAccessor(property, getter, setter);
    }
}
