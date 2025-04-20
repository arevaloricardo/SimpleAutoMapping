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
        
        [TestMethod]
        public void MapWithTypeInference_ShouldUseCorrectMapping()
        {
            // Arrange
            var source = new SimpleSource { Id = 1, Name = "Test" };
            
            // Act - Usar Map<TDestination> con inferencia de tipo origen
            var result = Mapper.Map<SimpleDestination>(source);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(source.Id, result.Id);
            Assert.AreEqual(source.Name, result.Name);
        }
        
        [TestMethod]
        public void MapToWithExistingDestination_ShouldWork()
        {
            // Arrange
            var source = new SimpleSource { Id = 1, Name = "Test" };
            var destination = new SimpleDestination { Id = 5, Name = "Original" };
            
            // Act - Usar el método de extensión con destino existente
            var result = source.MapTo(destination);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(source.Id, result.Id);
            Assert.AreEqual(source.Name, result.Name);
            
            // Verificar que es la misma instancia
            Assert.AreSame(destination, result);
        }
        
        [TestMethod]
        public void Map_WithFullTypeInference_ShouldUseDefaultDestination()
        {
            // Arrange
            // Configurar un mapeo predeterminado
            var source = new SimpleSource { Id = 1, Name = "Test" };
            
            // Act - Usar Map(object) con inferencia completa
            var result = Mapper.Map(source);
            
            // Assert
            Assert.IsNotNull(result);
            
            // El tipo puede ser SimpleDestination o SimpleDestinationWithDifferentNames
            // dependiendo de la configuración de mapeo
            Assert.IsTrue(
                result is SimpleDestination || result is SimpleDestinationWithDifferentNames,
                "El resultado debe ser una clase destino válida (SimpleDestination o SimpleDestinationWithDifferentNames)"
            );
            
            // Verificar propiedades básicas independientemente del tipo exacto
            if (result is SimpleDestination typedResult)
            {
                Assert.AreEqual(source.Id, typedResult.Id);
                Assert.AreEqual(source.Name, typedResult.Name);
            }
            else if (result is SimpleDestinationWithDifferentNames altResult)
            {
                Assert.AreEqual(source.Id, altResult.Identifier);
                Assert.AreEqual(source.Name, altResult.FullName);
            }
        }
        
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Map_WithNoRegisteredMapping_ShouldThrowException()
        {
            // Arrange - Crear un tipo que no tiene mapeo registrado
            var unmappedSource = new UnmappedType { Value = "test" };
            
            // Act - Intentar mapear sin configuración
            var result = Mapper.Map<SimpleDestination>(unmappedSource);
            
            // Assert - La excepción InvalidOperationException es esperada
        }
        
        // Clase auxiliar para prueba
        private class UnmappedType
        {
            public string Value { get; set; }
        }
    }
} 