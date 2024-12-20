using Mappy;

// Test Simple Object Mapping
var source = new Source { Id = 1, Name = "Test" };
var destination = source.Map<Destination>();
Console.WriteLine($"Simple Mapping - Source Name: {source.Name}, Destination Name: {destination.Name}");

// Test Nested Object Mapping
var nestedSource = new NestedSource { Id = 1, Inner = new InnerSource { Detail = "DetailInfo" } };
var nestedDestination = nestedSource.Map<NestedDestination>();
Console.WriteLine($"Nested Mapping - Source Detail: {nestedSource.Inner.Detail}, Destination Detail: {nestedDestination.Inner.Detail}");

// Test Collection Mapping
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

// Test Asynchronous Mapping
var asyncSource = new Source { Id = 2, Name = "AsyncTest" };
var asyncDestination = await asyncSource.MapAsync<Destination>(async d =>
{
    d.Name = await Task.FromResult(asyncSource.Name + " - Async");
});
Console.WriteLine($"Async Mapping - Source Name: {asyncSource.Name}, Destination Name: {asyncDestination.Name}");

// Performance Test
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

// Test Custom Mapping
var source4 = new Source { Id = 1, Name = "Test" };

var customDestination = source.Map<Destination>(d =>
{
    // Custom logic: Add a suffix to the Name property
    d.Name = $"{source.Name} - Custom Mapped";
});

Console.WriteLine($"Custom Mapping - Source Name: {source.Name}, Destination Name: {customDestination.Name}");

// Test Custom Asynchronous Mapping
var asyncSource4 = new Source { Id = 2, Name = "AsyncTest" };

var asyncCustomDestination = await asyncSource.MapAsync<Destination>(async d =>
{
    // Custom async logic: Simulate an async transformation
    d.Name = await Task.FromResult(source.Name + " - Async Custom");
});

Console.WriteLine($"Async Custom Mapping - Source Name: {asyncSource.Name}, Destination Name: {asyncCustomDestination.Name}");



Console.WriteLine("Press any key to exit...");
Console.ReadKey();

public class Source
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Destination
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class NestedSource
{
    public int Id { get; set; }
    public InnerSource Inner { get; set; }
}

public class InnerSource
{
    public string Detail { get; set; }
}

public class NestedDestination
{
    public int Id { get; set; }
    public InnerDestination Inner { get; set; }
}

public class InnerDestination
{
    public string Detail { get; set; }
}