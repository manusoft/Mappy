![NuGet Version](https://img.shields.io/nuget/v/Mappy.dotNet) ![NuGet Downloads](https://img.shields.io/nuget/dt/Mappy.dotNet)

# 🍁Mappy - Object Mapping
![8253945](https://github.com/user-attachments/assets/1c6efc2b-c138-4e57-a13e-4657744b556e)

## Introduction
**Mappy** is a lightweight object mapping utility for C# applications. It allows you to easily map objects between models and DTOs, handle nested objects, and map collections. This utility supports both synchronous and asynchronous mapping operations, with options for custom transformations.

---

## Features
- **Simple Object Mapping**: Map properties between objects with identical names and types.
- **Nested Object Mapping**: Automatically maps nested properties.
- **Collection Mapping**: Handles collections of objects and maps them to the destination collection type.
- **Custom Mapping**: Supports custom transformations using lambda expressions.
- **Asynchronous Mapping**: Enables async operations for custom transformations.
- **Support mapping private properties**.
- **Null Safety**: Handles null values gracefully. :🆕
- **Type Safety**: Ensures type safety by matching properties based on type rather than just name, preventing errors when types differ. :🆕
- **Circular Reference Handling**: Implements circular reference handling function. 🆕
- **Performance**: Provides good performance for typical scenarios. 🆕
  
---

## Installation
To install Mappy, you can use NuGet:

``` shell
dotnet add package Mappy.dotNet --version 2.0.0
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


