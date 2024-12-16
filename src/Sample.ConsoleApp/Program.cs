using Mappy;

// Basic Mapping
Console.WriteLine("1.Basic Mapping...");

var product = new Product
{
    Id = 1,
    Name = "Laptop",
    Price = 1200.50m,
    Category = new Category { Id = 10, Name = "Electronics" }
};

var productDto = product.Map<ProductDto>();
Console.WriteLine($"Product DTO: {productDto.Name}, {productDto.Category.Name}");
Console.WriteLine("");

// Mapping Collections...
Console.WriteLine("2.Mapping Collections...");

var products = new List<Product>
{
    new Product { Id = 1, Name = "Laptop", Price = 1200.50m },
    new Product { Id = 2, Name = "Phone", Price = 800.00m }
};

var productDtos = products.MapCollection<ProductDto>();
productDtos.ForEach(dto => Console.WriteLine($"{dto.Id} - {dto.Name}"));
Console.WriteLine("");

// Async Mapping with Custom Logic
Console.WriteLine("3.Async Mapping with Custom Logic...");

var product2 = new Product { Id = 1, Name = "Laptop", Price = 1200.50m };

var productDto2 = await product2.MapAsync<ProductDto>(async dto =>
{
    dto.Name = await Task.FromResult(dto.Name.ToUpper()); // Simulate async operation
});

Console.WriteLine($"Async Product DTO: {productDto2.Name}");
Console.WriteLine("");

// Mapping Nested Collections
Console.WriteLine("4.Mapping Nested Collections...");

var order = new Order
{
    Id = 100,
    CustomerName = "John Doe",
    Items = new List<OrderItem>
    {
        new OrderItem { ProductName = "Laptop", Quantity = 1 },
        new OrderItem { ProductName = "Mouse", Quantity = 2 }
    }
};

var orderDto = order.Map<OrderDto>();
Console.WriteLine($"Order DTO: {orderDto.CustomerName}");
orderDto.Items.ForEach(item => Console.WriteLine($"{item.ProductName} - {item.Quantity}"));
