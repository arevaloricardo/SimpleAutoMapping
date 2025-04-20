using BenchmarkDotNet.Attributes;
using SimpleAutoMapping.Tests.TestModels;
using AutoMapper;
using Mapster;
using System;
using System.Collections.Generic;

namespace SimpleAutoMapping.Tests.BenchmarkTests
{
    [MemoryDiagnoser]
    public class MapperComparison
    {
        // Objetos de origen para las pruebas
        private SimpleSource _simpleSource;
        private Person _complexPerson;
        private List<SimpleSource> _simpleList;
        private List<Person> _complexList;
        
        // Instancias para AutoMapper y configuración
        private AutoMapper.IMapper _autoMapper;
        
        // Objeto destino para reutilización en mapeos parciales
        private SimpleDestination _existingDestination;
        
        [GlobalSetup]
        public void Setup()
        {
            // 1. Configurar SimpleAutoMapping
            SimpleAutoMapping.Mapper.Configuration.AddProfile<TestMappingProfile>();
            
            // 2. Configurar AutoMapper
            var config = new AutoMapper.MapperConfiguration(cfg => 
            {
                // Mapeo simple
                cfg.CreateMap<SimpleSource, SimpleDestination>();
                
                // Mapeo complejo
                cfg.CreateMap<Person, PersonDto>();
                cfg.CreateMap<Address, AddressDto>();
            });
            _autoMapper = config.CreateMapper();
            
            // 3. Configurar Mapster
            TypeAdapterConfig.GlobalSettings.NewConfig<SimpleSource, SimpleDestination>();
            TypeAdapterConfig.GlobalSettings.NewConfig<Person, PersonDto>();
            TypeAdapterConfig.GlobalSettings.NewConfig<Address, AddressDto>();
            
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
                PhoneNumbers = new List<string> { "555-1234", "555-5678" }
            };
            
            // Crear listas para benchmarks de colección
            _simpleList = new List<SimpleSource>();
            for (int i = 0; i < 100; i++)
            {
                _simpleList.Add(new SimpleSource 
                { 
                    Id = i, 
                    Name = $"Test {i}", 
                    CreatedDate = DateTime.Now.AddDays(-i), 
                    IsActive = i % 2 == 0 
                });
            }
            
            _complexList = new List<Person>();
            for (int i = 0; i < 100; i++)
            {
                _complexList.Add(new Person
                {
                    Id = i,
                    Name = $"Person {i}",
                    Address = new Address
                    {
                        Street = $"{i} Main St",
                        City = i % 3 == 0 ? "New York" : i % 3 == 1 ? "Los Angeles" : "Chicago",
                        ZipCode = $"{10000 + i}"
                    },
                    PhoneNumbers = new List<string> { $"555-{1000 + i}", $"555-{2000 + i}" }
                });
            }
            
            // Objeto destino para mapeos parciales
            _existingDestination = new SimpleDestination { Id = 5, Name = "Original", IsActive = false };
        }
        
        #region Mapeo Simple (Objeto Plano)
        
        [Benchmark(Baseline = true)]
        public SimpleDestination Manual_SimpleObject()
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
        public SimpleDestination SimpleAutoMapping_SimpleObject()
        {
            return SimpleAutoMapping.Mapper.Map<SimpleSource, SimpleDestination>(_simpleSource);
        }
        
        [Benchmark]
        public SimpleDestination AutoMapper_SimpleObject()
        {
            return _autoMapper.Map<SimpleDestination>(_simpleSource);
        }
        
        [Benchmark]
        public SimpleDestination Mapster_SimpleObject()
        {
            return _simpleSource.Adapt<SimpleDestination>();
        }
        
        #endregion
        
        #region Mapeo Complejo (Objetos Anidados)
        
