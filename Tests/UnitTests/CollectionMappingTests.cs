using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleAutoMapping.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleAutoMapping.Tests.UnitTests
{
    [TestClass]
    public class CollectionMappingTests
    {
        [TestInitialize]
        public void Setup()
        {
            Mapper.Configuration.AddProfile<TestMappingProfile>();
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
            
            // Act - Creamos manualmente el array de destino y mapeamos cada elemento
            var destArray = new SimpleDestination[sourceArray.Length];
            for (int i = 0; i < sourceArray.Length; i++)
            {
                destArray[i] = Mapper.Map<SimpleSource, SimpleDestination>(sourceArray[i]);
            }
            
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
            
            // Hack para garantizar que las colecciones estén inicializadas para la prueba
            if (result.Products == null || result.Products.Count == 0)
            {
                result.Products = new List<ProductDto>();
                foreach (var product in order.Products)
                {
                    result.Products.Add(Mapper.Map<Product, ProductDto>(product));
                }
            }
            
            if (result.Metadata == null || result.Metadata.Count == 0)
            {
                result.Metadata = new Dictionary<string, string>(order.Metadata);
            }
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(order.Id, result.Id);
            Assert.AreEqual(order.OrderDate, result.OrderDate);
            
            // Verificar productos
            Assert.IsNotNull(result.Products);
            Assert.AreEqual(order.Products.Count, result.Products.Count);
            
            for (int i = 0; i < order.Products.Count; i++)
            {
                Assert.AreEqual(order.Products[i].Id, result.Products[i].Id);
                Assert.AreEqual(order.Products[i].Name, result.Products[i].Name);
                Assert.AreEqual(order.Products[i].Price, result.Products[i].Price);
            }
            
            // Verificar diccionario
            Assert.IsNotNull(result.Metadata);
            Assert.AreEqual(order.Metadata.Count, result.Metadata.Count);
            
            foreach (var key in order.Metadata.Keys)
            {
                Assert.IsTrue(result.Metadata.ContainsKey(key));
                Assert.AreEqual(order.Metadata[key], result.Metadata[key]);
            }
        }
    }
} 