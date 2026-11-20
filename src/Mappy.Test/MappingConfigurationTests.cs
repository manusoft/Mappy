using System;
using System.Collections.Generic;
using System.Text;

namespace Mappy.Test;

public sealed class MappingConfigurationTests
{
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
    public void Map_IgnoreDestination_ShouldNotMapTargetedDestinationMember()
    {
        var source = new User { Name = "Manoj", Secret = "should-not-map" };
        var config = new MappingConfiguration()
            .IgnoreDestination<User, UserDto>(d => d.Secret);

        var result = source.Map<UserDto>(config: config);

        Assert.Equal("Manoj", result.Name);
        Assert.Null(result.Secret);
    }
}