using Mappy;

//// Basic Mapping
//Console.WriteLine("1.Basic Mapping...");

//var product = new Product
//{
//    Id = 1,
//    Name = "Laptop",
//    Price = 1200.50m,
//    Category = new Category { Id = 10, Name = "Electronics" }
//};

//var productDto = product.Map<ProductDto>();
//Console.WriteLine($"Product DTO: {productDto.Name}, {productDto.Category.Name}");
//Console.WriteLine("");

//// Mapping Collections...
//Console.WriteLine("2.Mapping Collections...");

//var products = new List<Product>
//{
//    new Product { Id = 1, Name = "Laptop", Price = 1200.50m },
//    new Product { Id = 2, Name = "Phone", Price = 800.00m }
//};

//var productDtos = products.MapCollection<ProductDto>();
//productDtos.ForEach(dto => Console.WriteLine($"{dto.Id} - {dto.Name}"));
//Console.WriteLine("");

//// Async Mapping with Custom Logic
//Console.WriteLine("3.Async Mapping with Custom Logic...");

//var product2 = new Product { Id = 1, Name = "Laptop", Price = 1200.50m };

//var productDto2 = await product2.MapAsync<ProductDto>(async dto =>
//{
//    dto.Name = await Task.FromResult(dto.Name.ToUpper()); // Simulate async operation
//});

//Console.WriteLine($"Async Product DTO: {productDto2.Name}");
//Console.WriteLine("");

//// Mapping Nested Collections
//Console.WriteLine("4.Mapping Nested Collections...");

//var order = new Order
//{
//    Id = 100,
//    CustomerName = "John Doe",
//    Items = new List<OrderItem>
//    {
//        new OrderItem { ProductName = "Laptop", Quantity = 1 },
//        new OrderItem { ProductName = "Mouse", Quantity = 2 }
//    }
//};

//var orderDto = order.Map<OrderDto>();
//Console.WriteLine($"Order DTO: {orderDto.CustomerName}");
//orderDto.Items.ForEach(item => Console.WriteLine($"{item.ProductName} - {item.Quantity}"));


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