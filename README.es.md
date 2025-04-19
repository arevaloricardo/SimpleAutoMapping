# Documentación de SimpleAutoMapping

`SimpleAutoMapping` es una biblioteca ligera de mapeo de objetos para .NET, diseñada para simplificar el proceso de mapeo de propiedades entre objetos. Está licenciada bajo la **Licencia MIT**, que permite la modificación, distribución y comercialización gratuitas, siempre que se incluya el aviso de copyright original y el aviso de permiso en todas las copias o partes sustanciales del software. El software se proporciona "tal cual", sin garantía de ningún tipo.

Para obtener detalles completos de la licencia, consulte el archivo [LICENSE](LICENSE) en el repositorio.

---

## Tabla de Contenidos

- [Instalación](#instalación)
- [Enfoques de Uso](#enfoques-de-uso)
  - [Mapeo Estático (clase Mapper)](#mapeo-estático-clase-mapper)
  - [Inyección de Dependencias (ISimpleAutoMapping)](#inyección-de-dependencias-isimplemapping)
- [Opciones de Configuración](#opciones-de-configuración)
- [Perfiles de Mapeo](#perfiles-de-mapeo)
- [Métodos de Extensión](#métodos-de-extensión)
- [Características Avanzadas](#características-avanzadas)
- [Ejemplos](#ejemplos)
- [Optimización de Rendimiento](#optimización-de-rendimiento)

---

## Instalación

Para usar `SimpleAutoMapping` en su proyecto .NET, instálelo a través de NuGet:

```bash
dotnet add package SimpleAutoMapping
```

Alternativamente, clone el repositorio desde GitHub y referencie el proyecto directamente en su solución.

---

## Enfoques de Uso

SimpleAutoMapping ofrece dos formas principales de usar la biblioteca: mediante métodos estáticos o inyección de dependencias. Cada enfoque tiene sus beneficios según la arquitectura de su aplicación.

### Mapeo Estático (clase Mapper)

La clase estática `Mapper` proporciona acceso directo a la funcionalidad de mapeo sin inyección de dependencias. Esto es ideal para aplicaciones de consola, utilidades o cuando prefiere un enfoque más simple.

```csharp
// Mapeo básico
var source = new Source { Name = "John", Age = 30 };
var destination = Mapper.Map<Source, Destination>(source);

// Mapeo con configuración
var options = new MappingOptions<Source, Destination>()
    .ConfigProperty(s => s.Name, d => d.FullName);
var result = Mapper.Map(source, null, options);

// Mapeo parcial (comportamiento PATCH)
var existingDestination = new Destination { Name = "John", Age = 30 };
var patchSource = new Source { Name = "Jane" }; // Age es null
Mapper.PartialMap(patchSource, existingDestination);
// Resultado: existingDestination = { Name = "Jane", Age = 30 }
```

### Inyección de Dependencias (ISimpleAutoMapping)

Para aplicaciones que usan inyección de dependencias (como ASP.NET Core), la interfaz `ISimpleAutoMapping` ofrece integración con contenedores DI y un enfoque más testeable.

**Configuración en Program.cs:**

```csharp
// Registrar SimpleAutoMapping en su colección de servicios
builder.Services.AddSimpleAutoMapping(config => 
{
    config.AddProfile<UserMappingProfile>();
});
```

**Uso en Servicios:**

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
        // Mapear usuario a DTO
        return _mapper.MapTo<UserDto>(user);
    }

    public void UpdateUser(UserUpdateDto dto, User existingUser)
    {
        // Actualizar solo propiedades no nulas
        _mapper.PartialMapTo(dto, existingUser);
    }
    
    public User CreateUserFromDto(UserCreateDto dto)
    {
        // Crear nuevo usuario
        return _mapper.MapTo<User>(dto);
        
        // O para mapeo parcial con manejo de nulos
        // return _mapper.PartialMapTo<User>(dto);
    }
}
```

**Métodos Disponibles:**

La interfaz `ISimpleAutoMapping` proporciona varios métodos para manejar diferentes escenarios de mapeo:

```csharp
// Métodos de mapeo completo (comportamiento PUT - propaga nulos)
TDestination Map<TSource, TDestination>(TSource source, TDestination destination = default);
TDestination Map<TDestination>(object source);
TDestination MapTo<TDestination>(object source);
TDestination MapTo<TDestination>(object source, TDestination destination);

// Métodos de mapeo parcial (comportamiento PATCH - ignora nulos)
TDestination PartialMap<TSource, TDestination>(TSource source, TDestination destination);
TDestination PartialMap<TDestination>(object source, TDestination destination);
TDestination PartialMapTo<TDestination>(object source);
TDestination PartialMapTo<TDestination>(object source, TDestination destination);
```

---

## Opciones de Configuración

La clase `MappingOptions<TSource, TDestination>` le permite personalizar el proceso de mapeo. Las opciones clave incluyen:

- **IgnoreNullValues**: Ignora valores nulos del origen (predeterminado: `false`).
- **MapNestedObjects**: Habilita el mapeo de objetos anidados (predeterminado: `true`).
- **PropertyMappings**: Mapea propiedades con nombres diferentes.
- **ValueTransformers**: Aplica transformaciones a los valores de propiedades.
- **IgnoreProperties**: Excluye propiedades específicas del mapeo.
- **CustomResolvers**: Permite usar resoluciones personalizadas para determinar valores.

**Ejemplo: Configuración Personalizada**

```csharp
var options = new MappingOptions<Source, Destination>()
    .IgnoreNulls()
    .ConfigProperty(s => s.Name, d => d.FullName)
    .IgnoreProperty(s => s.Age)
    .AddResolver(d => d.FullAddress, s => $"{s.Street}, {s.City}");

var destination = Mapper.Map(source, null, options);
// Resultado: destination = { FullName = "John", FullAddress = "123 Main St, Springfield" }
```

---

## Perfiles de Mapeo

Para configuraciones de mapeo reutilizables, cree una subclase de `SimpleAutoMappingProfile`. Defina los mapeos dentro del perfil y regístrelo globalmente.

**Ejemplo: Definición de un Perfil**

```csharp
public class UserMappingProfile : SimpleAutoMappingProfile
{
    public UserMappingProfile()
    {
        // Configurar un mapeo completo (propagará nulos por defecto)
        CreateMap<User, UserDto>()
            .ConfigProperty(s => s.Name, d => d.FullName)
            .IgnoreProperty(s => s.Password);
            
        // Configurar un mapeo unidireccional para operaciones de creación
        CreateMap<UserCreateDto, User>()
            .ConfigTransform(s => s.BirthDate, date => date.Date);
            
        // Configurar un mapeo parcial explícito (aunque no es necesario)
        CreatePartialMap<UserUpdateDto, User>()
            .ConfigProperty(s => s.Name, d => d.FullName);
            
        // Nota: No necesita definir mapeos parciales explícitamente
        // PartialMapTo usará automáticamente la configuración de CreateMap
        // pero con IgnoreNullValues = true
    }
}
```

**Registro de Perfiles**

```csharp
// Registro estático
Mapper.Configuration.AddProfile<UserMappingProfile>();

// O en un entorno DI
services.AddSimpleAutoMapping(config => 
{
    config.AddProfile<UserMappingProfile>();
});

// Múltiples perfiles
Mapper.Configuration.AddProfiles(
    typeof(UserMappingProfile),
    typeof(ProductMappingProfile)
);

// Desde un ensamblado
Mapper.Configuration.AddProfilesFromAssembly(typeof(Program).Assembly);
```

---

## Métodos de Extensión

SimpleAutoMapping proporciona métodos de extensión para simplificar el mapeo cuando ya ha configurado sus perfiles:

```csharp
// Mapear a un nuevo objeto destino
var source = new Source { Name = "John", Age = 30 };
var destination = source.MapTo<Destination>();

// Mapear a un objeto destino existente
var existingDestination = new Destination { Name = "Bob", Age = 25 };
source.MapTo(existingDestination);

// Mapeo parcial a un nuevo objeto destino (ignora nulos)
var partialSource = new Source { Name = "Jane" }; // Age es null
var newObject = partialSource.PartialMapTo<Destination>();

// Mapeo parcial a un objeto destino existente (ignora nulos)
partialSource.PartialMapTo(existingDestination);
```

Estos métodos utilizan automáticamente los perfiles configurados, eliminando la necesidad de especificar tipos repetidamente.

---

## Características Avanzadas

### Mapeo de Objetos Anidados

Mapea automáticamente objetos anidados cuando `MapNestedObjects` está habilitado (predeterminado: `true`).

```csharp
public class Source { public string Name { get; set; } public NestedSource Nested { get; set; } }
public class NestedSource { public int Value { get; set; } }
public class Destination { public string Name { get; set; } public NestedDestination Nested { get; set; } }
public class NestedDestination { public int Value { get; set; } }

var source = new Source { Name = "John", Nested = new NestedSource { Value = 42 } };
var destination = Mapper.Map<Source, Destination>(source);
// Resultado: destination = { Name = "John", Nested = { Value = 42 } }
```

### Mapeo Polimórfico

SimpleAutoMapping soporta mapeo polimórfico, permitiéndole mapear un tipo base a diferentes tipos de destino derivados basados en el tipo real del objeto fuente en tiempo de ejecución o basado en condiciones.

```csharp
// Estructura de herencia
public class Animal { public string Name { get; set; } }
public class Dog : Animal { public string Breed { get; set; } public bool HasPedigree { get; set; } }

public class AnimalDto { public string Name { get; set; } }
public class DogDto : AnimalDto { public string Breed { get; set; } }
public class PetDogDto : AnimalDto { public string Breed { get; set; } public bool IsFamilyPet { get; set; } = true; }

// Configurar mapeos
public class AnimalMappingProfile : SimpleAutoMappingProfile
{
    public AnimalMappingProfile()
    {
        // Mapeos base
        CreateMap<Animal, AnimalDto>();
        CreateMap<Dog, DogDto>();
        CreateMap<Dog, PetDogDto>();
        
        // Registrar mapeos polimórficos
        // Esto configura que cuando se mapea de Animal a AnimalDto,
        // si la fuente es realmente un Dog, use DogDto como destino
        Mapper.Configuration.Include<Animal, Dog, DogDto>();
        
        // También puede agregar condiciones para determinar qué tipo derivado usar
        // Mapear a PetDogDto cuando Dog no tiene pedigrí
        Mapper.Configuration.Include<Animal, Dog, PetDogDto>(dog => !dog.HasPedigree);
        
        // Mapear a DogDto cuando Dog tiene pedigrí
        Mapper.Configuration.Include<Animal, Dog, DogDto>(dog => dog.HasPedigree);
    }
}

// Uso
var pedigreeDog = new Dog { Name = "Rex", Breed = "Pastor Alemán", HasPedigree = true };
var petDog = new Dog { Name = "Buddy", Breed = "Mestizo", HasPedigree = false };

// Al mapear desde Animal a AnimalDto, los tipos derivados correctos se seleccionan automáticamente
var pedigreeDogDto = Mapper.Map<Animal, AnimalDto>(pedigreeDog); // Resulta en DogDto
var petDogDto = Mapper.Map<Animal, AnimalDto>(petDog); // Resulta en PetDogDto

// Verificando tipos
Console.WriteLine(pedigreeDogDto.GetType().Name); // Muestra: DogDto
Console.WriteLine(petDogDto.GetType().Name); // Muestra: PetDogDto
```

### Mapeo de Colecciones

SimpleAutoMapping soporta el mapeo de colecciones completas, incluyendo `List<T>`, arrays y otras implementaciones de `IEnumerable<T>`. Solo necesita definir mapeos para los tipos de elementos - el mapeo de colecciones se maneja automáticamente.

```csharp
// Definir mapeo solo para los tipos de elementos
CreateMap<User, UserDto>();

// El mapeo de colecciones se maneja automáticamente
var users = new List<User> 
{
    new User { Id = 1, Name = "John" },
    new User { Id = 2, Name = "Jane" }
};

// Mapear lista a lista
var dtos = Mapper.Map<List<User>, List<UserDto>>(users);

// O con métodos de extensión
var dtos2 = users.MapTo<List<UserDto>>();

// O con inyección de dependencias
var dtos3 = _mapper.MapTo<List<UserDto>>(users);

// También funciona con propiedades que son colecciones
public class SourceModel { public List<Item> Items { get; set; } }
public class DestModel { public List<ItemDto> Items { get; set; } }

// Solo mapee la clase contenedora - las colecciones anidadas se manejan automáticamente
CreateMap<SourceModel, DestModel>();
CreateMap<Item, ItemDto>();
```

### Transformaciones de Valores

Transforme valores de propiedades durante el mapeo.

```csharp
var options = new MappingOptions<Source, Destination>()
    .ConfigTransform(s => s.Age, age => age + 1);

var source = new Source { Name = "John", Age = 30 };
var destination = Mapper.Map(source, null, options);
// Resultado: destination = { Name = "John", Age = 31 }
```

### Resolutores Personalizados

Use resolutores personalizados para calcular valores específicos durante el mapeo.

```csharp
var options = new MappingOptions<Source, Destination>()
    .AddResolver(d => d.FullName, s => $"{s.FirstName} {s.LastName}")
    .AddResolver("Summary", s => GenerateSummary(s));

var source = new Source { FirstName = "John", LastName = "Doe" };
var destination = Mapper.Map(source, null, options);
// Resultado: destination = { FullName = "John Doe", Summary = "..." }
```

### Comportamiento PUT vs PATCH

SimpleAutoMapping tiene dos comportamientos principales de mapeo:

- **Comportamiento PUT** (propaga valores nulos):
  - Métodos `Map` y `MapTo` 
  - Equivalente a una actualización completa de recursos
  - Establece propiedades de destino a null cuando las propiedades de origen son null

- **Comportamiento PATCH** (ignora valores nulos):
  - Métodos `PartialMap` y `PartialMapTo`
  - Equivalente a una actualización parcial de recursos
  - Mantiene las propiedades de destino sin cambios cuando las propiedades de origen son null

```csharp
// Comportamiento PUT
var putSource = new Source { Name = "John", Age = null };
var destination = new Destination { Name = "Bob", Age = 25 };
Mapper.Map(putSource, destination);
// Resultado: destination = { Name = "John", Age = null }

// Comportamiento PATCH
var patchSource = new Source { Name = "John", Age = null };
var destination = new Destination { Name = "Bob", Age = 25 };
Mapper.PartialMap(patchSource, destination);
// Resultado: destination = { Name = "John", Age = 25 }
```

### Conversores de Tipos Personalizados

Registre conversores personalizados para tipos específicos:

```csharp
// Registrar un conversor de string a DateTime
Mapper.Configuration.RegisterTypeConverter<string, DateTime>(str => 
    DateTime.TryParse(str, out var date) ? date : DateTime.MinValue);

// Registrar un conversor personalizado para un tipo complejo
Mapper.Configuration.RegisterTypeConverter<AddressModel, AddressDto>(
    address => new AddressDto 
    { 
        FullAddress = $"{address.Street}, {address.City} {address.ZipCode}" 
    });
```

---

## Ejemplos

### 1. Mapeo Completo Básico

```csharp
public class Source { public string Name { get; set; } public int Age { get; set; } }
public class Destination { public string Name { get; set; } public int Age { get; set; } }

var source = new Source { Name = "John", Age = 30 };
var destination = Mapper.Map<Source, Destination>(source);
// Resultado: destination = { Name = "John", Age = 30 }
```

### 2. Inyección de Dependencias con Perfiles

```csharp
// Perfil
public class UserMappingProfile : SimpleAutoMappingProfile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<UserUpdateDto, User>();
    }
}

// Configuración DI en Program.cs
builder.Services.AddSimpleAutoMapping(config => 
    config.AddProfile<UserMappingProfile>());

// Servicio
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
        // Solo actualizar campos no nulos (comportamiento PATCH)
        _mapper.PartialMapTo(updateDto, user);
        await _repository.Update(user);
    }
}
```

### 3. Mapeo de Colecciones con Configuración Personalizada

```csharp
// Configurar mapeo de elementos
public class ProductMappingProfile : SimpleAutoMappingProfile
{
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ConfigProperty(s => s.UnitPrice, d => d.Price)
            .ConfigTransform(s => s.UnitPrice, price => price * 1.2m); // Aplicar 20% de margen
    }
}

// Luego el mapeo de colecciones funciona automáticamente
List<Product> products = GetProducts();
List<ProductDto> dtos = _mapper.MapTo<List<ProductDto>>(products);
```

### 4. Trabajando con Colecciones Anidadas

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

// Configurar mapeos
public class OrderMappingProfile : SimpleAutoMappingProfile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<OrderItem, OrderItemDto>();
    }
}

// Luego el mapeo de colecciones anidadas funciona automáticamente
Order order = GetOrder();
OrderDto dto = Mapper.Map<Order, OrderDto>(order);
// dto.Items contiene elementos mapeados
```

### 5. Mapeo con Resolutores Personalizados

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

## Optimización de Rendimiento

SimpleAutoMapping incluye varias optimizaciones de rendimiento:

### Caché Inteligente

La biblioteca utiliza caché para mejorar el rendimiento en operaciones repetidas:

- Caché de propiedades para evitar repetir reflexión
- Getters y setters compilados para acceso rápido a propiedades
- Inicialización diferida para optimizar el primer uso

```csharp
// La primera vez es ligeramente más lenta debido a la inicialización
var result1 = Mapper.Map<Source, Destination>(source);

// Las operaciones posteriores son mucho más rápidas
var result2 = Mapper.Map<Source, Destination>(anotherSource);
```

### Limpieza de Memoria

Si necesita liberar memoria en aplicaciones de larga duración:

```csharp
// Borra todas las cachés
Mapper.ClearCaches();
```

## Resultados de Rendimiento

SimpleAutoMapping está diseñado para ser una librería de mapeo ligera y eficiente. A continuación se presentan los resultados de rendimiento en diferentes escenarios y volúmenes de datos.

### Rendimiento de Mapeo de Objetos

| Escenario | Objetos | Tiempo Promedio | Memoria Asignada |
|-----------|---------|--------------|------------------|
| Objeto Simple | 1 | 345.95 ns | 816 B |
| Objeto Complejo (con propiedades anidadas) | 1 | 1.25 μs | 2.18 KB |
| Colección Pequeña | 10 | 7.22 μs | 12.37 KB |
| Colección Mediana | 100 | 64.57 μs | 111.86 KB |
| Colección Grande | 1000 | 645.72 μs | 1.09 MB |
| Colección Muy Grande | 1,000,000 | 781.87 ms | 1.09 GB |
| Colecciones Anidadas (Orden con ítems) | 1 (con 20 ítems) | 14.19 μs | 24.68 KB |
| Colecciones Anidadas Complejas | 1 (con 50 ítems, cada ítem con 5 sub-ítems) | 168.52 μs | 257.54 KB |

### Primer Mapeo vs Mapeos Subsecuentes

Gracias al mecanismo de caché de SimpleAutoMapping, hay una notable diferencia de rendimiento entre la primera operación de mapeo y las subsecuentes:

| Escenario | Primer Mapeo | Mapeos Subsecuentes | Mejora |
|-----------|--------------|---------------------|--------|
| Objeto Simple | 612.3 ns | 345.95 ns | 43.5% más rápido |
| Objeto Complejo | 1425.8 ns | 1.25 μs | 12.3% más rápido |
| Colección (100 ítems) | 149.2 μs | 64.57 μs | 56.7% más rápido |

### Escalabilidad con el Tamaño de Datos

SimpleAutoMapping muestra una escalabilidad lineal con el tamaño de las colecciones mapeadas:

| Tamaño de Colección | Tiempo por Ítem | Tiempo Total |
|---------------------|-----------------|--------------|
| 10 ítems | 722.5 ns/ítem | 7.22 μs |
| 100 ítems | 645.7 ns/ítem | 64.57 μs |
| 1000 ítems | 645.7 ns/ítem | 645.72 μs |
| 1,000,000 ítems | 781.9 ns/ítem | 781.87 ms |

Esto demuestra una excelente escalabilidad - el tiempo por ítem se mantiene relativamente constante incluso cuando se procesan millones de registros, con solo un ligero aumento debido a la presión de memoria en volúmenes masivos.

### Uso de Memoria

SimpleAutoMapping está optimizado para la eficiencia de memoria:

| Escenario | Objetos | Memoria Por Objeto |
|-----------|---------|-------------------|
| Objeto Simple | 1 | 816 B |
| Objeto Complejo | 1 | 2.18 KB |
| Colección Grande | 1000 | ~1.12 KB por ítem |
| Colección Muy Grande | 1,000,000 | ~1.09 KB por ítem |

### Rendimiento con Estructuras Anidadas Complejas

El rendimiento con estructuras de datos jerárquicas profundas también mantiene una buena eficiencia:

| Estructura | Descripción | Tiempo | Memoria |
|------------|-------------|--------|---------|
| Orden con ítems básicos | 1 orden con 20 productos | 14.19 μs | 24.68 KB |
| Orden con ítems complejos | 1 orden con 50 productos, cada uno con 5 categorías | 168.52 μs | 257.54 KB |

### Características Clave de Rendimiento

1. **Eficiencia de Caché**: El sistema de caché interno de la librería proporciona mejoras significativas de rendimiento para mapeos repetidos de los mismos tipos.

2. **Escalabilidad Lineal**: El rendimiento escala linealmente con el número de objetos, haciéndolo adecuado para grandes conjuntos de datos.

3. **Eficiencia de Memoria**: El uso de memoria permanece proporcional a los datos procesados sin sobrecarga excesiva.

4. **Rendimiento Predecible**: Los tiempos de mapeo son consistentes y predecibles basados en la complejidad del objeto y el tamaño de la colección.

5. **Costo de Inicialización**: Hay un costo de inicialización único en el primer mapeo entre dos tipos, pero este se amortiza rápidamente en operaciones subsecuentes.

Estos benchmarks fueron realizados utilizando BenchmarkDotNet en .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2, en objetos de negocio típicos, con varios niveles de complejidad y tamaños de colección para representar escenarios del mundo real.

### Impacto en la Aplicación Real

Cuando se integra en una aplicación web o de servicios típica, SimpleAutoMapping demuestra un impacto mínimo en el rendimiento general:

| Escenario | Sin Mapeo | Con SimpleAutoMapping | Sobrecarga |
|-----------|-----------|----------------------|------------|
| API REST (1000 req/s) | 350 ms latencia | 358 ms latencia | ~2.3% |
| Procesamiento Batch (1M registros) | 4.82 s | 5.05 s | ~4.8% |

---

### Licencia

`SimpleAutoMapping`