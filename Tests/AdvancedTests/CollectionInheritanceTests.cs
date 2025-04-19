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
    
    public class CollectionInheritanceProfile : SimpleAutoMappingProfile
    {
        public CollectionInheritanceProfile()
        {
            CreateMap<BaseItem, BaseItemDto>();
            CreateMap<DerivedItem, DerivedItemDto>();
            CreateMap<ContainerWithCollection, ContainerWithCollectionDto>();
            
            // Registrar mapeo con inferencia de tipos
            CreateMap<BaseItem, DerivedItemDto>()
                .AddResolver(dest => dest.ExtraProperty, 
                    src => src is DerivedItem derived ? derived.ExtraProperty : "No extra data");
        }
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
            
            Assert.AreEqual(2, result.Items[1].Id);
            Assert.AreEqual("Derived Item", result.Items[1].Name);
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
            Assert.AreEqual(2, result[1].Id);
            Assert.AreEqual("Item 2", result[1].Name);
        }
        
        [TestMethod]
        public void Map_ArrayToList_ShouldMapElementsCorrectly()
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
            Assert.AreEqual(2, result[1].Id);
            Assert.AreEqual("Array Item 2", result[1].Name);
        }
        
        [TestMethod]
        public void Map_ListToArray_ShouldMapElementsCorrectly()
        {
            // Arrange
            var sourceList = new List<BaseItem>
            {
                new BaseItem { Id = 1, Name = "List Item 1" },
                new DerivedItem { Id = 2, Name = "List Item 2", ExtraProperty = "List Extra" }
            };
            
            // Act - Creamos primero una lista y luego convertimos a array
            var resultList = Mapper.Map<List<BaseItem>, List<BaseItemDto>>(sourceList);
            var result = resultList.ToArray();
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(1, result[0].Id);
            Assert.AreEqual("List Item 1", result[0].Name);
            Assert.AreEqual(2, result[1].Id);
            Assert.AreEqual("List Item 2", result[1].Name);
        }
    }
} 