// Tests para métodos que están siendo llamados pero no implementados
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleAutoMapping;

namespace Tests.UnitTests
{
    // Modelos de prueba definidos localmente
    public class SimpleSource
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class SimpleDestination
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class ComplexSourceWithCollection
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<SimpleSource>? Items { get; set; }
    }

    public class ComplexDestinationWithCollection
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<SimpleDestination>? Items { get; set; }
    }

    [TestClass]
    public class MissingMethods
    {
        [TestMethod]
        public void MapCollectionFast_ShouldWork()
        {
            // En MapCollectionOptimized se llama a MapCollectionFast
            // Ahora que está implementado, debería funcionar correctamente
            
            // Arrange
            var source = new ComplexSourceWithCollection
            {
                Id = 1,
                Name = "Source",
                Items = new List<SimpleSource>
                {
                    new SimpleSource { Id = 1, Name = "Item 1" },
                    new SimpleSource { Id = 2, Name = "Item 2" }
                }
            };
            
            // Registrar mapeos con Map directamente (configuración inline)
            Mapper.Map<SimpleSource, SimpleDestination>(null, null, options => { });
            
            // Act (configuración inline para el mapeo principal)
            var result = Mapper.Map<ComplexSourceWithCollection, ComplexDestinationWithCollection>(
                source, 
                null, 
                options => { }
            );
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(source.Id, result.Id);
            Assert.AreEqual(source.Name, result.Name);
            Assert.IsNotNull(result.Items);
            Assert.AreEqual(source.Items.Count, result.Items.Count);
            
            for (int i = 0; i < source.Items.Count; i++)
            {
                Assert.AreEqual(source.Items[i].Id, result.Items[i].Id);
                Assert.AreEqual(source.Items[i].Name, result.Items[i].Name);
            }
            
            // Limpiar caché para no afectar otras pruebas
            Mapper.ClearCaches();
        }
        
        [TestMethod]
        public void MapCollectionFast_DirectInvocation_ShouldWork()
        {
            // Arrange - Crear colecciones fuente y destino
            var sourceCollection = new List<SimpleSource>
            {
                new SimpleSource { Id = 1, Name = "Item 1" },
                new SimpleSource { Id = 2, Name = "Item 2" }
            };
            
            var destCollection = new List<SimpleDestination>();
            
            // Obtener el método MapCollectionFast por reflexión
            MethodInfo methodInfo = typeof(Mapper).GetMethod("MapCollectionFast", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            Assert.IsNotNull(methodInfo, "El método MapCollectionFast debería existir");
            
            // Invocar el método directamente
            methodInfo.Invoke(null, new object[] { 
                sourceCollection, 
                destCollection, 
                typeof(List<SimpleDestination>), 
                false, // ignoreNulls
                Mapper.Configuration
            });
            
            // Assert
            Assert.AreEqual(2, destCollection.Count, "La colección destino debería tener 2 elementos");
            Assert.AreEqual(1, destCollection[0].Id, "El Id del primer elemento debe ser 1");
            Assert.AreEqual("Item 1", destCollection[0].Name, "El nombre del primer elemento debe ser Item 1");
            Assert.AreEqual(2, destCollection[1].Id, "El Id del segundo elemento debe ser 2");
            Assert.AreEqual("Item 2", destCollection[1].Name, "El nombre del segundo elemento debe ser Item 2");
        }
    }
} 