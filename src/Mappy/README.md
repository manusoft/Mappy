![NuGet Version](https://img.shields.io/nuget/v/Mappy.dotNet) ![.NET](https://img.shields.io/badge/.NET%20%7C%208%20%7C%209%20%7C%2010-blueviolet)

# 🍁Mappy - Object Mapping

## Introduction
**Mappy** is a lightweight object mapping utility for C# applications. It allows you to easily map objects between models and DTOs, handle nested objects, and map collections. This utility supports both synchronous and asynchronous mapping operations, with options for custom transformations.

---

## Features
- **Simple Object Mapping**: Map properties between objects with identical names and types.
- **Nested Object Mapping**: Automatically maps nested properties.
- **Collection Mapping**: Handles collections of objects and maps them to the destination collection type.
- **Custom Mapping**: Supports custom transformations using lambda expressions.
- **Asynchronous Mapping**: Enables async operations for custom transformations.
- **Null Safety**: Handles null values gracefully.
- **Support mapping private properties**.
- **Type Safety**: Ensures type safety by matching properties based on type rather than just name, preventing errors when types differ.
- **Circular Reference Handling**: Implements circular reference handling function. 
- **Improved Type Checking**: Use `IsAssignableTo` for more robust type compatibility checks. 🆕
- **Caching Reflection Data**: Cache `PropertyInfo` to reduce reflection overhead. 🆕
- **Better Collection Handling**: Support additional collection types (e.g., arrays, `ICollection<T>`). 🆕
- **Error Handling**: Add detailed exceptions for common failure cases. 🆕
- **Configuration Options**: Introduce a mapping configuration for custom property mappings or exclusions 🆕
- **Code Organization**: Split into smaller methods for better readability and maintainability. 🆕
- **Performance Optimization**: Minimize unnecessary object creations and improve circular reference handling. 🆕
---

### Key Enhancements and Refactorings:
1. **Mapping Configuration**:
   - Added `MappingConfiguration` to allow custom property mappings (e.g., mapping `SourcePropA` to `DestPropB`) and property exclusions.
   - Example usage:
     ```csharp
     var config = new MappingConfiguration();
     config.AddPropertyMapping(typeof(Source), typeof(Destination), "SourceName", "DestName");
     config.ExcludeProperty(typeof(Source), typeof(Destination), "IgnoreProp");
     var result = source.Map<Destination>(config: config);
     ```

2. **Performance Improvements**:
   - Cached `PropertyInfo` using `ConcurrentDictionary` to reduce reflection overhead.
   - Used `ReferenceEqualityComparer` for `HashSet<object>` to optimize circular reference checks.
   - Minimized object allocations in critical paths.

3. **Better Collection Handling**:
   - Added support for arrays in `MapCollection` by converting `List<T>` to `T[]` when needed.
   - Improved handling of non-generic and generic collections.

4. **Improved Type Safety**:
   - Replaced `IsAssignableFrom` with `IsAssignableTo` for clearer type compatibility checks.
   - Added detailed exception messages for type mismatches and instantiation failures.

5. **Code Organization**:
   - Split `MapProperties` into smaller methods (`MapProperty`) for better readability.
   - Consolidated common logic across sync and async methods.

6. **Error Handling**:
   - Wrapped property mapping in try-catch blocks to provide context-specific error messages.
   - Ensured null checks for created instances.

## Installation
To install Mappy, you can use NuGet:

``` shell
dotnet add package Mappy.dotNet --version 3.0.0
```

## Usage

### 1. Simple Mapping
```csharp
var source = new Source { Id = 1, Name = "Test" };
var destination = source.Map<Destination>();
Console.WriteLine($"Simple Mapping - Source Name: {source.Name}, Destination Name: {destination.Name}");
```
``` shell
Simple Mapping - Source Name: Test, Destination Name: Test
```

### 2. Nested Object Mapping
```csharp
var nestedSource = new NestedSource { Id = 1, Inner = new InnerSource { Detail = "DetailInfo" } };
var nestedDestination = nestedSource.Map<NestedDestination>();
Console.WriteLine($"Nested Mapping - Source Detail: {nestedSource.Inner.Detail}, Destination Detail: {nestedDestination.Inner.Detail}");
```
``` shell
Nested Mapping - Source Detail: DetailInfo, Destination Detail: DetailInfo
```

### 3. Collection Mapping
```csharp
var sourceList = new List<Source>
            {
                new Source { Id = 1, Name = "Item1" },
                new Source { Id = 2, Name = "Item2" }
            };
var destinationList = sourceList.MapCollection<Destination>();
Console.WriteLine("Collection Mapping:");
foreach (var item in destinationList)
{
    Console.WriteLine($"Source Name: {item.Name}");
}
```
``` shell
Collection Mapping:
Source Name: Item1
Source Name: Item2
```

### 4. Async Mapping
```csharp
var asyncSource = new Source { Id = 2, Name = "AsyncTest" };
var asyncDestination = await asyncSource.MapAsync<Destination>(async d =>
{
    d.Name = await Task.FromResult(asyncSource.Name + " - Async");
});
Console.WriteLine($"Async Mapping - Source Name: {asyncSource.Name}, Destination Name: {asyncDestination.Name}");
```
``` shell
Async Mapping - Source Name: AsyncTest, Destination Name: AsyncTest - Async
```

### 5. Custom Mapping
```csharp
var source4 = new Source { Id = 1, Name = "Test" };

var customDestination = source.Map<Destination>(d =>
{
    // Custom logic: Add a suffix to the Name property
    d.Name = $"{source.Name} - Custom Mapped";
});

Console.WriteLine($"Custom Mapping - Source Name: {source.Name}, Destination Name: {customDestination.Name}");
```
``` shell
Custom Mapping - Source Name: Test, Destination Name: Test - Custom Mapped
```


### 6. Asynchronous Custom Mapping
```csharp
var asyncSource4 = new Source { Id = 2, Name = "AsyncTest" };

var asyncCustomDestination = await asyncSource.MapAsync<Destination>(async d =>
{
    // Custom async logic: Simulate an async transformation
    d.Name = await Task.FromResult(source.Name + " - Async Custom");
});

Console.WriteLine($"Async Custom Mapping - Source Name: {asyncSource.Name}, Destination Name: {asyncCustomDestination.Name}");
```
``` shell
Async Custom Mapping - Source Name: AsyncTest, Destination Name: Test - Async Custom
```

### 7. Type Safety :🆕
```csharp
try
{
    var source = new Source { Id = 1, Name = "Test" };
    var invalidDestination = source.Map<InvalidDestination>(); // Will throw InvalidOperationException due to type mismatch
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Type Safety Error: {ex.Message}");
}
```

### 8. Performance Test (Simple & Async) :🆕
```csharp
int count = 10000;
var largeSourceList = Enumerable.Range(1, count).Select(i => new Source { Id = i, Name = "Test" }).ToList();

// Measuring Simple Mapping Performance
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var largeDestinationList = largeSourceList.MapCollection<Destination>();
stopwatch.Stop();
Console.WriteLine($"Simple Collection Mapping Performance: {stopwatch.ElapsedMilliseconds} ms for {count} items");

// Measuring Asynchronous Mapping Performance
stopwatch.Restart();
var largeAsyncDestinationList = await largeSourceList.MapCollectionAsync<Destination>(async d =>
{
    d.Name = await Task.FromResult("Async - " + d.Name);
});
stopwatch.Stop();
Console.WriteLine($"Asynchronous Collection Mapping Performance: {stopwatch.ElapsedMilliseconds} ms for {count} items");
```
``` shell
Simple Collection Mapping Performance: 18 ms for 10000 items
Asynchronous Collection Mapping Performance: 29 ms for 10000 items
```
---

## Performance Considerations
While Mappy is effective for typical scenarios, it may not be optimized for very large datasets or scenarios with high-frequency mapping needs. The performance of the library could be impacted by reflection-based operations, especially for large collections and complex nested mappings.

## Contributing
Contributions to Mappy are welcome! If you find any issues or have suggestions for improvements, feel free to create a pull request or report an issue.

## License
This project is open source and can be freely modified and distributed.


