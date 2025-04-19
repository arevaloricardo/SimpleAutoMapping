using BenchmarkDotNet.Attributes;
using SimpleAutoMapping.Tests.TestModels;
using System;

namespace SimpleAutoMapping.Tests.BenchmarkTests
{
    [MemoryDiagnoser]
    public class MappingBenchmarks
    {
        private SimpleSource _simpleSource;
        private Person _complexPerson;
        
        [GlobalSetup]
        public void Setup()
        {
            // Configurar el mapper
            Mapper.Configuration.AddProfile<TestMappingProfile>();
            
            // Preparar datos de prueba
            _simpleSource = new SimpleSource 
            { 
                Id = 1, 
                Name = "Test", 
                CreatedDate = DateTime.Now, 
                IsActive = true 
            };
            
            _complexPerson = new Person
            {
                Id = 1,
                Name = "John Doe",
                Address = new Address
                {
                    Street = "123 Main St",
                    City = "New York",
                    ZipCode = "10001"
                },
                PhoneNumbers = new System.Collections.Generic.List<string> { "555-1234", "555-5678" }
            };
        }
        
        [Benchmark]
        public SimpleDestination Map_SimpleObject()
        {
            return Mapper.Map<SimpleSource, SimpleDestination>(_simpleSource);
        }
        
        [Benchmark]
        public SimpleDestination MapManually_SimpleObject()
        {
            return new SimpleDestination
            {
                Id = _simpleSource.Id,
                Name = _simpleSource.Name,
                CreatedDate = _simpleSource.CreatedDate,
                IsActive = _simpleSource.IsActive
            };
        }
        
        [Benchmark]
        public PersonDto Map_ComplexObject()
        {
            return Mapper.Map<Person, PersonDto>(_complexPerson);
        }
        
        [Benchmark]
        public PersonDto MapManually_ComplexObject()
        {
            return new PersonDto
            {
                Id = _complexPerson.Id,
                Name = _complexPerson.Name,
                Address = new AddressDto
                {
                    Street = _complexPerson.Address.Street,
                    City = _complexPerson.Address.City,
                    ZipCode = _complexPerson.Address.ZipCode
                },
                PhoneNumbers = new System.Collections.Generic.List<string>(_complexPerson.PhoneNumbers)
            };
        }
        
        [Benchmark]
        public SimpleDestination PartialMap_SimpleObject()
        {
            var destination = new SimpleDestination { Id = 5, Name = "Original" };
            return Mapper.PartialMap(_simpleSource, destination);
        }
    }
} 