using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SimpleAutoMapping.Tests.UnitTests
{
    [TestClass]
    public class TypeConversionTests
    {
        // Modelos específicos para pruebas de conversión
        class SourceWithTypes
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; } = string.Empty;
            public DateTime DateValue { get; set; }
            public decimal DecimalValue { get; set; }
            public bool BoolValue { get; set; }
            public int? NullableInt { get; set; }
            public MyEnum EnumValue { get; set; }
        }
        
        class DestWithTypes
        {
            public string IntValue { get; set; } = string.Empty;
            public int StringValue { get; set; }
            public string DateValue { get; set; } = string.Empty;
            public string DecimalValue { get; set; } = string.Empty;
            public string BoolValue { get; set; } = string.Empty;
            public int NullableInt { get; set; }
            public int EnumValue { get; set; }
        }
        
        enum MyEnum
        {
            One = 1,
            Two = 2
        }
        
        [TestMethod]
        public void Map_WithTypeConversions_ShouldConvertAutomatically()
        {
            // Arrange
            var source = new SourceWithTypes
            {
                IntValue = 123,
                StringValue = "456",
                DateValue = new DateTime(2023, 1, 15),
                DecimalValue = 123.45m,
                BoolValue = true,
                NullableInt = 789,
                EnumValue = MyEnum.Two
            };
            
            // Configure custom type converters
            Mapper.Configuration.RegisterTypeConverter<DateTime, string>(d => d.ToString("yyyy-MM-dd"));
            Mapper.Configuration.RegisterTypeConverter<decimal, string>(d => $"${d}");
            Mapper.Configuration.RegisterTypeConverter<bool, string>(b => b ? "Yes" : "No");
            Mapper.Configuration.RegisterTypeConverter<MyEnum, int>(e => (int)e);
            
            // Act
            var result = Mapper.Map<SourceWithTypes, DestWithTypes>(source);
            
            // Assert
            Assert.AreEqual("123", result.IntValue);
            Assert.AreEqual(456, result.StringValue);
            Assert.AreEqual("2023-01-15", result.DateValue);
            Assert.AreEqual("$123.45", result.DecimalValue);
            Assert.AreEqual("Yes", result.BoolValue);
            Assert.AreEqual(789, result.NullableInt);
            Assert.AreEqual(2, result.EnumValue);
        }
        
        [TestMethod]
        public void Map_WithNullableToNonNullable_ShouldHandleCorrectly()
        {
            // Arrange
            var source = new SourceWithTypes
            {
                NullableInt = null
            };
            
            // Act
            var result = Mapper.Map<SourceWithTypes, DestWithTypes>(source);
            
            // Assert
            Assert.AreEqual(0, result.NullableInt); // Valor por defecto para int
        }
    }
} 