# 🍁Mappy 4.0

![Static Badge](https://img.shields.io/badge/Mappy-red) 
![NuGet Version](https://img.shields.io/nuget/v/Mappy.dotNet)
![NuGet Downloads](https://img.shields.io/nuget/dt/Mappy.dotNet)
![License](https://img.shields.io/badge/license-MIT-green)

**Mappy** is a lightweight, convention-first object mapper for modern .NET.

It keeps the common case simple:

```csharp
var dto = entity.Map<EntityDto>();
```

Mappy 4.0 focuses on predictable mapping, cached mapping plans, compiled property accessors, nested objects, collections, immutable constructor-based types, circular-reference preservation, explicit configuration, converters, and mapping into existing objects.

## Why Mappy?

- Simple API with no mandatory configuration
- Cached mapping plans per source/destination pair
- Compiled property getters and setters
- Nested object mapping
- Collection and array mapping
- Constructor mapping for immutable DTOs and records
- Circular and shared-reference preservation
- Explicit property rename and ignore configuration
- Explicit type converters
- `MapTo` for updating existing objects
- Detailed `MappingException` errors
- Targets .NET 8, 9, and 10
- No external runtime dependencies

## Installation

```bash
dotnet add package Mappy.dotNet --version 4.0.0
```

## Basic mapping

```csharp
var source = new User
{
    Id = 1,
    Name = "Manoj",
    Email = "manoj@example.com"
};

var dto = source.Map<UserDto>();
```

Properties with matching names and compatible types are mapped automatically.

## Nested mapping

```csharp
var dto = order.Map<OrderDto>();
```

Mappy recursively maps compatible nested objects:

```text
Order
 └── Customer
      ↓
OrderDto
 └── CustomerDto
```

## Collections

```csharp
var result = users.MapCollection<UserDto>();
```

Arrays are also supported by the mapping engine:

```csharp
UserDto[] result = users
    .MapCollection<UserDto>()
    .ToArray();
```

Supported destination collection shapes include arrays, `List<T>`, `HashSet<T>`, and compatible generic collection interfaces such as `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, and `IReadOnlyCollection<T>`.

## Rename properties

Use strongly typed configuration when source and destination names differ:

```csharp
var config = new MappingConfiguration()
    .Map<User, UserDto>(
        source => source.FullName,
        destination => destination.Name);

var dto = user.Map<UserDto>(config: config);
```

The original string-based configuration API is also retained:

```csharp
var config = new MappingConfiguration();

config.AddPropertyMapping(
    typeof(User),
    typeof(UserDto),
    "FullName",
    "Name");
```

## Ignore properties

```csharp
var config = new MappingConfiguration()
    .Ignore<User, UserDto>(x => x.PasswordHash);

var dto = user.Map<UserDto>(config: config);
```

Or:

```csharp
config.ExcludeProperty(
    typeof(User),
    typeof(UserDto),
    "PasswordHash");
```

## Explicit converters

Mappy intentionally avoids broad implicit conversions. Register conversions explicitly:

```csharp
var config = new MappingConfiguration()
    .AddConverter<Guid, string>(
        value => value.ToString("N"));

var dto = entity.Map<EntityDto>(config: config);
```

This keeps type behavior predictable.

## Mapping into an existing object

For update scenarios:

```csharp
request.MapTo(entity);
```

Optional mapping options are supported:

```csharp
request.MapTo(
    entity,
    options: new MappingOptions
    {
        IgnoreNullValues = true
    });
```

This is useful for PATCH-style updates where null values should leave existing destination values unchanged.

## Immutable types and records

Mappy 4.0 can map constructor parameters when a destination does not have a parameterless constructor:

```csharp
public sealed class User
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

public sealed class UserDto(string name, int age)
{
    public string Name { get; } = name;
    public int Age { get; } = age;
}
```

Then:

```csharp
var dto = user.Map<UserDto>();
```

## Circular references

Mappy 4.0 preserves object identity by default.

For:

```text
Parent
 └── Child
      └── Parent
```

the mapped graph can retain the same relationship:

```csharp
var result = source.Map<Destination>();

ReferenceEquals(
    result,
    result.Child.Parent);
```

Returns `true`.

The legacy boolean API remains available:

```csharp
var result = source.Map<Destination>(
    handleCircularReferences: false);
```

For more control:

```csharp
var options = new MappingOptions
{
    PreserveReferences = true
};
```

## Private properties

For compatibility with earlier Mappy releases, non-public instance properties remain supported by default.

You can explicitly disable them:

```csharp
var options = new MappingOptions
{
    IncludeNonPublicProperties = false
};

var dto = source.Map<UserDto>();
```

## Asynchronous custom mapping

Normal object mapping is synchronous and does not introduce an unnecessary async pipeline.

Async is available when your custom mapping itself requires asynchronous work:

```csharp
var dto = await source.MapAsync<UserDto>(async destination =>
{
    destination.DisplayName =
        await GetDisplayNameAsync(source.Id);
});
```

For collections:

```csharp
var result = await users.MapCollectionAsync<UserDto>(
    async destination =>
    {
        destination.DisplayName =
            await GetDisplayNameAsync(destination.Id);
    });
```

## Type safety

Mappy maps directly assignable or explicitly supported values.

For incompatible types:

```csharp
public class Source
{
    public int Id { get; set; }
}

public class Destination
{
    public DateTime Id { get; set; }
}
```

mapping fails with a `MappingException` instead of silently performing an unexpected conversion.

For intentional conversions, register a converter.

## Performance

Mappy 4.0 separates mapping-plan construction from mapping execution.

The first mapping for a source/destination pair builds and caches metadata:

```text
Source + Destination
        ↓
Mapping plan
        ↓
Compiled property accessors
        ↓
Cached
        ↓
Fast repeated mapping
```

Property getters and setters are compiled once rather than using `PropertyInfo.GetValue` and `SetValue` for every mapped property.

For trustworthy performance comparisons, the repository includes a dedicated benchmark project. Benchmarks should be run in Release mode on the target runtime and hardware rather than relying on a single stopwatch measurement.

## Design goals

Mappy deliberately does not require:

- a dependency injection container
- a global service provider
- a large configuration system
- runtime source-generation tooling
- implicit conversion of arbitrary types
- external runtime packages

The goal is a small API with strong internals.

## Target frameworks

Mappy targets:

- .NET 8
- .NET 9
- .NET 10

## License

Mappy is released under the MIT License.
