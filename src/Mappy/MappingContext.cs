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
