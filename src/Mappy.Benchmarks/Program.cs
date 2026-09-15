using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Mappy;

BenchmarkRunner.Run<MappingBenchmarks>();

public class MappingBenchmarks
{
    private readonly List<User> _users =
        Enumerable.Range(1, 10_000)
            .Select(i => new User
            {
                Id = i,
                Name = $"User {i}",
                Address = new Address
                {
                    City = "Abu Dhabi",
                    Country = "UAE"
                }
            })
            .ToList();

    [Benchmark]
    public UserDto Single() => _users[0].Map<UserDto>();

    [Benchmark]
    public List<UserDto> Collection() => _users.MapCollection<UserDto>();
}

public sealed class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Address Address { get; set; } = new();
}

public sealed class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AddressDto Address { get; set; } = new();
}

public sealed class Address
{
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
}

public sealed class AddressDto
{
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
}
