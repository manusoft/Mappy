namespace Mappy;

public class MappingConfiguration
{
    public Dictionary<(Type Source, Type Destination), Dictionary<string, string>> PropertyMappings { get; } = new();
    public HashSet<(Type Source, Type Destination, string Property)> ExcludedProperties { get; } = new();

    public void AddPropertyMapping(Type sourceType, Type destType, string sourceProp, string destProp)
    {
        var key = (sourceType, destType);
        if (!PropertyMappings.ContainsKey(key))
            PropertyMappings[key] = new Dictionary<string, string>();
        PropertyMappings[key][sourceProp] = destProp;
    }

    public void ExcludeProperty(Type sourceType, Type destType, string propertyName)
    {
        ExcludedProperties.Add((sourceType, destType, propertyName));
    }
}