        [Benchmark]
        public PersonDto Manual_ComplexObject()
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
                PhoneNumbers = new List<string>(_complexPerson.PhoneNumbers)
            };
        }
        
        [Benchmark]
        public PersonDto SimpleAutoMapping_ComplexObject()
        {
            return SimpleAutoMapping.Mapper.Map<Person, PersonDto>(_complexPerson);
        }
        
        [Benchmark]
        public PersonDto AutoMapper_ComplexObject()
        {
            return _autoMapper.Map<PersonDto>(_complexPerson);
        }
        
        [Benchmark]
        public PersonDto Mapster_ComplexObject()
        {
            return _complexPerson.Adapt<PersonDto>();
        }
        
        #endregion
        
        #region Mapeo de Colecciones (Simple)
        
        [Benchmark]
        public List<SimpleDestination> Manual_SimpleCollection()
        {
            var result = new List<SimpleDestination>(_simpleList.Count);
            foreach (var item in _simpleList)
            {
                result.Add(new SimpleDestination
                {
                    Id = item.Id,
                    Name = item.Name,
                    CreatedDate = item.CreatedDate,
                    IsActive = item.IsActive
                });
            }
            return result;
        }
        
        [Benchmark]
        public List<SimpleDestination> SimpleAutoMapping_SimpleCollection()
        {
            return SimpleAutoMapping.Mapper.Map<List<SimpleSource>, List<SimpleDestination>>(_simpleList);
        }
        
        [Benchmark]
        public List<SimpleDestination> AutoMapper_SimpleCollection()
        {
            return _autoMapper.Map<List<SimpleDestination>>(_simpleList);
        }
        
        [Benchmark]
        public List<SimpleDestination> Mapster_SimpleCollection()
        {
            return _simpleList.Adapt<List<SimpleDestination>>();
        }
        
        #endregion
        
        #region Mapeo de Colecciones (Complejo)
        
        [Benchmark]
        public List<PersonDto> Manual_ComplexCollection()
        {
            var result = new List<PersonDto>(_complexList.Count);
            foreach (var person in _complexList)
            {
                result.Add(new PersonDto
                {
                    Id = person.Id,
                    Name = person.Name,
                    Address = new AddressDto
                    {
                        Street = person.Address.Street,
                        City = person.Address.City,
                        ZipCode = person.Address.ZipCode
                    },
                    PhoneNumbers = new List<string>(person.PhoneNumbers)
                });
            }
            return result;
        }
        
        [Benchmark]
        public List<PersonDto> SimpleAutoMapping_ComplexCollection()
        {
            return SimpleAutoMapping.Mapper.Map<List<Person>, List<PersonDto>>(_complexList);
        }
        
        [Benchmark]
        public List<PersonDto> AutoMapper_ComplexCollection()
        {
            return _autoMapper.Map<List<PersonDto>>(_complexList);
        }
        
        [Benchmark]
        public List<PersonDto> Mapster_ComplexCollection()
        {
            return _complexList.Adapt<List<PersonDto>>();
        }
        
        #endregion
        
        #region Mapeo Parcial (Partial Update)
        
        [Benchmark]
        public SimpleDestination Manual_PartialUpdate()
        {
            var destination = new SimpleDestination 
            { 
                Id = _existingDestination.Id,
                Name = _existingDestination.Name,
                IsActive = _existingDestination.IsActive,
                CreatedDate = _existingDestination.CreatedDate
            };
            
            // Actualizar solo propiedades no nulas (simulando PATCH)
            destination.Id = _simpleSource.Id;
            destination.IsActive = _simpleSource.IsActive;
            // Se omite Name para simular null o patch
            
            return destination;
        }
        
        [Benchmark]
        public SimpleDestination SimpleAutoMapping_PartialUpdate()
        {
            var destination = new SimpleDestination 
            { 
                Id = _existingDestination.Id,
                Name = _existingDestination.Name,
                IsActive = _existingDestination.IsActive,
                CreatedDate = _existingDestination.CreatedDate
            };
            
            return SimpleAutoMapping.Mapper.PartialMap(_simpleSource, destination);
        }
        
        [Benchmark]
        public SimpleDestination AutoMapper_PartialUpdate()
        {
            var destination = new SimpleDestination 
            { 
                Id = _existingDestination.Id,
                Name = _existingDestination.Name,
                IsActive = _existingDestination.IsActive,
                CreatedDate = _existingDestination.CreatedDate
            };
            
            // AutoMapper no tiene un método PartialMap nativo
            // Se utiliza el mapeo normal
            return _autoMapper.Map(_simpleSource, destination);
        }
        
        [Benchmark]
        public SimpleDestination Mapster_PartialUpdate()
        {
            var destination = new SimpleDestination 
            { 
                Id = _existingDestination.Id,
                Name = _existingDestination.Name,
                IsActive = _existingDestination.IsActive,
                CreatedDate = _existingDestination.CreatedDate
            };
            
            // Mapster no tiene un PartialMap nativo, adaptamos con sus opciones
            return _simpleSource.Adapt(destination);
        }
        
        #endregion
    }
} 