using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleAutoMapping.Tests.TestModels;

namespace SimpleAutoMapping.Tests.UnitTests
{
    [TestClass]
    public class PartialMappingTests
    {
        [TestInitialize]
        public void Setup()
        {
            Mapper.Configuration.AddProfile<TestMappingProfile>();
        }
        
        [TestMethod]
        public void PartialMap_WithNullValues_ShouldIgnoreNulls()
        {
            // Arrange
            var source = new SimpleSource { Id = 1, Name = null };
            var destination = new SimpleDestination { Id = 5, Name = "Original" };
            
            // Act
            var result = Mapper.PartialMap(source, destination);
            
            // Assert
            Assert.AreEqual(1, result.Id); // Debería actualizarse
            Assert.AreEqual("Original", result.Name); // No debería cambiar
        }
        
        [TestMethod]
        public void PartialMapTo_ExtensionMethod_ShouldIgnoreNulls()
        {
            // Arrange
            var source = new SimpleSource { Id = 1, Name = null };
            var destination = new SimpleDestination { Id = 5, Name = "Original" };
            
            // Act
            var result = source.PartialMapTo(destination);
            
            // Assert
            Assert.AreEqual(1, result.Id);
            Assert.AreEqual("Original", result.Name);
        }
        
        [TestMethod]
        public void PartialMapTo_WithNewDestination_ShouldCreateAndMapNonNulls()
        {
            // Arrange
            var source = new SimpleSource { Id = 1, Name = "Test", IsActive = true, CreatedDate = DateTime.MinValue };
            
            // Act
            var result = source.PartialMapTo<SimpleDestination>();
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Id);
            Assert.AreEqual("Test", result.Name);
            Assert.AreEqual(true, result.IsActive);
            Assert.AreEqual(DateTime.MinValue, result.CreatedDate);
        }
    }
} 