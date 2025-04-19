using System;
using System.Collections.Generic;

namespace SimpleAutoMapping.Tests.TestModels
{
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
    }

    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<Category>? Categories { get; set; }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<CategoryDto>? Categories { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public required List<Product> Products { get; set; } = new List<Product>();
        public required Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public required List<ProductDto> Products { get; set; } = new List<ProductDto>();
        public required Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
} 