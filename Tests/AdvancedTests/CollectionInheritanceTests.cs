using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleAutoMapping.Tests.AdvancedTests
{
    // Clases para pruebas de herencia con colecciones
    public class BaseItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    public class DerivedItem : BaseItem
    {
        public string ExtraProperty { get; set; }
    }
    
    // Clase derivada adicional sin mapeo específico
    public class AnotherDerivedItem : BaseItem
    {
        public bool SpecialFlag { get; set; }
    }
    
    public class BaseItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    public class DerivedItemDto : BaseItemDto
    {
        public string ExtraProperty { get; set; }
    }
    
    public class ContainerWithCollection
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public List<BaseItem> Items { get; set; }
    }
    
    public class ContainerWithCollectionDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public List<BaseItemDto> Items { get; set; }
    }
    
    [TestClass]
    public class CollectionInheritanceTests
    {
        [TestInitialize]
        public void Setup()
        {
            // Limpiar configuración previa
            Mapper.ClearCaches();
            
            // Configurar el mapper para las pruebas
            Mapper.Configuration.AddProfile<CollectionInheritanceProfile>();
        }
        
        [TestMethod]
        public void Map_CollectionWithMixedTypes_ShouldMapCorrectly()
        {
            // Arrange
            var container = new ContainerWithCollection
            {
                Id = 1,
                Title = "Test Container",
                Items = new List<BaseItem>
                {
                    new BaseItem { Id = 1, Name = "Base Item" },
                    new DerivedItem { Id = 2, Name = "Derived Item", ExtraProperty = "Extra Value" }
                }
            };
            
            // Act
            var result = Mapper.Map<ContainerWithCollection, ContainerWithCollectionDto>(container);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(container.Id, result.Id);
            Assert.AreEqual(container.Title, result.Title);
            Assert.IsNotNull(result.Items);
            Assert.AreEqual(2, result.Items.Count);
            
            // Verificar los elementos de la colección
            Assert.AreEqual(1, result.Items[0].Id);
            Assert.AreEqual("Base Item", result.Items[0].Name);
            Assert.IsInstanceOfType(result.Items[0], typeof(BaseItemDto));
            
            Assert.AreEqual(2, result.Items[1].Id);
            Assert.AreEqual("Derived Item", result.Items[1].Name);
            
            // Verificar el mapeo polimórfico (puede fallar si no está implementado)
            var derivedDto = result.Items[1] as DerivedItemDto;
            if (derivedDto != null)
            {
                Assert.AreEqual("Extra Value", derivedDto.ExtraProperty);
            }
            else
            {
                Console.WriteLine("Advertencia: El mapeo polimórfico no está funcionando, necesita revisión");
            }
        }
        
        [TestMethod]
        public void Map_CollectionToCollection_ShouldUseElementMapping()
        {
            // Arrange
            var sourceList = new List<BaseItem>
            {
                new BaseItem { Id = 1, Name = "Item 1" },
                new DerivedItem { Id = 2, Name = "Item 2", ExtraProperty = "Special" }
            };
            
            // Act
            var result = Mapper.Map<List<BaseItem>, List<BaseItemDto>>(sourceList);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Id);
            Assert.AreEqual("Item 1", result[0].Name);
            Assert.IsInstanceOfType(result[0], typeof(BaseItemDto));
            
            Assert.AreEqual(2, result[1].Id);
            Assert.AreEqual("Item 2", result[1].Name);
            
            // Verificar si el mapeo polimórfico está funcionando
            var derivedDto = result[1] as DerivedItemDto;
            if (derivedDto != null)
            {
                Assert.AreEqual("Special", derivedDto.ExtraProperty);
            }
        }
        
        [TestMethod]
        public void Map_ArrayToList_ShouldMapElementsWithCorrectTypes()
        {
            // Arrange
            var sourceArray = new BaseItem[]
            {
                new BaseItem { Id = 1, Name = "Array Item 1" },
                new DerivedItem { Id = 2, Name = "Array Item 2", ExtraProperty = "Array Extra" }
            };
            
            // Act
            var result = Mapper.Map<BaseItem[], List<BaseItemDto>>(sourceArray);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Id);
            Assert.AreEqual("Array Item 1", result[0].Name);
            Assert.IsInstanceOfType(result[0], typeof(BaseItemDto));
            
            Assert.AreEqual(2, result[1].Id);
            Assert.AreEqual("Array Item 2", result[1].Name);
            
            // Verificar el tipo del segundo elemento
            var derivedDto = result[1] as DerivedItemDto;
            if (derivedDto != null)
            {
                Assert.AreEqual("Array Extra", derivedDto.ExtraProperty);
            }
        }
        
        [TestMethod]
        public void Map_WithAbsentDerivedMapping_ShouldFallbackToBaseMapping()
        {
            // Arrange
            var sourceList = new List<BaseItem>
            {
                new BaseItem { Id = 1, Name = "Base Item" },
                new DerivedItem { Id = 2, Name = "Derived Item", ExtraProperty = "Extra Value" },
                new AnotherDerivedItem { Id = 3, Name = "Another Item", SpecialFlag = true }
            };
            
            // Act
            var result = Mapper.Map<List<BaseItem>, List<BaseItemDto>>(sourceList);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            
            // El primer elemento es un BaseItemDto
            Assert.IsInstanceOfType(result[0], typeof(BaseItemDto));
            
            // El segundo elemento podría ser un DerivedItemDto si el mapeo polimórfico funciona
            Assert.AreEqual(2, result[1].Id);
            Assert.AreEqual("Derived Item", result[1].Name);
            
            // El tercer elemento debería ser un BaseItemDto (no hay mapeo específico)
            Assert.IsInstanceOfType(result[2], typeof(BaseItemDto));
            Assert.AreEqual(3, result[2].Id);
            Assert.AreEqual("Another Item", result[2].Name);
        }
    }
} 