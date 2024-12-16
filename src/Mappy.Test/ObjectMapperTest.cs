namespace Mappy.Test;

public class ObjectMapperTest
{
    [Fact]
    public void Map_ShouldMapPropertiesCorrectly()
    {
        // Arrange
        var source = new Source { Id = 1, Name = "Test" };

        // Act
        var destination = source.Map<Destination>();

        // Assert
        Assert.Equal(source.Id, destination.Id);
        Assert.Equal(source.Name, destination.Name);
    }

    [Fact]
    public void MapCollection_ShouldMapCollectionPropertiesCorrectly()
    {
        // Arrange
        var sourceList = new List<Source>
            {
                new Source { Id = 1, Name = "Test1" },
                new Source { Id = 2, Name = "Test2" }
            };

        // Act
        var destinationList = sourceList.MapCollection<Destination>();

        // Assert
        Assert.Equal(sourceList.Count, destinationList.Count);
        for (int i = 0; i < sourceList.Count; i++)
        {
            Assert.Equal(sourceList[i].Id, destinationList[i].Id);
            Assert.Equal(sourceList[i].Name, destinationList[i].Name);
        }
    }
}

public class Source
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Destination
{
    public int Id { get; set; }
    public string Name { get; set; }
}