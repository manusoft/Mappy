namespace Mappy.Test;

public sealed class MapTests
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
        Assert.Equal(source.Nested.Value, destination.Nested!.Value);
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
    public void Map_RecordWithNullableDateOnly_ShouldMapCorrectly()
    {
        var source = new EmployeeSource
        {
            Id = "1",
            Code = "EMP001",
            Name = "Manoj",
            Designation = "Developer",
            Department = "Engineering",
            Email = "manoj@example.com",
            Phone = "123456789",
            IdentityUserId = "identity-1",
            JoiningDate = new DateOnly(2026, 1, 15),
            TerminationDate = null,
            Status = RecordStatus.Active,
            ConcurrencyStamp = "stamp"
        };

        var result = source.Map<EmployeeListResponse>();

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.Code, result.Code);
        Assert.Equal(source.Name, result.Name);
        Assert.Equal(source.JoiningDate, result.JoiningDate);
        Assert.Null(result.TerminationDate);
        Assert.Equal(source.Status, result.Status);
    }

    [Fact]
    public void Map_RecordWithEnumToStringConstructorParameter_ShouldMapCorrectly()
    {
        var source = new SpeciesSource
        {
            Id = "species-1",
            Code = "BRO",
            Name = "Broiler",
            Category = SpeciesCategory.Broiler
        };

        var result = source.Map<SpeciesLookupItem>();

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.Code, result.Code);
        Assert.Equal(source.Name, result.Name);
        Assert.Equal(nameof(SpeciesCategory.Broiler), result.Category);
    }

    [Fact]
    public void Map_TimeOnly_ShouldMapCorrectly()
    {
        var source = new TimeSource { StartTime = new TimeOnly(8, 30) };

        var result = source.Map<TimeDestination>();

        Assert.Equal(source.StartTime, result.StartTime);
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
    public void Map_PrivateProperties_ShouldRemainSupported()
    {
        var source = new PrivateSource(10, "secret");

        var result = source.Map<PrivateDestination>();

        Assert.Equal(10, result.PublicValue);
        Assert.Equal("secret", result.GetPrivateValue());
    }

    [Fact]
    public void Map_ParameterlessConstructorOnly_ShouldMapCorrectly()
    {
        var source = new Source { Id = 7, Name = "Parameterless" };

        var result = source.Map<ParameterlessOnlyDestination>();

        Assert.Equal(7, result.Id);
        Assert.Equal("Parameterless", result.Name);
    }

    [Fact]
    public void Map_TypeWithoutParameterlessConstructorOrMatchingConstructor_ShouldThrowMappingException()
    {
        var source = new Source { Id = 1, Name = "Test" };

        Assert.Throws<MappingException>(() => source.Map<NoParameterlessOrMatchingConstructor>());
    }

    [Fact]
    public void Map_RecordWithParameterlessAndPrimaryConstructor_ShouldMapAndAllowWithOverride()
    {
        var source = new EnrichedItemSource { Id = "item-1", Code = "ITM-01", Name = "Feed Bag" };

        var result = source.Map<EnrichedItemResponse>() with { AvailableQuantity = 25.5m };

        Assert.Equal("item-1", result.Id);
        Assert.Equal("ITM-01", result.Code);
        Assert.Equal("Feed Bag", result.Name);
        Assert.Equal(25.5m, result.AvailableQuantity);
    }

    [Fact]
    public void Map_UnmatchedConstructorParameter_ShouldDefaultAndSupportWithOverride()
    {
        var source = new EnrichedItemSource { Id = "item-2", Code = "ITM-02", Name = "Waterer" };

        var result = source.Map<EnrichedItemResponse>() with { AvailableQuantity = 12m };

        Assert.Equal("item-2", result.Id);
        Assert.Equal(12m, result.AvailableQuantity);
    }

    [Fact]
    public void Map_NullSource_ShouldThrowArgumentNullException()
    {
        Source? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.Map<Destination>());
    }

    [Fact]
    public void MapTo_WithIgnoreNullValues_ShouldLeaveDestinationValueWhenSourceMemberIsNull()
    {
        var source = new User { Name = "OnlyName", Secret = null };
        var destination = new UserDto { Name = "Initial", Secret = "keep-this" };
        var options = new MappingOptions { IgnoreNullValues = true };

        source.MapTo(destination, options: options);

        Assert.Equal("OnlyName", destination.Name);
        Assert.Equal("keep-this", destination.Secret);
    }

    [Fact]
    public void Map_NumericConversions_ShouldMapBetweenIntAndDecimal()
    {
        var source = new NumericSource { IntValue = 15, DecimalValue = 99.75m };

        var result = source.Map<NumericDestination>();

        Assert.Equal(15m, result.IntValue);
        Assert.Equal(100, result.DecimalValue);
    }

    [Fact]
    public void Map_ReadOnlyCollectionToListAndArrayConstructorParameters_ShouldMapCorrectly()
    {
        IReadOnlyCollection<LineSource> lines =
        [
            new LineSource(1, "Line A"),
            new LineSource(2, "Line B")
        ];

        var source = new OrderWithReadOnlyLinesSource
        {
            OrderId = "ORD-100",
            Lines = lines
        };

        var listResult = source.Map<OrderWithListLinesResponse>();
        var arrayResult = source.Map<OrderWithArrayLinesResponse>();

        Assert.Equal("ORD-100", listResult.OrderId);
        Assert.Equal(2, listResult.Lines.Count);
        Assert.Equal("Line A", listResult.Lines[0].Label);

        Assert.Equal("ORD-100", arrayResult.OrderId);
        Assert.Equal(2, arrayResult.Lines.Length);
        Assert.Equal("Line B", arrayResult.Lines[1].Label);
    }
}