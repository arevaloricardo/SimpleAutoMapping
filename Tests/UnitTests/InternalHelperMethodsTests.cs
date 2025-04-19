using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections;

namespace SimpleAutoMapping.Tests.UnitTests
{
    [TestClass]
    public class InternalHelperMethodsTests
    {
        [TestMethod]
        public void IsCollection_WithStringType_ShouldReturnFalse()
        {
            // Arrange
            var type = typeof(string);
            
            // Act
            var result = Mapper.IsCollection(type);
            
            // Assert
            Assert.IsFalse(result, "El método IsCollection no debe tratar string como colección");
        }
        
        [TestMethod]
        public void IsCollection_WithArrayType_ShouldReturnTrue()
        {
            // Arrange
            var type = typeof(int[]);
            
            // Act
            var result = Mapper.IsCollection(type);
            
            // Assert
            Assert.IsTrue(result, "El método IsCollection debe identificar arrays como colecciones");
        }
        
        [TestMethod]
        public void IsCollection_WithListType_ShouldReturnTrue()
        {
            // Arrange
            var type = typeof(List<string>);
            
            // Act
            var result = Mapper.IsCollection(type);
            
            // Assert
            Assert.IsTrue(result, "El método IsCollection debe identificar List<T> como colecciones");
        }
        
        [TestMethod]
        public void IsCollection_WithDictionaryType_ShouldReturnTrue()
        {
            // Arrange
            var type = typeof(Dictionary<string, int>);
            
            // Act
            var result = Mapper.IsCollection(type);
            
            // Assert
            Assert.IsTrue(result, "El método IsCollection debe identificar Dictionary<K,V> como colecciones");
        }
        
        [TestMethod]
        public void GetElementType_WithArrayType_ShouldReturnElementType()
        {
            // Arrange
            var arrayType = typeof(int[]);
            
            // Act
            var result = Mapper.GetElementType(arrayType);
            
            // Assert
            Assert.AreEqual(typeof(int), result, "GetElementType debe devolver el tipo de elemento correcto para arrays");
        }
        
        [TestMethod]
        public void GetElementType_WithListType_ShouldReturnElementType()
        {
            // Arrange
            var listType = typeof(List<string>);
            
            // Act
            var result = Mapper.GetElementType(listType);
            
            // Assert
            Assert.AreEqual(typeof(string), result, "GetElementType debe devolver el tipo de elemento correcto para List<T>");
        }
        
        [TestMethod]
        public void GetElementType_WithIEnumerableType_ShouldReturnElementType()
        {
            // Arrange
            var enumerableType = typeof(IEnumerable<DateTime>);
            
            // Act
            var result = Mapper.GetElementType(enumerableType);
            
            // Assert
            Assert.AreEqual(typeof(DateTime), result, "GetElementType debe devolver el tipo de elemento correcto para IEnumerable<T>");
        }
        
        [TestMethod]
        public void GetElementType_WithNonGenericIEnumerable_ShouldReturnObjectType()
        {
            // Arrange
            var nonGenericType = typeof(ArrayList);
            
            // Act
            var result = Mapper.GetElementType(nonGenericType);
            
            // Assert
            Assert.AreEqual(typeof(object), result, "GetElementType debe devolver object para colecciones no genéricas");
        }
    }
} 