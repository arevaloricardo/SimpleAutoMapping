using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using SimpleAutoMapping.Tests.TestModels;

namespace SimpleAutoMapping.Tests.UnitTests
{
    [TestClass]
    public class ConfigurationMethodsTests
    {
        private SimpleAutoMappingConfiguration _configuration;
        
        [TestInitialize]
        public void Setup()
        {
            // Crear una nueva configuración para cada prueba
            _configuration = new SimpleAutoMappingConfiguration();
            
            // Registrar algunos mapeos para las pruebas
            var options = new MappingOptions<SimpleSource, SimpleDestination>();
            _configuration.RegisterMapping(typeof(SimpleSource), typeof(SimpleDestination), options);
            
            var complexOptions = new MappingOptions<Person, PersonDto>();
            _configuration.RegisterMapping(typeof(Person), typeof(PersonDto), complexOptions);
        }
        
        [TestMethod]
        public void FindDestinationType_WithRegisteredType_ShouldReturnDestinationType()
        {
            // Act
            var result = _configuration.FindDestinationType(typeof(SimpleSource));
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(typeof(SimpleDestination), result);
        }
        
        [TestMethod]
        public void FindDestinationType_WithUnregisteredType_ShouldReturnNull()
        {
            // Act
            var result = _configuration.FindDestinationType(typeof(string));
            
            // Assert
            Assert.IsNull(result);
        }
        
        [TestMethod]
        public void FindDestinationType_WithRegisteredCollectionElementType_ShouldReturnCollectionDestinationType()
        {
            // Arrange
            // Registrar el tipo de colección primero para asegurarnos de que funciona con el tipo de elemento
            var personCollectionOptions = new MappingOptions<List<Person>, List<PersonDto>>();
            _configuration.RegisterMapping(typeof(List<Person>), typeof(List<PersonDto>), personCollectionOptions);
            
            // Act
            var result = _configuration.FindDestinationType(typeof(List<Person>));
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(typeof(List<PersonDto>), result);
        }
        
        [TestMethod]
        public void FindDestinationType_WithUnregisteredCollectionButRegisteredElementType_ShouldReturnConstructedCollectionType()
        {
            // Arrange
            // No registramos el tipo completo de colección, solo el elemento
            // Person -> PersonDto ya está registrado en Setup()
            
            // Act - Intentar inferir el tipo de destino para un array de Person
            var result = _configuration.FindDestinationType(typeof(Person[]));
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsGenericType);
            Assert.AreEqual(typeof(List<>), result.GetGenericTypeDefinition());
            Assert.AreEqual(typeof(PersonDto), result.GetGenericArguments()[0]);
        }
        
        [TestMethod]
        public void FindDestinationType_WithDifferentCollectionTypesButSameElementType_ShouldWork()
        {
            // Arrange - Person -> PersonDto ya está registrado en Setup()
            
            // Act - Probar con varios tipos de colección
            var arrayResult = _configuration.FindDestinationType(typeof(Person[]));
            var listResult = _configuration.FindDestinationType(typeof(List<Person>));
            var ienumerableResult = _configuration.FindDestinationType(typeof(IEnumerable<Person>));
            
            // Assert - Todos deberían generar el mismo tipo destino: List<PersonDto>
            Assert.IsNotNull(arrayResult);
            Assert.IsNotNull(listResult);
            Assert.IsNotNull(ienumerableResult);
            
            // Verificar que todos son List<PersonDto>
            Assert.IsTrue(arrayResult.IsGenericType);
            Assert.AreEqual(typeof(List<>), arrayResult.GetGenericTypeDefinition());
            Assert.AreEqual(typeof(PersonDto), arrayResult.GetGenericArguments()[0]);
            
            Assert.IsTrue(listResult.IsGenericType);
            Assert.AreEqual(typeof(List<>), listResult.GetGenericTypeDefinition());
            Assert.AreEqual(typeof(PersonDto), listResult.GetGenericArguments()[0]);
            
            Assert.IsTrue(ienumerableResult.IsGenericType);
            Assert.AreEqual(typeof(List<>), ienumerableResult.GetGenericTypeDefinition());
            Assert.AreEqual(typeof(PersonDto), ienumerableResult.GetGenericArguments()[0]);
        }
    }
} 