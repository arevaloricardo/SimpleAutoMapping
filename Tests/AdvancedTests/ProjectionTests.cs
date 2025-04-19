using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleAutoMapping.Tests.AdvancedTests
{
    [TestClass]
    public class ProjectionTests
    {
        [TestInitialize]
        public void Setup()
        {
            // Limpiar configuración antes de cada prueba
            Mapper.ClearCaches();
        }

        [TestMethod]
        public void ProjectTo_SimpleProperties_MapsCorrectly()
        {
            // Arrange
            Mapper.Configuration.AddProfile<SimpleUserProfile>();
            
            var users = new List<UserEntity>
            {
                new UserEntity { Id = 1, Name = "John", Email = "john@example.com", CreatedDate = DateTime.Now.AddDays(-5) },
                new UserEntity { Id = 2, Name = "Jane", Email = "jane@example.com", CreatedDate = DateTime.Now.AddDays(-3) }
            }.AsQueryable();
            
            // Act
            var result = users.ProjectTo<UserEntity, UserDto>().ToList();
            
            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("John", result[0].Name);
            Assert.AreEqual("john@example.com", result[0].Email);
            Assert.AreEqual(1, result[0].Id);
            
            Assert.AreEqual("Jane", result[1].Name);
            Assert.AreEqual("jane@example.com", result[1].Email);
            Assert.AreEqual(2, result[1].Id);
        }
        
        [TestMethod]
        public void ProjectTo_CustomPropertyMapping_MapsCorrectly()
        {
            // Arrange
            Mapper.Configuration.AddProfile<UserViewModelProfile>();
            
            var users = new List<UserEntity>
            {
                new UserEntity { Id = 1, Name = "John Doe", Email = "john@example.com" },
                new UserEntity { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
            }.AsQueryable();
            
            // Act
            var result = users.ProjectTo<UserEntity, UserViewModel>().ToList();
            
            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("John Doe", result[0].FullName);
            Assert.AreEqual("john@example.com", result[0].EmailAddress);
            
            Assert.AreEqual("Jane Smith", result[1].FullName);
            Assert.AreEqual("jane@example.com", result[1].EmailAddress);
        }
        
        [TestMethod]
        public void ProjectTo_WithFiltering_AppliesFilter()
        {
            // Arrange
            Mapper.Configuration.AddProfile<SimpleUserProfile>();
            
            var users = new List<UserEntity>
            {
                new UserEntity { Id = 1, Name = "John", Email = "john@example.com", IsActive = true },
                new UserEntity { Id = 2, Name = "Jane", Email = "jane@example.com", IsActive = false },
                new UserEntity { Id = 3, Name = "Bob", Email = "bob@example.com", IsActive = true }
            }.AsQueryable();
            
            // Act
            var result = users.Where(u => u.IsActive)
                              .ProjectTo<UserEntity, UserDto>()
                              .ToList();
            
            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(u => u.Name == "John"));
            Assert.IsTrue(result.Any(u => u.Name == "Bob"));
            Assert.IsFalse(result.Any(u => u.Name == "Jane"));
        }
        
        [TestMethod]
        public void ProjectTo_WithOrderingAndPaging_Works()
        {
            // Arrange
            Mapper.Configuration.AddProfile<SimpleUserProfile>();
            
            var users = new List<UserEntity>
            {
                new UserEntity { Id = 1, Name = "John", Points = 100 },
                new UserEntity { Id = 2, Name = "Jane", Points = 300 },
                new UserEntity { Id = 3, Name = "Bob", Points = 200 },
                new UserEntity { Id = 4, Name = "Alice", Points = 400 },
                new UserEntity { Id = 5, Name = "Tom", Points = 50 }
            }.AsQueryable();
            
            // Act - obtener los 2 usuarios con mayor puntuación
            var result = users.OrderByDescending(u => u.Points)
                              .Take(2)
                              .ProjectTo<UserEntity, UserDto>()
                              .ToList();
            
            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Alice", result[0].Name);
            Assert.AreEqual("Jane", result[1].Name);
        }
        
        [TestMethod]
        public void ProjectTo_SimpleVersion_UsesInference()
        {
            // Arrange
            var profile = new ProjectionTestProfile();
            Mapper.Configuration.AddProfile<ProjectionTestProfile>();
            
            var users = new List<UserEntity>
            {
                new UserEntity { Id = 1, Name = "John", Email = "john@example.com" }
            }.AsQueryable();
            
            // Act - usar la versión simplificada que infiere los tipos
            var result = users.ProjectTo<UserViewModel>().ToList();
            
            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("John", result[0].FullName);
            Assert.AreEqual("john@example.com", result[0].EmailAddress);
        }
        
        // Clases de prueba
        public class UserEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public bool IsActive { get; set; }
            public int Points { get; set; }
            public DateTime CreatedDate { get; set; }
        }
        
        public class UserDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public bool IsActive { get; set; }
            public int Points { get; set; }
        }
        
        public class UserViewModel
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string EmailAddress { get; set; }
        }
        
        // Perfiles de mapeo para pruebas
        public class SimpleUserProfile : SimpleAutoMappingProfile
        {
            public SimpleUserProfile()
            {
                CreateMap<UserEntity, UserDto>();
            }
        }
        
        public class UserViewModelProfile : SimpleAutoMappingProfile
        {
            public UserViewModelProfile()
            {
                CreateMap<UserEntity, UserViewModel>()
                    .ConfigProperty(s => s.Name, d => d.FullName)
                    .ConfigProperty(s => s.Email, d => d.EmailAddress);
            }
        }
        
        // Perfil de mapeo para pruebas
        public class ProjectionTestProfile : SimpleAutoMappingProfile
        {
            public ProjectionTestProfile()
            {
                CreateMap<UserEntity, UserViewModel>()
                    .ConfigProperty(s => s.Name, d => d.FullName)
                    .ConfigProperty(s => s.Email, d => d.EmailAddress);
            }
        }
    }
} 