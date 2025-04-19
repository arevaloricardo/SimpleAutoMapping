using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleAutoMapping.Tests.TestModels;
using System;

namespace SimpleAutoMapping.Tests.UnitTests
{
    [TestClass]
    public class CustomConfigurationTests
    {
        [TestInitialize]
        public void Setup()
        {
            Mapper.Configuration.AddProfile<TestMappingProfile>();
        }
        
        [TestMethod]
        public void Map_WithCustomPropertyMappings_ShouldMapCorrectly()
        {
            // Arrange
            var source = new SimpleSource 
            { 
                Id = 1, 
                Name = "Test", 
                CreatedDate = DateTime.Now, 
                IsActive = true 
            };
            
            // Act - Usar el perfil que ya contiene la configuración
            var result = Mapper.Map<SimpleSource, SimpleDestinationWithDifferentNames>(source);
            
            // Assert
            Assert.AreEqual(source.Id, result.Identifier);
            Assert.AreEqual(source.Name, result.FullName);
            Assert.AreEqual(source.CreatedDate, result.Created);
            Assert.AreEqual(source.IsActive, result.Status);
        }
        
        [TestMethod]
        public void Map_WithCustomTransformer_ShouldTransformValue()
        {
            // Arrange
            var source = new SimpleSource { Name = "test name" };
            
            // Act - Crear configuración inline
            var result = Mapper.Map<SimpleSource, SimpleDestination>(
                source,
                configOptions: options => options.ConfigTransform(
                    src => src.Name,
                    name => name?.ToUpper()
                )
            );
            
            // Assert
            Assert.AreEqual("TEST NAME", result.Name);
        }
        
        [TestMethod]
        public void Map_WithCustomResolver_ShouldUseResolver()
        {
            // Arrange
            var source = new SimpleSource { Id = 123, Name = "Test" };
            
            // Act
            var result = Mapper.Map<SimpleSource, SimpleDestination>(
                source,
                configOptions: options => options.AddResolver(
                    dest => dest.Name,
                    src => $"ID: {src.Id}, Name: {src.Name}"
                )
            );
            
            // Assert
            Assert.AreEqual("ID: 123, Name: Test", result.Name);
        }
        
        [TestMethod]
        public void Map_WithIgnoredProperty_ShouldNotMapProperty()
        {
            // Arrange
            var source = new SimpleSource { Id = 1, Name = "Test" };
            var destination = new SimpleDestination { Id = 5, Name = "Original" };
            
            // Act
            var result = Mapper.Map<SimpleSource, SimpleDestination>(
                source,
                destination,
                configOptions: options => options.IgnoreProperty(src => src.Id)
            );
            
            // Assert
            Assert.AreEqual(5, result.Id); // No debería cambiar
            Assert.AreEqual("Test", result.Name); // Debería actualizarse
        }
    }
} 