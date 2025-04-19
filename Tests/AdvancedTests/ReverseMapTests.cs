using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SimpleAutoMapping.Tests.AdvancedTests
{
    [TestClass]
    public class ReverseMapTests
    {
        [TestInitialize]
        public void Setup()
        {
            // Limpiar configuración antes de cada prueba
            Mapper.ClearCaches();
        }

        [TestMethod]
        public void ReverseMap_BasicProperties_MapsInBothDirections()
        {
            // Arrange
            Mapper.Configuration.AddProfile<BasicReverseMapProfile>();
            
            var source = new SourceModel { Id = 1, Name = "Test User", Age = 30 };
            
            // Act
            var dest = Mapper.Map<SourceModel, DestModel>(source);
            var reversed = Mapper.Map<DestModel, SourceModel>(dest);
            
            // Assert
            Assert.AreEqual(source.Id, dest.Id);
            Assert.AreEqual(source.Name, dest.FullName);
            Assert.AreEqual(source.Age, dest.Age);
            
            Assert.AreEqual(source.Id, reversed.Id);
            Assert.AreEqual(source.Name, reversed.Name);
            Assert.AreEqual(source.Age, reversed.Age);
        }
        
        [TestMethod]
        public void ReverseMap_CustomPropertyMapping_AppliesInverse()
        {
            // Arrange
            Mapper.Configuration.AddProfile<BasicReverseMapProfile>();
            
            var source = new SourceModel { Id = 1, Name = "Test User" };
            var dest = new DestModel { Id = 2, FullName = "Other User" };
            
            // Act
            var mappedDest = source.MapTo<DestModel>();
            var mappedSource = mappedDest.MapTo<SourceModel>();
            
            // Assert
            Assert.AreEqual(source.Name, mappedDest.FullName);
            Assert.AreEqual(mappedDest.FullName, mappedSource.Name);
        }
        
        [TestMethod]
        public void ReverseMap_WithTransformations_DoesNotApplyTransformationsBackwards()
        {
            // Arrange
            Mapper.Configuration.AddProfile<TransformationReverseMapProfile>();
            
            var source = new SourceModel { Id = 1, Age = 30 };
            
            // Act
            var dest = Mapper.Map<SourceModel, DestModel>(source);
            var reversed = Mapper.Map<DestModel, SourceModel>(dest);
            
            // Assert
            Assert.AreEqual(60, dest.Age); // Transformado
            Assert.AreEqual(60, reversed.Age); // No se revierte la transformación
        }
        
        [TestMethod]
        public void ReverseMap_WithProfile_WorksInBothDirections()
        {
            // Arrange
            Mapper.Configuration.AddProfile<ReverseMapTestProfile>();
            
            var source = new SourceModel { Id = 1, Name = "Test User", Age = 30 };
            
            // Act
            var dest = source.MapTo<DestModel>();
            var reversed = dest.MapTo<SourceModel>();
            
            // Assert
            Assert.AreEqual(source.Id, dest.Id);
            Assert.AreEqual(source.Name, dest.FullName);
            Assert.AreEqual(source.Age, dest.Age);
            
            Assert.AreEqual(dest.Id, reversed.Id);
            Assert.AreEqual(dest.FullName, reversed.Name);
            Assert.AreEqual(dest.Age, reversed.Age);
        }
        
        // Clases de prueba
        public class SourceModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }
        
        public class DestModel
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public int Age { get; set; }
        }
        
        // Perfiles de mapeo para pruebas
        public class BasicReverseMapProfile : SimpleAutoMappingProfile
        {
            public BasicReverseMapProfile()
            {
                CreateMap<SourceModel, DestModel>()
                    .ConfigProperty(s => s.Name, d => d.FullName)
                    .ReverseMap();
            }
        }
        
        public class TransformationReverseMapProfile : SimpleAutoMappingProfile
        {
            public TransformationReverseMapProfile()
            {
                CreateMap<SourceModel, DestModel>()
                    .ConfigTransform(s => s.Age, age => age * 2)
                    .ReverseMap();
            }
        }
        
        // Perfil de mapeo para pruebas
        public class ReverseMapTestProfile : SimpleAutoMappingProfile
        {
            public ReverseMapTestProfile()
            {
                CreateMap<SourceModel, DestModel>()
                    .ConfigProperty(s => s.Name, d => d.FullName)
                    .ReverseMap();
            }
        }
    }
} 