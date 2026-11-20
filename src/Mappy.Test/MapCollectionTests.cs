using System;
using System.Collections.Generic;
using System.Text;
namespace Mappy.Test;

public sealed class MapCollectionTests
{
    [Fact]
    public void MapCollection_List_ShouldMapCorrectly()
    {
        var source = new List<Source>
        {
            new() { Id = 1, Name = "One" },
            new() { Id = 2, Name = "Two" }
        };

        var result = source.MapCollection<Destination>();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public void MapCollection_Array_ShouldMapCorrectly()
    {
        var source = new[] { new Source { Id = 1 }, new Source { Id = 2 } };

        var result = source.MapCollection<Destination>().ToArray();

        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.Id));
    }

    [Fact]
    public void MapCollection_SharedNestedReference_ShouldPreserveIdentityAcrossItems()
    {
        var sharedNested = new Nested { Value = "Shared" };
        var items = new List<Source>
        {
            new() { Id = 1, Name = "First", Nested = sharedNested },
            new() { Id = 2, Name = "Second", Nested = sharedNested }
        };

        var result = items.MapCollection<Destination>(handleCircularReferences: true);

        Assert.Equal(2, result.Count);
        Assert.NotNull(result[0].Nested);
        Assert.Same(result[0].Nested, result[1].Nested);
    }
}