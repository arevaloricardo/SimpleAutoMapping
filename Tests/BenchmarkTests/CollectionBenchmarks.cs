using BenchmarkDotNet.Attributes;
using SimpleAutoMapping.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleAutoMapping.Tests.BenchmarkTests
{
    [MemoryDiagnoser]
    public class CollectionBenchmarks
    {
        private List<SimpleSource> _smallList;
        private List<SimpleSource> _largeList;
        private List<SimpleSource> _millionList;
        private Order _orderWithProducts;
        private Order _complexOrderWithProducts;
        
        [GlobalSetup]
        public void Setup()
        {
            // Configurar el mapper
            Mapper.Configuration.AddProfile<TestMappingProfile>();
            
            // Preparar datos de prueba
            _smallList = Enumerable.Range(1, 10).Select(i => new SimpleSource
            {
                Id = i,
                Name = $"Item {i}",
                CreatedDate = DateTime.Now.AddDays(-i),
                IsActive = i % 2 == 0
            }).ToList();
            
            _largeList = Enumerable.Range(1, 1000).Select(i => new SimpleSource
            {
                Id = i,
                Name = $"Item {i}",
                CreatedDate = DateTime.Now.AddDays(-i),
                IsActive = i % 2 == 0
            }).ToList();
            
            // Crear la lista de 1 millón de registros
            _millionList = Enumerable.Range(1, 1_000_000).Select(i => new SimpleSource
            {
                Id = i,
                Name = $"Item {i}",
                CreatedDate = DateTime.Now.AddDays(-i % 365),
                IsActive = i % 2 == 0
            }).ToList();
            
            _orderWithProducts = new Order
            {
                Id = 1,
                OrderDate = DateTime.Now,
                Products = Enumerable.Range(1, 20).Select(i => new Product
                {
                    Id = i,
                    Name = $"Product {i}",
                    Price = i * 10.99m
                }).ToList(),
                Metadata = new Dictionary<string, string>
                {
                    { "Status", "Pending" },
                    { "Customer", "John Doe" },
                    { "Priority", "High" }
                }
            };
            
            // Crear una orden más compleja con productos anidados
            _complexOrderWithProducts = new Order
            {
                Id = 2,
                OrderDate = DateTime.Now,
                Products = Enumerable.Range(1, 50).Select(i => new Product
                {
                    Id = i,
                    Name = $"Complex Product {i}",
                    Price = i * 15.99m,
                    Categories = Enumerable.Range(1, 5).Select(j => new Category
                    {
                        Id = j,
                        Name = $"Category {j} for Product {i}"
                    }).ToList()
                }).ToList(),
                Metadata = new Dictionary<string, string>
                {
                    { "Status", "Processing" },
                    { "Customer", "Jane Smith" },
                    { "Priority", "Medium" },
                    { "Notes", "Special handling required" }
                }
            };
        }
        
        [Benchmark]
        public List<SimpleDestination> Map_SmallList()
        {
            return Mapper.Map<List<SimpleSource>, List<SimpleDestination>>(_smallList);
        }
        
        [Benchmark]
        public List<SimpleDestination> MapManually_SmallList()
        {
            return _smallList.Select(src => new SimpleDestination
            {
                Id = src.Id,
                Name = src.Name,
                CreatedDate = src.CreatedDate,
                IsActive = src.IsActive
            }).ToList();
        }
        
        [Benchmark]
        public List<SimpleDestination> Map_LargeList()
        {
            return Mapper.Map<List<SimpleSource>, List<SimpleDestination>>(_largeList);
        }
        
        [Benchmark]
        public OrderDto Map_NestedCollections()
        {
            return Mapper.Map<Order, OrderDto>(_orderWithProducts);
        }
        
        [Benchmark]
        public OrderDto Map_ComplexNestedCollections()
        {
            return Mapper.Map<Order, OrderDto>(_complexOrderWithProducts);
        }
        
        [Benchmark]
        public List<SimpleDestination> Map_MillionRecords()
        {
            return Mapper.Map<List<SimpleSource>, List<SimpleDestination>>(_millionList);
        }
    }
} 