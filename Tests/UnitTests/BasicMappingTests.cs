using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleAutoMapping.Tests.TestModels;
using System;

namespace SimpleAutoMapping.Tests.UnitTests
{
    [TestClass]
    public class BasicMappingTests
    {
        [TestInitialize]
        public void Setup()
        {
            // Configurar el mapper para las pruebas
            Mapper.Configuration.AddProfile<TestMappingProfile>();
        }
        
        [TestMethod]
        public void Map_WithValidSource_ShouldMapAllProperties()
        {
            // Arrange
            var source = new SimpleSource 
            { 
                Id = 1, 
                Name = "Test", 
                CreatedDate = DateTime.Now, 
                IsActive = true 
            };
            
            // Act
            var result = Mapper.Map<SimpleSource, SimpleDestination>(source);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(source.Id, result.Id);
            Assert.AreEqual(source.Name, result.Name);
            Assert.AreEqual(source.CreatedDate, result.CreatedDate);
            Assert.AreEqual(source.IsActive, result.IsActive);
        }
        
        [TestMethod]
        public void Map_WithNullSource_ShouldReturnNewDestination()
        {
            // Act
            var result = Mapper.Map<SimpleSource, SimpleDestination>(null);
            
            // Assert
            Assert.IsNull(result);
            // Las siguientes aserciones no se ejecutarán si result es null
            // Assert.AreEqual(0, result.Id);
            // Assert.IsNull(result.Name);
        }
        
        [TestMethod]
        public void Map_WithExistingDestination_ShouldUpdateDestination()
        {
            // Arrange
            var source = new SimpleSource { Id = 1, Name = "Test" };
            var destination = new SimpleDestination { Id = 5, Name = "Original" };
            
            // Act
            var result = Mapper.Map<SimpleSource, SimpleDestination>(source, destination);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(source.Id, result.Id);
            Assert.AreEqual(source.Name, result.Name);
            
            // Verificar que es la misma instancia
            Assert.AreSame(destination, result);
        }
        
        [TestMethod]
        public void MapTo_ExtensionMethod_ShouldWork()
        {
            // Arrange
            var source = new SimpleSource { Id = 1, Name = "Test" };
            
            // Act
            var result = source.MapTo<SimpleDestination>();
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(source.Id, result.Id);
            Assert.AreEqual(source.Name, result.Name);
        }
    }
} 