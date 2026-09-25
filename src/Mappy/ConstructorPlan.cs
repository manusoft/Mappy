using System.Reflection;

namespace Mappy;

internal sealed class ConstructorPlan
{
    private readonly ConstructorInfo _constructor;
    private readonly IReadOnlyList<(ParameterInfo Parameter, MemberAccessor Source)> _parameters;

    private ConstructorPlan(
        ConstructorInfo constructor,
        IReadOnlyList<(ParameterInfo Parameter, MemberAccessor Source)> parameters)
    {
        _constructor = constructor;
        _parameters = parameters;
    }

    public HashSet<string> ParameterNames =>
        _parameters.Select(x => x.Parameter.Name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static ConstructorPlan? TryCreate(
        IReadOnlyDictionary<string, MemberAccessor> sourceMembers,
        Type destinationType)
    {
        if (destinationType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                Type.EmptyTypes,
                modifiers: null) is not null)
        {
            return null;
        }

        var constructors = destinationType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0)
                continue;

            var matches = new List<(ParameterInfo, MemberAccessor)>(parameters.Length);
            var valid = true;

            foreach (var parameter in parameters)
            {
                if (parameter.Name is null ||
                    !sourceMembers.TryGetValue(parameter.Name, out var source))
                {
                    if (parameter.IsOptional || parameter.HasDefaultValue)
                        continue;

                    valid = false;
                    break;
                }

                if (!MappingEngine.CanMapType(
                        source.Property.PropertyType,
                        parameter.ParameterType))
                {
                    valid = false;
                    break;
                }

                matches.Add((parameter, source));
            }

            if (valid && matches.Count == parameters.Length)
                return new ConstructorPlan(constructor, matches);

            // Optional parameters may be omitted. A constructor is still valid
            // when every required parameter has a matching source member.
            if (valid)
                return new ConstructorPlan(constructor, matches);
        }

        return null;
    }

    public object Create(object source, MappingContext context, MappingConfiguration? config)
    {
        var parameters = _constructor.GetParameters();
        var args = new object?[parameters.Length];
        var mappedParameters = _parameters.ToDictionary(
            x => x.Parameter.Name!,
            x => x.Source,
            StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (!mappedParameters.TryGetValue(parameter.Name!, out var sourceMember))
            {
                args[i] = parameter.HasDefaultValue
                    ? parameter.DefaultValue
                    : GetDefault(parameter.ParameterType);
                continue;
            }

            object? value;
            try
            {
                value = sourceMember.Getter(source);
            }
            catch (Exception ex)
            {
                throw new MappingException(
                    $"Unable to read constructor parameter '{parameter.Name}' while mapping " +
                    $"{source.GetType().FullName} to {_constructor.DeclaringType?.FullName}.",
                    source.GetType(), _constructor.DeclaringType, parameter.Name, ex);
            }

            if (value is null)
            {
                if (!MappingEngine.IsNullableType(parameter.ParameterType))
                {
                    throw new MappingException(
                        $"Source member '{sourceMember.Property.Name}' is null but constructor parameter " +
                        $"'{parameter.Name}' of '{_constructor.DeclaringType?.FullName}' is non-nullable.",
                        source.GetType(), _constructor.DeclaringType, parameter.Name);
                }

                args[i] = null;
                continue;
            }

            args[i] = MappingEngine.MapValue(value, parameter.ParameterType, context, config);
        }

        try
        {
            return _constructor.Invoke(args);
        }
        catch (TargetInvocationException ex)
        {
            throw new MappingException(
                $"Constructor mapping failed for '{_constructor.DeclaringType?.FullName}'.",
                sourceType: source.GetType(),
                destinationType: _constructor.DeclaringType,
                innerException: ex.InnerException ?? ex);
        }
        catch (Exception ex)
        {
            throw new MappingException(
                $"Constructor mapping failed for '{_constructor.DeclaringType?.FullName}'.",
                sourceType: source.GetType(),
                destinationType: _constructor.DeclaringType,
                innerException: ex);
        }
    }

    private static object? GetDefault(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;
}
