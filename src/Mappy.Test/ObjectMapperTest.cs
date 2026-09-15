using Mappy;

namespace Mappy.Test;

public sealed class ObjectMapperTest
{
    [Fact]
    public void Map_SimpleProperties_ShouldMapCorrectly()
    {
        var source = new Source { Id = 1, Name = "Test" };

        var destination = source.Map<Destination>();

        Assert.Equal(source.Id, destination.Id);
        Assert.Equal(source.Name, destination.Name);
    }

    [Fact]
    public void Map_NestedObjects_ShouldMapCorrectly()
    {
        var source = new Source
        {
            Id = 1,
            Nested = new Nested { Value = "NestedValue" }
        };

        var destination = source.Map<Destination>();

        Assert.NotSame(source.Nested, destination.Nested);
        Assert.Equal(source.Nested.Value, destination.Nested.Value);
    }

    [Fact]
    public void Map_Collection_ShouldMapCorrectly()
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
    public void Map_Array_ShouldMapCorrectly()
    {
        var source = new[] { new Source { Id = 1 }, new Source { Id = 2 } };

        var result = source.MapCollection<Destination>().ToArray();

        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.Id));
    }

    [Fact]
    public void Map_ConfiguredRename_ShouldMapCorrectly()
    {
        var source = new RenamedSource { FullName = "Manoj" };
        var config = new MappingConfiguration()
            .Map<RenamedSource, RenamedDestination>(
                x => x.FullName,
                x => x.Name);

        var result = source.Map<RenamedDestination>(config: config);

        Assert.Equal("Manoj", result.Name);
    }

    [Fact]
    public void Map_Ignore_ShouldNotMapExcludedMember()
    {
        var source = new User { Name = "Manoj", Secret = "hidden" };
        var config = new MappingConfiguration()
            .Ignore<User, UserDto>(x => x.Secret);

        var result = source.Map<UserDto>(config: config);

        Assert.Equal("Manoj", result.Name);
        Assert.Null(result.Secret);
    }

    [Fact]
    public void Map_Converter_ShouldMapExplicitConversion()
    {
        var source = new IdentifierSource { Id = Guid.NewGuid() };
        var config = new MappingConfiguration()
            .AddConverter<Guid, string>(x => x.ToString("N"));

        var result = source.Map<IdentifierDestination>(config: config);

        Assert.Equal(source.Id.ToString("N"), result.Id);
    }

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
    public void Map_CircularReference_ShouldPreserveIdentity()
    {
        var root = new CircularSource { Name = "Root" };
        var child = new CircularSource { Name = "Child" };
        root.Children.Add(child);
        child.Children.Add(root);

        var result = root.Map<CircularDestination>();

        Assert.Same(result, result.Children[0].Children[0]);
    }

    [Fact]
    public void Map_RecordWithConstructor_ShouldMapCorrectly()
    {
        var source = new User { Name = "Manoj", Age = 42 };

        var result = source.Map<UserRecord>();

        Assert.Equal(source.Name, result.Name);
        Assert.Equal(source.Age, result.Age);
    }

    [Fact]
    public void Map_TypeMismatch_ShouldThrowMappingException()
    {
        var source = new Source { Id = 1, Name = "Test" };

        var exception = Assert.Throws<MappingException>(
            () => source.Map<InvalidDestination>());

        Assert.Contains("cannot map", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

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

    [Fact]
    public void Map_PrivateProperties_ShouldRemainSupported()
    {
        var source = new PrivateSource(10, "secret");

        var result = source.Map<PrivateDestination>();

        Assert.Equal(10, result.PublicValue);
        Assert.Equal("secret", result.GetPrivateValue());
    }
}

public sealed class Source
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public Nested? Nested { get; set; }
}

public sealed class Destination
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public Nested? Nested { get; set; }
}

public sealed class Nested
{
    public string? Value { get; set; }
}

public sealed class InvalidDestination
{
    public DateTime Id { get; set; }
    public string? Name { get; set; }
}

public sealed class RenamedSource
{
    public string FullName { get; set; } = "";
}

public sealed class RenamedDestination
{
    public string Name { get; set; } = "";
}

public sealed class User
{
    public string Name { get; set; } = "";
    public string? Secret { get; set; }
    public int Age { get; set; }
}

public sealed class UserDto
{
    public string Name { get; set; } = "";
    public string? Secret { get; set; }
}

public sealed class IdentifierSource
{
    public Guid Id { get; set; }
}

public sealed class IdentifierDestination
{
    public string Id { get; set; } = "";
}

public sealed class UserRecord(string name, int age)
{
    public string Name { get; } = name;
    public int Age { get; } = age;
}

public sealed class PrivateSource
{
    public int PublicValue { get; }
    private string PrivateValue { get; }

    public PrivateSource(int publicValue, string privateValue)
    {
        PublicValue = publicValue;
        PrivateValue = privateValue;
    }
}

public sealed class PrivateDestination
{
    public int PublicValue { get; set; }
    private string? PrivateValue { get; set; }

    public string? GetPrivateValue() => PrivateValue;
}

public sealed class CircularSource
{
    public string Name { get; set; } = "";
    public List<CircularSource> Children { get; set; } = [];
}

public sealed class CircularDestination
{
    public string Name { get; set; } = "";
    public List<CircularDestination> Children { get; set; } = [];
}
