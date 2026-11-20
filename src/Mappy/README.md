# 🍁 Mappy - Object Mapper

![Static Badge](https://img.shields.io/badge/Mappy-red) 
![NuGet Version](https://img.shields.io/nuget/v/Mappy.dotNet)
![NuGet Downloads](https://img.shields.io/nuget/dt/Mappy.dotNet)
![License](https://img.shields.io/badge/license-MIT-green)

**Mappy** is a lightweight, convention-first object mapper for .NET with zero external runtime dependencies.

It keeps the common case simple:

```c#
var dto = entity.Map<EntityDto>();
```

Mappy focuses on predictable mapping, cached mapping plans, compiled property accessors, nested objects, collections, positional records and immutable constructor types, circular and shared-reference tracking, explicit converters, and in-place updates.

## Why Mappy?

* Zero mandatory configuration—convention mapping works out of the box
* Cached mapping plans per source, destination, and member visibility combination
* Compiled property getters and setters
* Nested object and collection/array mapping (`List<T>`, `HashSet<T>`, arrays, and compatible collection interfaces)
* Constructor and positional `record` mapping, including unmatched/extra parameters
* Circular and shared-reference preservation across graphs and collections
* Strongly typed and string-based property rename and ignore rules
* Explicit type converters plus enum and compatible numeric conversions
* `MapTo` for updating existing instances and `customMapping` / `with` for post-map enrichment
* Detailed `MappingException` diagnostics (`SourceType`, `DestinationType`, `MemberName`)
* Targets `netstandard2.0` and `net10.0` with no external runtime dependencies

## Installation

```bash
dotnet add package Mappy.dotNet --version 4.2.0

```

## Basic Mapping

Properties with matching names (case-insensitive) and compatible types are mapped automatically:

```csharp
var source = new User
{
    Id = 1,
    Name = "Manoj",
    Email = "manoj@example.com"
};

var dto = source.Map<UserDto>();

```

## Nested Mapping

Mappy recursively maps compatible nested objects:

```text
Order
 └── Customer
      ↓
OrderDto
 └── CustomerDto

```

```csharp
var dto = order.Map<OrderDto>();

```

## Collections

Map sequences into `List<T>`, arrays, `HashSet<T>`, or compatible collection interfaces:

```csharp
var result = users.MapCollection<UserDto>();
UserDto[] array = users.MapCollection<UserDto>().ToArray();

```

In Mappy 4.2, `MapCollection` and `MapCollectionAsync` share a single mapping context across items by default so shared references across elements preserve their mapped identity (`handleCircularReferences: false` disables this if needed).

## Renaming and Ignoring Properties

Use strongly typed expressions when property names differ:

```csharp
var config = new MappingConfiguration()
    .Map<User, UserDto>(
        source => source.FullName,
        destination => destination.Name)
    .Ignore<User, UserDto>(source => source.PasswordHash);

var dto = user.Map<UserDto>(config: config);

```

String-based configuration methods are also supported:

```csharp
var config = new MappingConfiguration();
config.AddPropertyMapping(typeof(User), typeof(UserDto), "FullName", "Name");
config.ExcludeProperty(typeof(User), typeof(UserDto), "PasswordHash");

```

## Explicit Converters and Conversions

Register custom type converters when you need explicit transformation logic:

```csharp
var config = new MappingConfiguration()
    .AddConverter<Guid, string>(value => value.ToString("N"));

var dto = entity.Map<EntityDto>(config: config);

```

Mappy also supports enum-to-string, string-to-enum, enum-to-primitive, `DateOnly` / `TimeOnly`, and compatible numeric conversions while throwing `MappingException` on incompatible types instead of silently guessing.

## Records, Constructors, and Post-Map Enrichment

Mappy matches constructor parameters by name for immutable classes and positional `record` types without requiring a parameterless constructor:

```csharp
public sealed record ItemResponse(
    string Id,
    string Code,
    string Name,
    decimal AvailableQuantity)
{
    public ItemResponse() : this(string.Empty, string.Empty, string.Empty, 0m)
    {
    }
}

```

When a destination has extra fields not present on the source (such as `AvailableQuantity`), they default cleanly and can be populated via `customMapping` (for mutable/settable properties) or a `with` expression (for `init`-only record properties):

```csharp
// With customMapping callback
var dto = item.Map<ItemResponse>(
    customMapping: r => r.AvailableQuantity = availableQty);

// Or with a 'with' expression on init-only record members
var immutableDto = item.Map<ItemResponse>() with { AvailableQuantity = availableQty };

```

## Mapping into an Existing Object

Update an existing destination instance with `MapTo`:

```csharp
request.MapTo(
    entity,
    options: new MappingOptions
    {
        IgnoreNullValues = true
    });

```

Setting `IgnoreNullValues = true` leaves existing destination properties untouched when source values are `null`—useful for partial/PATCH updates.

## Circular References and Private Properties

Object identity tracking is enabled by default via `MappingOptions.PreserveReferences`:

```text
Parent
 └── Child
      └── Parent

```

```csharp
var result = source.Map<Destination>();
bool isSameRoot = ReferenceEquals(result, result.Child.Parent); // true

```

Non-public instance properties are included by default for backward compatibility and can be toggled via `MappingOptions`:

```csharp
var options = new MappingOptions
{
    IncludeNonPublicProperties = false,
    PreserveReferences = true
};

```

## Asynchronous Custom Mapping

Core mapping runs synchronously. Use `MapAsync` or `MapCollectionAsync` when your post-mapping enrichment requires asynchronous calls:

```csharp
var dto = await source.MapAsync<UserDto>(async destination =>
{
    destination.DisplayName = await GetDisplayNameAsync(source.Id);
});

var list = await users.MapCollectionAsync<UserDto>(async destination =>
{
    destination.DisplayName = await GetDisplayNameAsync(destination.Id);
});

```

## Performance and Design Goals

Mappy builds and caches mapping plans with compiled expression getters and setters on first use so repeated mappings avoid per-item reflection overhead:

```text
Source + Destination + Visibility
               ↓
         MappingPlan
               ↓
   Compiled Getters / Setters
               ↓
     Cached for Reuse

```

Mappy intentionally avoids:

* DI containers or global service locators
* Heavy configuration DSLs
* Source generators or external runtime dependencies
* Silent, arbitrary type coercion

## Target Frameworks

* `.NET Standard 2.0` (`netstandard2.0`)
* `.NET 10` (`net10.0`)

## License

Mappy is released under the MIT License.
