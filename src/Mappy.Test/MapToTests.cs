namespace Mappy.Test;

public sealed class MapToTests
{
    [Fact]
    public void MapTo_ShouldUpdateExistingDestination()
    {
        var source = new User { Name = "Updated", Secret = "new-secret" };
        var destination = new UserDto { Name = "Old", Secret = "old-secret" };

        source.MapTo(destination);

        Assert.Equal("Updated", destination.Name);
        Assert.Equal("new-secret", destination.Secret);
    }

    [Fact]
    public void MapTo_WithIgnoreNullValues_ShouldPreserveExistingDestinationValuesForNullSourceMembers()
    {
        var source = new User { Name = "PatchedName", Secret = null };
        var destination = new UserDto { Name = "OriginalName", Secret = "keep-me" };
        var options = new MappingOptions { IgnoreNullValues = true };

        source.MapTo(destination, options: options);

        Assert.Equal("PatchedName", destination.Name);
        Assert.Equal("keep-me", destination.Secret);
    }
}