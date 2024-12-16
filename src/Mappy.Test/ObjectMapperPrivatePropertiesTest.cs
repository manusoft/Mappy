namespace Mappy.Test;

public class ObjectMapperPrivatePropertiesTest
{
    [Fact]
    public void Map_ShouldMapPublicAndPrivateProperties()
    {
        // Arrange
        var source = new SourceClass(10, "secret");

        // Act
        var destination = source.Map<DestinationClass>();

        // Assert
        Assert.Equal(source.PublicProperty, destination.PublicProperty);
        Assert.Equal(source.GetType().GetProperty("PrivateProperty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                               .GetValue(source), destination.GetType().GetProperty("PrivateProperty").GetValue(destination));
    }
}

public class SourceClass
{
    public int PublicProperty { get; set; }
    private string PrivateProperty { get; set; }

    public SourceClass(int publicValue, string privateValue)
    {
        PublicProperty = publicValue;
        PrivateProperty = privateValue;
    }
}

public class DestinationClass
{
    public int PublicProperty { get; set; }
    public string PrivateProperty { get; set; }
}