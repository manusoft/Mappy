namespace Mappy.Test;

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

public sealed class EmployeeSource
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? IdentityUserId { get; set; }
    public DateOnly? JoiningDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public RecordStatus Status { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
}

public sealed record EmployeeListResponse(
    string Id,
    string Code,
    string Name,
    string? Designation,
    string? Department,
    string? Email,
    string? Phone,
    string? IdentityUserId,
    DateOnly? JoiningDate,
    DateOnly? TerminationDate,
    RecordStatus Status,
    string ConcurrencyStamp);

public enum RecordStatus
{
    Active,
    Inactive
}

public sealed class SpeciesSource
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public SpeciesCategory Category { get; set; }
}

public enum SpeciesCategory
{
    Broiler,
    Layer
}

public sealed record SpeciesLookupItem(
    string Id,
    string Code,
    string Name,
    string Category);

public sealed class TimeSource
{
    public TimeOnly StartTime { get; set; }
}

public sealed class TimeDestination
{
    public TimeOnly StartTime { get; set; }
}

public sealed class ParameterlessOnlyDestination
{
    public ParameterlessOnlyDestination()
    {
    }

    public int Id { get; set; }
    public string? Name { get; set; }
}

public sealed class NoParameterlessOrMatchingConstructor
{
    public NoParameterlessOrMatchingConstructor(Guid requiredUnmatchedId)
    {
        RequiredUnmatchedId = requiredUnmatchedId;
    }

    public Guid RequiredUnmatchedId { get; }
}

public sealed class EnrichedItemSource
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed record EnrichedItemResponse(
    string Id,
    string Code,
    string Name,
    decimal AvailableQuantity)
{
    public EnrichedItemResponse() : this(string.Empty, string.Empty, string.Empty, 0m)
    {
    }
}

public sealed class NumericSource
{
    public int IntValue { get; set; }
    public decimal DecimalValue { get; set; }
}

public sealed class NumericDestination
{
    public decimal IntValue { get; set; }
    public int DecimalValue { get; set; }
}

public sealed record LineSource(int LineNo, string Label);

public sealed record LineResponse(int LineNo, string Label);

public sealed class OrderWithReadOnlyLinesSource
{
    public string OrderId { get; set; } = "";
    public IReadOnlyCollection<LineSource> Lines { get; set; } = [];
}

public sealed record OrderWithListLinesResponse(
    string OrderId,
    List<LineResponse> Lines);

public sealed record OrderWithArrayLinesResponse(
    string OrderId,
    LineResponse[] Lines);