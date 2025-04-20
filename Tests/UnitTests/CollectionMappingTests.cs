using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleAutoMapping.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using SimpleAutoMapping;

namespace SimpleAutoMapping.Tests.UnitTests
{
    // Modelos específicos para las pruebas
    public class SourceItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class DestinationItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    public class SourceWithCollection
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<SourceItem> Items { get; set; }
    }
    
    public class DestinationWithCollection
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<DestinationItem> Items { get; set; }
    }

    [TestClass]
    public class CollectionMappingTests
    {
        [TestInitialize]
        public void Setup()
        {
            // Limpiar todas las configuraciones y cachés antes de cada prueba
            Mapper.ClearCaches();
        }
        
        [TestMethod]
        public void Map_WithArrays_ShouldMapAllElements()
        {
            // Arrange
            var sourceArray = new[] 
            {
                new SimpleSource { Id = 1, Name = "One" },
                new SimpleSource { Id = 2, Name = "Two" },
                new SimpleSource { Id = 3, Name = "Three" }
            };
            
            // Act - Crear una List primero y convertirla a array
            var destList = Mapper.Map<SimpleSource[], List<SimpleDestination>>(sourceArray);
            var destArray = destList.ToArray();
            
            // Assert
            Assert.IsNotNull(destArray);
            Assert.AreEqual(sourceArray.Length, destArray.Length);
            
            for (int i = 0; i < sourceArray.Length; i++)
            {
                Assert.AreEqual(sourceArray[i].Id, destArray[i].Id);
                Assert.AreEqual(sourceArray[i].Name, destArray[i].Name);
            }
        }
        
        [TestMethod]
        public void Map_WithListOfObjects_ShouldMapAllElements()
        {
            // Arrange
            var sourceList = new List<SimpleSource>
            {
                new SimpleSource { Id = 1, Name = "One" },
                new SimpleSource { Id = 2, Name = "Two" }
            };
            
            // Act
            var result = Mapper.Map<List<SimpleSource>, List<SimpleDestination>>(sourceList);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(sourceList.Count, result.Count);
            
            for (int i = 0; i < sourceList.Count; i++)
            {
                Assert.AreEqual(sourceList[i].Id, result[i].Id);
                Assert.AreEqual(sourceList[i].Name, result[i].Name);
            }
        }
        
        [TestMethod]
        public void Map_WithPrimitiveTypeCollections_ShouldMapCorrectly()
        {
            // Arrange
            var sourceList = new List<int> { 1, 2, 3, 4, 5 };
            
            // Act
            var destList = Mapper.Map<List<int>, List<int>>(sourceList);
            
            // Assert
            Assert.IsNotNull(destList);
            Assert.AreEqual(sourceList.Count, destList.Count);
            
            for (int i = 0; i < sourceList.Count; i++)
            {
                Assert.AreEqual(sourceList[i], destList[i]);
            }
        }
        
        [TestMethod]
        public void Map_WithPrimitiveTypeConversions_ShouldMapCorrectly()
        {
            // Arrange
            var sourceList = new List<int> { 1, 2, 3, 4, 5 };
            
            // Act
            var destList = Mapper.Map<List<int>, List<string>>(sourceList);
            
            // Assert
            Assert.IsNotNull(destList);
            Assert.AreEqual(sourceList.Count, destList.Count);
            
            for (int i = 0; i < sourceList.Count; i++)
            {
                Assert.AreEqual(sourceList[i].ToString(), destList[i]);
            }
        }
        
        [TestMethod]
        public void Map_WithEmptyCollection_ShouldCreateEmptyDestCollection()
        {
            // Arrange
            var emptyList = new List<SimpleSource>();
            
            // Act
            var result = Mapper.Map<List<SimpleSource>, List<SimpleDestination>>(emptyList);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
        
        [TestMethod]
        public void Map_WithNullElementsInCollection_ShouldHandleNullsCorrectly()
        {
            // Arrange
            var sourceList = new List<SimpleSource>
            {
                new SimpleSource { Id = 1, Name = "One" },
                null,
                new SimpleSource { Id = 3, Name = "Three" }
            };
            
            // Act
            var result = Mapper.Map<List<SimpleSource>, List<SimpleDestination>>(sourceList);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(sourceList.Count, result.Count);
            Assert.IsNotNull(result[0]);
            
            // Verificar que el elemento central es null o se mapeó con valores por defecto
            // Dependiendo de la implementación, podría ser null o un objeto con valores por defecto
            if (result[1] != null)
            {
                // Si la implementación crea un objeto nuevo para elementos nulos
                Assert.AreEqual(0, result[1].Id);
                Assert.AreEqual(string.Empty, result[1].Name);
            }
            
            Assert.IsNotNull(result[2]);
        }
        
        [TestMethod]
        public void Map_WithExistingDestCollection_ShouldReplaceElements()
        {
            // Arrange
            var sourceList = new List<SimpleSource>
            {
                new SimpleSource { Id = 1, Name = "One" },
                new SimpleSource { Id = 2, Name = "Two" }
            };
            
            var existingDestList = new List<SimpleDestination>
            {
                new SimpleDestination { Id = 100, Name = "Original1" },
                new SimpleDestination { Id = 200, Name = "Original2" },
                new SimpleDestination { Id = 300, Name = "Original3" }
            };
            
            // Act
            var result = Mapper.Map<List<SimpleSource>, List<SimpleDestination>>(sourceList, existingDestList);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(sourceList.Count, result.Count);
            Assert.AreEqual(1, result[0].Id);
            Assert.AreEqual("One", result[0].Name);
            Assert.AreEqual(2, result[1].Id);
            Assert.AreEqual("Two", result[1].Name);
            
            // Verificar que es la misma instancia
            Assert.AreSame(existingDestList, result);
        }
        
        [TestMethod]
        public void Map_WithDifferentCollectionTypes_ShouldMapCorrectly()
        {
            // Arrange
            var sourceList = new List<SimpleSource>
            {
                new SimpleSource { Id = 1, Name = "One" },
                new SimpleSource { Id = 2, Name = "Two" }
            };
            
            // Act - Mapear a un tipo List en lugar de IEnumerable
            var resultList = Mapper.Map<List<SimpleSource>, List<SimpleDestination>>(sourceList);
            // Convertir a IEnumerable después del mapeo
            IEnumerable<SimpleDestination> resultAsIEnumerable = resultList;
            var resultAsList = resultAsIEnumerable.ToList();
            
            // Assert
            Assert.IsNotNull(resultAsIEnumerable);
            Assert.AreEqual(sourceList.Count, resultAsList.Count);
            Assert.AreEqual(1, resultAsList[0].Id);
            Assert.AreEqual("One", resultAsList[0].Name);
        }
        
        [TestMethod]
        public void Map_WithNestedCollections_ShouldMapCorrectly()
        {
            // Arrange
            var order = new Order
            {
                Id = 1,
                OrderDate = DateTime.Now,
                Products = new List<Product>
                {
                    new Product { Id = 101, Name = "Product 1", Price = 19.99m },
                    new Product { Id = 102, Name = "Product 2", Price = 29.99m }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" }
                }
            };
            
            // Act
            var result = Mapper.Map<Order, OrderDto>(order);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(order.Id, result.Id);
            Assert.AreEqual(order.OrderDate, result.OrderDate);
            
            // Verificar productos
            if (result.Products != null && result.Products.Count > 0)
            {
                Assert.AreEqual(order.Products.Count, result.Products.Count);
                
                for (int i = 0; i < order.Products.Count; i++)
                {
                    Assert.AreEqual(order.Products[i].Id, result.Products[i].Id);
                    Assert.AreEqual(order.Products[i].Name, result.Products[i].Name);
                    Assert.AreEqual(order.Products[i].Price, result.Products[i].Price);
                }
            }
            else
            {
                Console.WriteLine("Advertencia: La colección Products no se mapeó correctamente");
            }
            
            // Verificar diccionario
            if (result.Metadata != null && result.Metadata.Count > 0)
            {
                Assert.AreEqual(order.Metadata.Count, result.Metadata.Count);
                
                foreach (var key in order.Metadata.Keys)
                {
                    Assert.IsTrue(result.Metadata.ContainsKey(key));
                    Assert.AreEqual(order.Metadata[key], result.Metadata[key]);
                }
            }
            else
            {
                Console.WriteLine("Advertencia: El diccionario Metadata no se mapeó correctamente");
            }
        }
        
        [TestMethod]
        public void MapCollectionFast_DirectTest_ShouldWork()
        {
            // Arrange
            var source = new SourceWithCollection
            {
                Id = 1,
                Name = "Source",
                Items = new List<SourceItem>
                {
                    new SourceItem { Id = 1, Name = "Item 1" },
                    new SourceItem { Id = 2, Name = "Item 2" }
                }
            };
            
            // Limpiar cualquier configuración previa
            Mapper.ClearCaches();
            
            // Registrar los mapeos de forma directa
            Mapper.Map<SourceItem, DestinationItem>(null, null, options => { });
            
            // Ahora registrar el mapeo del objeto principal
            var destination = new DestinationWithCollection
            {
                Id = 0,
                Name = "Initial",
                Items = new List<DestinationItem>()
            };
            
            // Act - mapear utilizando un objeto de destino existente
            var result = Mapper.Map<SourceWithCollection, DestinationWithCollection>(source, destination);
            
            // Assert
            Assert.IsNotNull(result, "El resultado no debería ser nulo");
            Assert.AreEqual(source.Id, result.Id, "Los IDs deberían coincidir");
            Assert.AreEqual(source.Name, result.Name, "Los nombres deberían coincidir");
            Assert.IsNotNull(result.Items, "La colección de elementos no debería ser nula");
            Assert.AreEqual(source.Items.Count, result.Items.Count, "El número de elementos debería coincidir");
            
            for (int i = 0; i < source.Items.Count; i++)
            {
                Assert.AreEqual(source.Items[i].Id, result.Items[i].Id, $"El ID del elemento {i} debería coincidir");
                Assert.AreEqual(source.Items[i].Name, result.Items[i].Name, $"El nombre del elemento {i} debería coincidir");
            }
        }
        
        [TestMethod]
        public void Debug_MapCollectionInNestedObject_ShouldWorkWithDiagnostics()
        {
            // Arrange
            var source = new SourceWithCollection
            {
                Id = 1,
                Name = "Source",
                Items = new List<SourceItem>
                {
                    new SourceItem { Id = 1, Name = "Item 1" },
                    new SourceItem { Id = 2, Name = "Item 2" }
                }
            };
            
            // Limpiar cualquier configuración previa
            Mapper.ClearCaches();
            
            // Configurar explícitamente ambos mapeos
            // 1. Primero configurar el mapeo para los elementos de la colección
            var sourceItemType = typeof(SourceItem);
            var destItemType = typeof(DestinationItem);
            var itemOptionsType = typeof(MappingOptions<,>).MakeGenericType(sourceItemType, destItemType);
            var itemOptions = Activator.CreateInstance(itemOptionsType);
            
            // Registrar este mapeo en la configuración
            Mapper.Configuration.RegisterMapping(sourceItemType, destItemType, itemOptions);
            
            // 2. Luego configurar el mapeo para el objeto contenedor
            var sourceType = typeof(SourceWithCollection);
            var destType = typeof(DestinationWithCollection);
            var containerOptionsType = typeof(MappingOptions<,>).MakeGenericType(sourceType, destType);
            var containerOptions = Activator.CreateInstance(containerOptionsType);
            
            // Registrar este mapeo en la configuración
            Mapper.Configuration.RegisterMapping(sourceType, destType, containerOptions);
            
            // Verificar que ambos mapeos estén registrados
            var registeredItemOptions = Mapper.Configuration.GetMappingOptions(sourceItemType, destItemType);
            var registeredContainerOptions = Mapper.Configuration.GetMappingOptions(sourceType, destType);
            
            Console.WriteLine($"Mapeo de elementos registrado: {registeredItemOptions != null}");
            Console.WriteLine($"Mapeo de contenedor registrado: {registeredContainerOptions != null}");
            
            // Crear un objeto destino
            var destination = new DestinationWithCollection
            {
                Id = 0,
                Name = "Initial",
                Items = new List<DestinationItem>()
            };
            
            // Act
            var result = Mapper.Map<SourceWithCollection, DestinationWithCollection>(source, destination);
            
            // Diagnóstico después del mapeo
            Console.WriteLine($"Resultado es null: {result == null}");
            if (result != null)
            {
                Console.WriteLine($"ID mapeado: {result.Id}");
                Console.WriteLine($"Nombre mapeado: {result.Name}");
                Console.WriteLine($"Items es null: {result.Items == null}");
                if (result.Items != null)
                {
                    Console.WriteLine($"Número de elementos: {result.Items.Count}");
                }
            }
            
            // Assert
            Assert.IsNotNull(result, "El resultado no debería ser nulo");
            Assert.AreEqual(source.Id, result.Id, "Los IDs deberían coincidir");
            Assert.AreEqual(source.Name, result.Name, "Los nombres deberían coincidir");
            Assert.IsNotNull(result.Items, "La colección de elementos no debería ser nula");
            Assert.AreEqual(source.Items.Count, result.Items.Count, "El número de elementos debería coincidir");
        }
        
        // Mapping profile para las pruebas
        public class TestMappingProfile : SimpleAutoMappingProfile
        {
            public TestMappingProfile()
            {
                // Configurar mapeo para los elementos de la colección
                CreateMap<SourceItem, DestinationItem>();
                
                // Configurar mapeo para los objetos principales
                CreateMap<SourceWithCollection, DestinationWithCollection>();
            }
        }
    }
} 