namespace Mappy.Test;

public sealed class MapAsyncTests
{
    [Fact]
    public async Task MapAsync_CustomMapping_ShouldApplyCorrectly()
    {
        var source = new Source { Id = 1, Name = "AsyncTest" };

        var destination = await source.MapAsync<Destination>(async d =>
        {
            await Task.Yield();
            d.Name += " - Async";
        });

        Assert.Equal("AsyncTest - Async", destination.Name);
    }

    [Fact]
    public async Task MapCollectionAsync_ShouldMapCorrectly()
    {
        var source = new List<Source>
        {
            new() { Id = 1 },
            new() { Id = 2 }
        };

        var result = await source.MapCollectionAsync<Destination>(
            d => Task.CompletedTask);

        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.Id));
    }
}