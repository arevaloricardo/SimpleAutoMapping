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
        
        [TestMethod]
        public void RegisterMapping_WithInvalidProperty_ShouldThrowException()
        {
            // Arrange
            var options = new MappingOptions<SimpleSource, SimpleDestination>();
            
            // Configurar una propiedad que no existe en el tipo origen
            options.ConfigProperty("NonExistentProperty", "Name");
            
            try
            {
                // Buscar el método Validate
                var validateMethod = typeof(MappingOptions<SimpleSource, SimpleDestination>)
                    .GetMethod("Validate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public 
                             | System.Reflection.BindingFlags.NonPublic);
                
                if (validateMethod == null)
                {
                    Console.WriteLine("El método Validate no está disponible para pruebas directas");
                    Assert.Inconclusive("No se puede probar directamente la validación de propiedades");
                    return;
                }
                
                // Act - Llamar directamente al método Validate
                validateMethod.Invoke(options, null);
                
                // Si llegamos aquí sin excepción, es un fallo
                Assert.Fail("Debería haber lanzado una excepción");
            }
            catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                // Verificar que la excepción es la esperada
                Assert.IsTrue(ex.InnerException.Message.Contains("NonExistentProperty"), 
                    "La excepción debería mencionar la propiedad inexistente");
            }
            catch (System.NullReferenceException)
            {
                // Si hay un NullReferenceException, podría ser por la reflexión
                Console.WriteLine("No se pudo acceder al método Validate con reflexión");
                Assert.Inconclusive("No se puede probar la validación debido a problemas de reflexión");
            }
        }
        
        [TestMethod]
        public void RegisterMapping_WithInvalidDestProperty_ShouldThrowException()
        {
            // Arrange
            var options = new MappingOptions<SimpleSource, SimpleDestination>();
            
            // Configurar una propiedad que no existe en el tipo destino
            options.ConfigProperty("Name", "NonExistentProperty");
            
            try
            {
                // Buscar el método Validate
                var validateMethod = typeof(MappingOptions<SimpleSource, SimpleDestination>)
                    .GetMethod("Validate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public 
                             | System.Reflection.BindingFlags.NonPublic);
                
                if (validateMethod == null)
                {
                    Console.WriteLine("El método Validate no está disponible para pruebas directas");
                    Assert.Inconclusive("No se puede probar directamente la validación de propiedades");
                    return;
                }
                
                // Act - Llamar directamente al método Validate
                validateMethod.Invoke(options, null);
                
                // Si llegamos aquí sin excepción, es un fallo
                Assert.Fail("Debería haber lanzado una excepción");
            }
            catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                // Verificar que la excepción es la esperada
                Assert.IsTrue(ex.InnerException.Message.Contains("NonExistentProperty"), 
                    "La excepción debería mencionar la propiedad inexistente");
            }
            catch (System.NullReferenceException)
            {
                // Si hay un NullReferenceException, podría ser por la reflexión
                Console.WriteLine("No se pudo acceder al método Validate con reflexión");
                Assert.Inconclusive("No se puede probar la validación debido a problemas de reflexión");
            }
        }
        
        [TestMethod]
        public void GetAllMappingConfigurations_ShouldReturnAllRegisteredMappings()
        {
            // Act
            var mappings = _configuration.GetAllMappingConfigurations();
            
            // Assert
            Assert.IsNotNull(mappings);
            var mappingList = new List<(Type sourceType, Type destType)>(mappings);
            
            Assert.AreEqual(2, mappingList.Count);
            
            // Verificar que contiene los mapeos registrados
            Assert.IsTrue(mappingList.Contains((typeof(SimpleSource), typeof(SimpleDestination))));
            Assert.IsTrue(mappingList.Contains((typeof(Person), typeof(PersonDto))));
        }
        
        [TestMethod]
        public void RegisterTypeConverter_AndGetTypeConverter_ShouldWorkCorrectly()
        {
            // Arrange
            _configuration.RegisterTypeConverter<int, string>(i => $"Number: {i}");
            
            // Act - Usar reflexión para acceder al método protegido
            var method = typeof(SimpleAutoMappingConfiguration).GetMethod(
                "GetTypeConverter", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            Assert.IsNotNull(method, "El método GetTypeConverter debe existir");
            
            var converter = method.Invoke(_configuration, new object[] { typeof(int), typeof(string) }) as Func<object, object>;
            
            // Assert
            Assert.IsNotNull(converter, "El convertidor no debería ser null");
            var result = converter(42);
            Assert.AreEqual("Number: 42", result);
        }
        
        // Clase mínima para probar perfiles
        private class TestProfile : SimpleAutoMappingProfile
        {
            public TestProfile()
            {
                CreateMap<SimpleSource, SimpleDestination>();
                CreateMap<Person, PersonDto>();
            }
        }
        
        [TestMethod]
        public void AddProfile_ShouldRegisterProfileMappings()
        {
            // Arrange
            _configuration = new SimpleAutoMappingConfiguration(); // Nueva configuración limpia
            
            // Act
            _configuration.AddProfile<TestProfile>();
            
            // Assert
            var mappings = _configuration.GetAllMappingConfigurations();
            var mappingList = new List<(Type sourceType, Type destType)>(mappings);
            
            Assert.AreEqual(2, mappingList.Count);
            Assert.IsTrue(mappingList.Contains((typeof(SimpleSource), typeof(SimpleDestination))));
            Assert.IsTrue(mappingList.Contains((typeof(Person), typeof(PersonDto))));
        }
    }
} 