namespace Mappy;

/// <summary>
/// Controls how Mappy performs object mapping.
/// </summary>
public sealed class MappingOptions
{
    /// <summary>Preserves object identity when a source graph contains shared or circular references.</summary>
    public bool PreserveReferences { get; set; } = true;

    /// <summary>Includes non-public instance properties in convention-based mapping.</summary>
    public bool IncludeNonPublicProperties { get; set; } = true;

    /// <summary>Allows registered/custom conversions in addition to direct assignment.</summary>
    public bool AllowConversions { get; set; } = true;

    /// <summary>Ignores null source values when mapping onto an existing destination.</summary>
    public bool IgnoreNullValues { get; set; } = false;
}
