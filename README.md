# Mappy - Object-to-Object Mapping

## Introduction
**Mappy** is a lightweight object mapping utility for C# applications. It allows you to easily map objects between models and DTOs, handle nested objects, and map collections. This utility supports both synchronous and asynchronous mapping operations, with options for custom transformations.

---

## Features
1. **Simple Object Mapping**: Map properties between objects with identical names and types.
2. **Nested Object Mapping**: Automatically maps nested properties.
3. **Collection Mapping**: Handles collections of objects and maps them to the destination collection type.
4. **Custom Mapping**: Supports custom transformations using lambda expressions.
5. **Asynchronous Mapping**: Enables async operations for custom transformations.
6. **Null Safety**: Handles null values gracefully.

---

## Installation
1. Clone the project or copy the `ObjectMapper.cs` file into your project.
2. Include the namespace `Mappy` in your files to use the mapping functionality:
   ```csharp
   using Mappy;
   ```

---

## Usage

### 1. Basic Mapping
```csharp
var product = new Product
{
    Id = 1,
    Name = "Laptop",
    Price = 1200.50m,
    Category = new Category { Id = 10, Name = "Electronics" }
};

var productDto = product.Map<ProductDto>();
Console.WriteLine($"Product DTO: {productDto.Name}, {productDto.Category.Name}");
```

### 2. Custom Mapping
```csharp
var product = new Product
{
    Id = 1,
    Name = "Laptop",
    Price = 1200.50m
};

var productDto = product.Map<ProductDto>(dto =>
{
    dto.Name = product.Name.ToUpper();
});

Console.WriteLine($"Custom Product DTO: {productDto.Name}");
```

### 3. Async Mapping
```csharp
var product = new Product { Id = 1, Name = "Laptop", Price = 1200.50m };

var productDto = await product.MapAsync<ProductDto>(async dto =>
{
    dto.Name = await Task.FromResult(dto.Name.ToUpper());
});

Console.WriteLine($"Async Product DTO: {productDto.Name}");
```

### 4. Collection Mapping
```csharp
var products = new List<Product>
{
    new Product { Id = 1, Name = "Laptop", Price = 1200.50m },
    new Product { Id = 2, Name = "Phone", Price = 800.00m }
};

var productDtos = products.MapCollection<ProductDto>();
productDtos.ForEach(dto => Console.WriteLine($"{dto.Id} - {dto.Name}"));
```

### 5. Mapping Nested Collections
```csharp
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
```

---

## Code Reference

### ObjectMapper.cs
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SimpleMapper
{
    public static class ObjectMapper
    {
        public static TDestination Map<TDestination>(this object source, Action<TDestination> customMapping = null) where TDestination : new()
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var destination = new TDestination();
            MapProperties(source, destination);

            customMapping?.Invoke(destination);

            return destination;
        }

        public static async Task<TDestination> MapAsync<TDestination>(this object source, Func<TDestination, Task> customMapping = null) where TDestination : new()
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var destination = new TDestination();
            MapProperties(source, destination);

            if (customMapping != null)
            {
                await customMapping(destination);
            }

            return destination;
        }

        public static List<TDestination> MapCollection<TDestination>(this IEnumerable source) where TDestination : new()
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var destinationList = new List<TDestination>();
            foreach (var item in source)
            {
                destinationList.Add(item.Map<TDestination>());
            }
            return destinationList;
        }

        public static async Task<List<TDestination>> MapCollectionAsync<TDestination>(this IEnumerable source, Func<TDestination, Task> customMapping = null) where TDestination : new()
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var destinationList = new List<TDestination>();
            foreach (var item in source)
            {
                var destination = await item.MapAsync(customMapping);
                destinationList.Add(destination);
            }
            return destinationList;
        }

        private static void MapProperties(object source, object destination)
        {
            var sourceType = source.GetType();
            var destinationType = destination.GetType();
            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var destinationProperties = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var sourceProp in sourceProperties)
            {
                var destProp = destinationProperties.FirstOrDefault(p => p.Name == sourceProp.Name && p.CanWrite);

                if (destProp == null) continue;

                var sourceValue = sourceProp.GetValue(source);
                if (sourceValue == null)
                {
                    destProp.SetValue(destination, null);
                    continue;
                }

                if (IsSimpleType(destProp.PropertyType))
                {
                    destProp.SetValue(destination, sourceValue);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(destProp.PropertyType) && destProp.PropertyType != typeof(string))
                {
                    var collection = MapCollection(sourceValue as IEnumerable, destProp.PropertyType);
                    destProp.SetValue(destination, collection);
                }
                else
                {
                    var nestedObject = Activator.CreateInstance(destProp.PropertyType);
                    MapProperties(sourceValue, nestedObject);
                    destProp.SetValue(destination, nestedObject);
                }
            }
        }

        private static object MapCollection(IEnumerable source, Type destinationType)
        {
            if (source == null) return null;

            var itemType = destinationType.IsGenericType
                ? destinationType.GetGenericArguments()[0]
                : destinationType.GetElementType();

            var destinationList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
            foreach (var item in source)
            {
                var mappedItem = Activator.CreateInstance(itemType);
                MapProperties(item, mappedItem);
                destinationList.Add(mappedItem);
            }

            return destinationList;
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || type.IsValueType || type == typeof(string) || type == typeof(DateTime);
        }
    }
}
```

---

## Notes
1. **Performance**: This mapper is not optimized for very large datasets or scenarios with high-frequency mapping needs. Use with caution for such cases.
2. **Limitations**:
   - Does not support mapping private properties.
   - Cannot handle circular references.

## License
This project is open source and can be freely modified and distributed.


