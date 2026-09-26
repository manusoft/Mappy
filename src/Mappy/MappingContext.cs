using System.Runtime.CompilerServices;

namespace Mappy;

internal sealed class MappingContext
{
    private readonly Dictionary<object, object> _references =
        new(ReferenceEqualityComparer.Instance);

    public MappingContext(MappingOptions options)
    {
        Options = options;
    }

    public MappingOptions Options { get; }

    public bool TryGet(object source, out object? destination) =>
        _references.TryGetValue(source, out destination);

    public void Track(object source, object destination)
    {
        if (Options.PreserveReferences)
            _references[source] = destination;
    }

    public void Untrack(object source)
    {
        if (Options.PreserveReferences)
            _references.Remove(source);
    }
}

internal sealed class ReferenceEqualityComparer : IEqualityComparer<object?>
{
    public static ReferenceEqualityComparer Instance { get; } = new();

    private ReferenceEqualityComparer() { }

    bool IEqualityComparer<object?>.Equals(object? x, object? y) => ReferenceEquals(x, y);

    int IEqualityComparer<object?>.GetHashCode(object? obj) => obj is null ? 0 : RuntimeHelpers.GetHashCode(obj);
}
