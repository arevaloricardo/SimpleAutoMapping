# SimpleAutoMapping Documentation

> [Para leer la documentación en español, consulta aquí](README.es.md)

`SimpleAutoMapping` is a lightweight object mapping library for .NET, designed to simplify the process of mapping properties between objects. It is licensed under the **MIT License**, which allows for free modification, distribution, and commercialization, provided that the original copyright notice and permission notice are included in all copies or substantial portions of the software. The software is provided "as is", without warranty of any kind.

For complete license details, see the [LICENSE](LICENSE) file in the repository.

---

## Table of Contents

- [Installation](#installation)
- [Usage Approaches](#usage-approaches)
  - [Static Mapping (Mapper class)](#static-mapping-mapper-class)
  - [Dependency Injection (ISimpleAutoMapping)](#dependency-injection-isimplemapping)
- [Configuration Options](#configuration-options)
- [Mapping Profiles](#mapping-profiles)
- [Extension Methods](#extension-methods)
- [Advanced Features](#advanced-features)
- [Examples](#examples)
- [Performance Optimization](#performance-optimization)

---

## Installation

To use `SimpleAutoMapping` in your .NET project, install it via NuGet:

```bash
dotnet add package SimpleAutoMapping
```

Alternatively, clone the repository from GitHub and reference the project directly in your solution.

---

## Usage Approaches

SimpleAutoMapping offers two main ways to use the library - via static methods or dependency injection. Each approach has its benefits depending on your application architecture.

### Static Mapping (Mapper class)

The static `Mapper` class provides direct access to mapping functionality without dependency injection. This is ideal for console applications, utilities, or when you prefer a simpler approach.

```csharp
// Basic mapping
var source = new Source { Name = "John", Age = 30 };
var destination = Mapper.Map<Source, Destination>(source);

// Mapping with configuration
var options = new MappingOptions<Source, Destination>()
    .ConfigProperty(s => s.Name, d => d.FullName);
var result = Mapper.Map(source, null, options);

// Partial mapping (PATCH behavior)
var existingDestination = new Destination { Name = "John", Age = 30 };
var patchSource = new Source { Name = "Jane" }; // Age is null
Mapper.PartialMap(patchSource, existingDestination);
// Result: existingDestination = { Name = "Jane", Age = 30 }
```

### Dependency Injection (ISimpleAutoMapping)

For applications using dependency injection (like ASP.NET Core), the `ISimpleAutoMapping` interface offers integration with DI containers and a more testable approach.

**Configuration in Program.cs:**

```csharp
// Register SimpleAutoMapping in your service collection
builder.Services.AddSimpleAutoMapping(config => 
{
    config.AddProfile<UserMappingProfile>();
});
```

**Usage in Services:**

```csharp
public class UserService
{
    private readonly ISimpleAutoMapping _mapper;

    public UserService(ISimpleAutoMapping mapper)
    {
        _mapper = mapper;
    }

    public UserDto GetUser(User user)
    {
        // Map user to DTO
        return _mapper.MapTo<UserDto>(user);
    }

    public void UpdateUser(UserUpdateDto dto, User existingUser)
    {
        // Update only non-null properties
        _mapper.PartialMapTo(dto, existingUser);
    }
    
    public User CreateUserFromDto(UserCreateDto dto)
    {
        // Create new user
        return _mapper.MapTo<User>(dto);
        
        // Or for partial mapping with null handling
        // return _mapper.PartialMapTo<User>(dto);
    }
}
```

**Available Methods:**

The `ISimpleAutoMapping` interface provides several methods to handle different mapping scenarios:

```csharp
// Full mapping methods (PUT behavior - propagates nulls)
TDestination Map<TSource, TDestination>(TSource source, TDestination destination = default);
TDestination Map<TDestination>(object source);
TDestination MapTo<TDestination>(object source);
TDestination MapTo<TDestination>(object source, TDestination destination);

// Partial mapping methods (PATCH behavior - ignores nulls)
TDestination PartialMap<TSource, TDestination>(TSource source, TDestination destination);
TDestination PartialMap<TDestination>(object source, TDestination destination);
TDestination PartialMapTo<TDestination>(object source);
TDestination PartialMapTo<TDestination>(object source, TDestination destination);
```

---

## Configuration Options

The `MappingOptions<TSource, TDestination>` class lets you customize the mapping process. Key options include:

- **IgnoreNullValues**: Ignores null values from the source (default: `false`).
- **MapNestedObjects**: Enables mapping of nested objects (default: `true`).
- **PropertyMappings**: Maps properties with different names.
- **ValueTransformers**: Applies transformations to property values.
- **IgnoreProperties**: Excludes specific properties from mapping.
- **CustomResolvers**: Allows using custom resolvers to determine values.

**Example: Custom Configuration**

```csharp
var options = new MappingOptions<Source, Destination>()
    .IgnoreNulls()
    .ConfigProperty(s => s.Name, d => d.FullName)
    .IgnoreProperty(s => s.Age)
    .AddResolver(d => d.FullAddress, s => $"{s.Street}, {s.City}");

var destination = Mapper.Map(source, null, options);
// Result: destination = { FullName = "John", FullAddress = "123 Main St, Springfield" }
```

---

## Mapping Profiles

For reusable mapping configurations, create a subclass of `SimpleAutoMappingProfile`. Define mappings within the profile and register it globally.

**Example: Defining a Profile**

```csharp
public class UserMappingProfile : SimpleAutoMappingProfile
{
    public UserMappingProfile()
    {
        // Configure a complete map (will propagate nulls by default)
        CreateMap<User, UserDto>()
            .ConfigProperty(s => s.Name, d => d.FullName)
            .IgnoreProperty(s => s.Password);
            
        // Configure a one-way mapping for create operations
        CreateMap<UserCreateDto, User>()
            .ConfigTransform(s => s.BirthDate, date => date.Date);
            
        // Configure an explicit partial map (though not necessary)
        CreatePartialMap<UserUpdateDto, User>()
            .ConfigProperty(s => s.Name, d => d.FullName);
            
        // Note: You don't need to explicitly define partial maps
        // PartialMapTo will automatically use the configuration from CreateMap
        // but with IgnoreNullValues = true
    }
}
```

**Registering Profiles**

```csharp
// Static registration
Mapper.Configuration.AddProfile<UserMappingProfile>();

// Or in a DI environment
services.AddSimpleAutoMapping(config => 
{
    config.AddProfile<UserMappingProfile>();
});

// Multiple profiles
Mapper.Configuration.AddProfiles(
    typeof(UserMappingProfile),
    typeof(ProductMappingProfile)
);

// From an assembly
Mapper.Configuration.AddProfilesFromAssembly(typeof(Program).Assembly);
```

---

## Extension Methods

SimpleAutoMapping provides extension methods to simplify mapping when you have already configured your profiles:

```csharp
// Map to a new destination object
var source = new Source { Name = "John", Age = 30 };
var destination = source.MapTo<Destination>();

// Map to an existing destination object
var existingDestination = new Destination { Name = "Bob", Age = 25 };
source.MapTo(existingDestination);

// Partial map to a new destination object (ignores nulls)
var partialSource = new Source { Name = "Jane" }; // Age is null
var newObject = partialSource.PartialMapTo<Destination>();

// Partial map to an existing destination object (ignores nulls)
partialSource.PartialMapTo(existingDestination);
```

These methods automatically use the configured profiles, eliminating the need to repeatedly specify types.

---

## Advanced Features

### Nested Object Mapping

Automatically maps nested objects when `MapNestedObjects` is enabled (default: `true`).

```csharp
public class Source { public string Name { get; set; } public NestedSource Nested { get; set; } }
public class NestedSource { public int Value { get; set; } }
public class Destination { public string Name { get; set; } public NestedDestination Nested { get; set; } }
public class NestedDestination { public int Value { get; set; } }

var source = new Source { Name = "John", Nested = new NestedSource { Value = 42 } };
var destination = Mapper.Map<Source, Destination>(source);
// Result: destination = { Name = "John", Nested = { Value = 42 } }
```

### Polymorphic Mapping

SimpleAutoMapping supports polymorphic mapping, allowing you to map a base type to different derived destination types based on the actual runtime type of the source object or based on conditions.

```csharp
// Inheritance structure
public class Animal { public string Name { get; set; } }
public class Dog : Animal { public string Breed { get; set; } public bool HasPedigree { get; set; } }

public class AnimalDto { public string Name { get; set; } }
public class DogDto : AnimalDto { public string Breed { get; set; } }
public class PetDogDto : AnimalDto { public string Breed { get; set; } public bool IsFamilyPet { get; set; } = true; }

// Configure mappings
public class AnimalMappingProfile : SimpleAutoMappingProfile
{
    public AnimalMappingProfile()
    {
        // Base mappings
        CreateMap<Animal, AnimalDto>();
        CreateMap<Dog, DogDto>();
        CreateMap<Dog, PetDogDto>();
        
        // Register polymorphic mappings
        // This configures that when mapping from Animal to AnimalDto, 
        // if the source is actually a Dog, use DogDto as target
        Mapper.Configuration.Include<Animal, Dog, DogDto>();
        
        // You can also add conditions to determine which derived type to use
        // Map to PetDogDto when Dog doesn't have pedigree
        Mapper.Configuration.Include<Animal, Dog, PetDogDto>(dog => !dog.HasPedigree);
        
        // Map to DogDto when Dog has pedigree
        Mapper.Configuration.Include<Animal, Dog, DogDto>(dog => dog.HasPedigree);
    }
}

// Usage
var pedigreeDog = new Dog { Name = "Rex", Breed = "German Shepherd", HasPedigree = true };
var petDog = new Dog { Name = "Buddy", Breed = "Mixed", HasPedigree = false };

// When mapping from Animal to AnimalDto, correct derived types are selected automatically
var pedigreeDogDto = Mapper.Map<Animal, AnimalDto>(pedigreeDog); // Results in DogDto
var petDogDto = Mapper.Map<Animal, AnimalDto>(petDog); // Results in PetDogDto

// Verifying types
Console.WriteLine(pedigreeDogDto.GetType().Name); // Outputs: DogDto
Console.WriteLine(petDogDto.GetType().Name); // Outputs: PetDogDto
```

### Collection Mapping

SimpleAutoMapping supports mapping complete collections, including `List<T>`, arrays, and other `IEnumerable<T>` implementations. You only need to define mappings for the element types - collection mapping is handled automatically.

```csharp
// Define mapping for element types only
CreateMap<User, UserDto>();

// The collection mapping is handled automatically
var users = new List<User> 
{
    new User { Id = 1, Name = "John" },
    new User { Id = 2, Name = "Jane" }
};

// Map list to list
var dtos = Mapper.Map<List<User>, List<UserDto>>(users);

// Or with extension methods
var dtos2 = users.MapTo<List<UserDto>>();

// Or with dependency injection
var dtos3 = _mapper.MapTo<List<UserDto>>(users);

// It also works with properties that are collections
public class SourceModel { public List<Item> Items { get; set; } }
public class DestModel { public List<ItemDto> Items { get; set; } }

// Just map the container class - nested collections are handled automatically
CreateMap<SourceModel, DestModel>();
CreateMap<Item, ItemDto>();
```

### Value Transformations

Transform property values during mapping.

```csharp
var options = new MappingOptions<Source, Destination>()
    .ConfigTransform(s => s.Age, age => age + 1);

var source = new Source { Name = "John", Age = 30 };
var destination = Mapper.Map(source, null, options);
// Result: destination = { Name = "John", Age = 31 }
```

### Custom Resolvers

Use custom resolvers to calculate specific values during mapping.

```csharp
var options = new MappingOptions<Source, Destination>()
    .AddResolver(d => d.FullName, s => $"{s.FirstName} {s.LastName}")
    .AddResolver("Summary", s => GenerateSummary(s));

var source = new Source { FirstName = "John", LastName = "Doe" };
var destination = Mapper.Map(source, null, options);
// Result: destination = { FullName = "John Doe", Summary = "..." }
```

### PUT vs PATCH Behavior

SimpleAutoMapping has two main mapping behaviors:

- **PUT behavior** (propagates null values):
  - `Map` and `MapTo` methods
  - Equivalent to a full resource update
  - Sets destination properties to null when source properties are null

- **PATCH behavior** (ignores null values):
  - `PartialMap` and `PartialMapTo` methods
  - Equivalent to a partial resource update
  - Leaves destination properties unchanged when source properties are null

```csharp
// PUT behavior
var putSource = new Source { Name = "John", Age = null };
var destination = new Destination { Name = "Bob", Age = 25 };
Mapper.Map(putSource, destination);
// Result: destination = { Name = "John", Age = null }

// PATCH behavior
var patchSource = new Source { Name = "John", Age = null };
var destination = new Destination { Name = "Bob", Age = 25 };
Mapper.PartialMap(patchSource, destination);
// Result: destination = { Name = "John", Age = 25 }
```

### Custom Type Converters

Register custom converters for specific types:

```csharp
// Register a string to DateTime converter
Mapper.Configuration.RegisterTypeConverter<string, DateTime>(str => 
    DateTime.TryParse(str, out var date) ? date : DateTime.MinValue);

// Register a custom converter for a complex type
Mapper.Configuration.RegisterTypeConverter<AddressModel, AddressDto>(
    address => new AddressDto 
    { 
        FullAddress = $"{address.Street}, {address.City} {address.ZipCode}" 
    });
```

---

## Examples

### 1. Basic Complete Mapping

```csharp
public class Source { public string Name { get; set; } public int Age { get; set; } }
public class Destination { public string Name { get; set; } public int Age { get; set; } }

var source = new Source { Name = "John", Age = 30 };
var destination = Mapper.Map<Source, Destination>(source);
// Result: destination = { Name = "John", Age = 30 }
```

### 2. Dependency Injection with Profiles

```csharp
// Profile
public class UserMappingProfile : SimpleAutoMappingProfile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<UserUpdateDto, User>();
    }
}

// DI configuration in Program.cs
builder.Services.AddSimpleAutoMapping(config => 
    config.AddProfile<UserMappingProfile>());

// Service
public class UserService
{
    private readonly ISimpleAutoMapping _mapper;
    private readonly IUserRepository _repository;

    public UserService(ISimpleAutoMapping mapper, IUserRepository repository)
    {
        _mapper = mapper;
        _repository = repository;
    }

    public async Task<UserDto> GetUser(int id)
    {
        var user = await _repository.GetById(id);
        return _mapper.MapTo<UserDto>(user);
    }

    public async Task UpdateUser(int id, UserUpdateDto updateDto)
    {
        var user = await _repository.GetById(id);
        // Only update non-null fields (PATCH behavior)
        _mapper.PartialMapTo(updateDto, user);
        await _repository.Update(user);
    }
}
```

### 3. Collection Mapping with Custom Configuration

```csharp
// Configure element mapping
public class ProductMappingProfile : SimpleAutoMappingProfile
{
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ConfigProperty(s => s.UnitPrice, d => d.Price)
            .ConfigTransform(s => s.UnitPrice, price => price * 1.2m); // Apply 20% markup
    }
}

// Then mapping collections works automatically
List<Product> products = GetProducts();
List<ProductDto> dtos = _mapper.MapTo<List<ProductDto>>(products);
```

### 4. Working with Nested Collections

```csharp
public class Order
{
    public int Id { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public List<OrderItemDto> Items { get; set; }
}

// Configure mappings
public class OrderMappingProfile : SimpleAutoMappingProfile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<OrderItem, OrderItemDto>();
    }
}

// Then mapping nested collections works automatically
Order order = GetOrder();
OrderDto dto = Mapper.Map<Order, OrderDto>(order);
// dto.Items contains mapped items
```

### 5. Mapping with Custom Resolvers

```csharp
public class UserMappingProfile : SimpleAutoMappingProfile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserSummaryDto>()
            .AddResolver(d => d.FullName, s => $"{s.FirstName} {s.LastName}")
            .AddResolver(d => d.Age, s => CalculateAge(s.BirthDate))
            .AddResolver(d => d.IsActive, s => s.LastLoginDate > DateTime.Now.AddDays(-30));
    }
    
    private int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
```

## Performance Optimization

SimpleAutoMapping includes several performance optimizations:

### Smart Caching

The library uses caching to improve performance in repeated operations:

- Property cache to avoid repeating reflection
- Getters and setters compiled for fast property access
- Lazy initialization to optimize first use

```csharp
// First time is slightly slower due to initialization
var result1 = Mapper.Map<Source, Destination>(source);

// Subsequent operations are much faster
var result2 = Mapper.Map<Source, Destination>(anotherSource);
```

### Memory Cleanup

If you need to free up memory in long-running applications:

```csharp
// Clears all caches
Mapper.ClearCaches();
```

## Benchmark Results

SimpleAutoMapping is designed to be a lightweight and efficient mapping library. Below are performance results in different scenarios and data volumes.

### Object Mapping Performance

| Scenario | Objects | Average Time | Memory Allocated |
|----------|---------|--------------|------------------|
| Simple Object | 1 | 345.95 ns | 816 B |
| Complex Object (with nested properties) | 1 | 1.25 μs | 2.18 KB |
| Small Collection | 10 | 7.22 μs | 12.37 KB |
| Medium Collection | 100 | 64.57 μs | 111.86 KB |
| Large Collection | 1000 | 645.72 μs | 1.09 MB |
| Very Large Collection | 1,000,000 | 781.87 ms | 1.09 GB |
| Nested Collections (Order with items) | 1 (with 20 items) | 14.19 μs | 24.68 KB |
| Complex Nested Collections | 1 (with 50 items, each with 5 sub-items) | 168.52 μs | 257.54 KB |

### First Mapping vs Subsequent Mappings

Thanks to SimpleAutoMapping's caching mechanism, there is a notable performance difference between the first mapping operation and subsequent ones:

| Scenario | First Mapping | Subsequent Mappings | Improvement |
|----------|--------------|---------------------|-------------|
| Simple Object | 612.3 ns | 345.95 ns | 43.5% faster |
| Complex Object | 1425.8 ns | 1.25 μs | 12.3% faster |
| Collection (100 items) | 149.2 μs | 64.57 μs | 56.7% faster |

### Scaling with Data Size

SimpleAutoMapping shows linear scaling with the size of mapped collections:

| Collection Size | Time per Item | Total Time |
|-----------------|---------------|------------|
| 10 items | 722.5 ns/item | 7.22 μs |
| 100 items | 645.7 ns/item | 64.57 μs |
| 1000 items | 645.7 ns/item | 645.72 μs |
| 1,000,000 items | 781.9 ns/item | 781.87 ms |

This demonstrates excellent scalability - time per item remains relatively constant even when processing millions of records, with only a slight increase due to memory pressure at massive volumes.

### Memory Usage

SimpleAutoMapping is optimized for memory efficiency:

| Scenario | Objects | Memory Per Object |
|----------|---------|-------------------|
| Simple Object | 1 | 816 B |
| Complex Object | 1 | 2.18 KB |
| Large Collection | 1000 | ~1.12 KB per item |
| Very Large Collection | 1,000,000 | ~1.09 KB per item |

### Performance with Complex Nested Structures

Performance with deep hierarchical data structures also maintains good efficiency:

| Structure | Description | Time | Memory |
|------------|-------------|--------|---------|
| Order with basic items | 1 order with 20 products | 14.19 μs | 24.68 KB |
| Order with complex items | 1 order with 50 products, each with 5 categories | 168.52 μs | 257.54 KB |

### Key Performance Characteristics

1. **Cache Efficiency**: The library's internal caching system provides significant performance improvements for repeated mappings of the same types.

2. **Linear Scalability**: Performance scales linearly with the number of objects, making it suitable for large datasets.

3. **Memory Efficiency**: Memory usage remains proportional to the processed data without excessive overhead.

4. **Predictable Performance**: Mapping times are consistent and predictable based on object complexity and collection size.

5. **Initialization Cost**: There is a one-time initialization cost on the first mapping between two types, but this is quickly amortized over subsequent operations.

These benchmarks were conducted using BenchmarkDotNet on .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2, with typical business objects, various levels of complexity and collection sizes to represent real-world scenarios.

### Real-World Application Impact

When integrated into a typical web or service application, SimpleAutoMapping demonstrates minimal impact on overall performance:

| Scenario | Without Mapping | With SimpleAutoMapping | Overhead |
|-----------|-----------|----------------------|------------|
| REST API (1000 req/s) | 350 ms latency | 358 ms latency | ~2.3% |
| Batch Processing (1M records) | 4.82 s | 5.05 s | ~4.8% |

---

### License

`SimpleAutoMapping` is open source and licensed under the **MIT License**, which permits free modification, distribution, and commercialization. Users must include the original copyright notice and permission notice in all copies or substantial portions of the software. The software is provided "as is", without warranty of any kind.

For full license details, please see the [LICENSE](LICENSE) file in the repository.

---