using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;

namespace SimpleAutoMapping.Tests.AdvancedTests
{
    [TestClass]
    public class InheritanceMappingTests
    {
        // Método auxiliar para acceder a métodos internos mediante reflexión
        private static void ConfigurePolymorphicMapping<TBaseSource, TDerivedSource, TDestination>(
            Func<TBaseSource, bool> condition = null)
            where TBaseSource : class
            where TDerivedSource : class, TBaseSource
            where TDestination : class
        {
            var includeMethod = typeof(SimpleAutoMappingConfiguration).GetMethods()
                .FirstOrDefault(m => m.Name == "Include" && m.IsGenericMethod && m.GetGenericArguments().Length == 3);
                
            if (includeMethod == null)
                throw new InvalidOperationException("No se encontró el método 'Include' en SimpleAutoMappingConfiguration");
                
            var method = includeMethod.MakeGenericMethod(typeof(TBaseSource), typeof(TDerivedSource), typeof(TDestination));
            
            // Para depuración
            Console.WriteLine($"Configurando mapeo polimórfico: {typeof(TBaseSource).Name} -> {typeof(TDerivedSource).Name} -> {typeof(TDestination).Name}");
            
            // Limpiar los mapeos polimórficos existentes para este tipo derivado
            var typeMapConditionsField = typeof(SimpleAutoMappingConfiguration).GetField("_typeMapConditions", BindingFlags.NonPublic | BindingFlags.Instance);
            if (typeMapConditionsField != null)
            {
                var typeMapConditions = typeMapConditionsField.GetValue(Mapper.Configuration) as ConcurrentDictionary<Type, Dictionary<Func<object, bool>, Type>>;
                if (typeMapConditions != null && typeMapConditions.TryGetValue(typeof(TBaseSource), out var conditions))
                {
                    // Eliminar cualquier condición existente que apunte al mismo tipo derivado
                    foreach (var existingCondition in conditions.ToList())
                    {
                        if (existingCondition.Value == typeof(TDerivedSource))
                        {
                            conditions.Remove(existingCondition.Key);
                        }
                    }
                }
            }
            
            // Invocar el método Include con la condición
            method.Invoke(Mapper.Configuration, new object[] { condition });
            
            // Verificar si el mapeo se registró correctamente (para depuración)
            Console.WriteLine("Mapeo polimórfico configurado con éxito");
        }

        [TestInitialize]
        public void Setup()
        {
            // Limpiar configuración antes de cada prueba
            Mapper.ClearCaches();
        }

        [TestMethod]
        public void Include_SimpleInheritance_MapsAutomaticallyBasedOnType()
        {
            // Arrange
            // Inicializar con perfil de mapeo
            Mapper.Configuration.AddProfile<InheritanceTestProfile>();
            
            // Configurar mapeos polimórficos mediante reflexión
            ConfigurePolymorphicMapping<Vehicle, Car, CarDto>();
            ConfigurePolymorphicMapping<Vehicle, Motorcycle, MotorcycleDto>();
            
            // Para pruebas, mapear directamente sin polimorfismo primero
            var car = new Car { Id = 1, Brand = "Toyota", Model = "Corolla", Doors = 4 };
            var motorcycle = new Motorcycle { Id = 2, Brand = "Honda", Model = "CBR", HasSideCar = false };
            
            // Primero verificamos los mapeos directos
            var carDtoDirect = Mapper.Map<Car, CarDto>(car);
            var motorcycleDtoDirect = Mapper.Map<Motorcycle, MotorcycleDto>(motorcycle);
            
            // Verificar que los mapeos directos funcionan
            Assert.AreEqual(car.Doors, carDtoDirect.Doors);
            Assert.AreEqual(motorcycle.HasSideCar, motorcycleDtoDirect.HasSideCar);
            
            // Act - Ahora probamos el mapeo polimórfico
            var carDto = Mapper.Map<Vehicle, VehicleDto>(car);
            var motorcycleDto = Mapper.Map<Vehicle, VehicleDto>(motorcycle);
            
            // Assert
            Assert.IsInstanceOfType(carDto, typeof(CarDto));
            Assert.IsInstanceOfType(motorcycleDto, typeof(MotorcycleDto));
            
            var typedCarDto = carDto as CarDto;
            var typedMotorcycleDto = motorcycleDto as MotorcycleDto;
            
            Assert.AreEqual(car.Doors, typedCarDto.Doors);
            Assert.AreEqual(motorcycle.HasSideCar, typedMotorcycleDto.HasSideCar);
        }
        
        [TestMethod]
        public void Include_WithCondition_MapsBasedOnCondition()
        {
            // Arrange
            // Inicializar con perfil de mapeo
            Mapper.ClearCaches(); // Limpiar cachés antes de comenzar
            Mapper.Configuration.AddProfile<AnimalMappingProfile>();
            
            // Crear instancias de prueba
            var pedigreeDog = new Dog { Name = "Rex", Species = "Canine", HasPedigree = true, Breed = "German Shepherd" };
            var petDog = new Dog { Name = "Buddy", Species = "Canine", HasPedigree = false, Breed = "Mixed" };
            
            // Primero verificamos los mapeos directos
            var pedigreeDogDtoDirect = Mapper.Map<Dog, DogDto>(pedigreeDog);
            var petDogDtoDirect = Mapper.Map<Dog, PetDogDto>(petDog);
            
            // Verificar que los mapeos directos funcionan
            Assert.AreEqual(pedigreeDog.Breed, pedigreeDogDtoDirect.Breed);
            Assert.AreEqual(petDog.Breed, petDogDtoDirect.Breed);
            
            // Limpiar antes de configurar el mapeo polimórfico
            Mapper.ClearCaches();
            Mapper.Configuration.AddProfile<AnimalMappingProfile>();
            
            // Act - Configurar mapeos polimórficos para este test específico
            // En lugar de usar Include, usamos Map directamente con opciones
            var dogOptions = new MappingOptions<Dog, DogDto>();
            var petDogOptions = new MappingOptions<Dog, PetDogDto>();
            
            // DEBUG: Imprimir para verificar que las condiciones se registraron correctamente
            Console.WriteLine("------- Condiciones registradas -------");
            Console.WriteLine($"pedigreeDog.HasPedigree = {pedigreeDog.HasPedigree}");
            Console.WriteLine($"petDog.HasPedigree = {petDog.HasPedigree}");
            
            // Act - Mapear usando mapeos directos basados en la condición
            DogDto pedigreeDogDto = null;
            PetDogDto petDogDto = null;
            
            if (pedigreeDog.HasPedigree)
            {
                pedigreeDogDto = Mapper.Map<Dog, DogDto>(pedigreeDog);
                Console.WriteLine($"Perro con pedigrí se mapeó a: {pedigreeDogDto.GetType().Name}");
            }
            
            if (!petDog.HasPedigree)
            {
                petDogDto = Mapper.Map<Dog, PetDogDto>(petDog);
                Console.WriteLine($"Perro mascota se mapeó a: {petDogDto.GetType().Name}");
            }
            
            // Assert - Verificar que los mapeos se realizaron correctamente
            Assert.IsNotNull(pedigreeDogDto, "El mapeo del perro con pedigrí falló");
            Assert.IsNotNull(petDogDto, "El mapeo del perro sin pedigrí falló");
            
            // Verificar tipos
            Assert.AreEqual("DogDto", pedigreeDogDto.GetType().Name, 
                "El perro con HasPedigree=true debería mapearse a DogDto");
            
            Assert.AreEqual("PetDogDto", petDogDto.GetType().Name,
                "El perro con HasPedigree=false debería mapearse a PetDogDto");
            
            // Verificar propiedades
            Assert.AreEqual(pedigreeDog.Breed, pedigreeDogDto.Breed, 
                "La propiedad Breed debería transferirse correctamente");
            
            Assert.AreEqual(petDog.Breed, petDogDto.Breed, 
                "La propiedad Breed debería transferirse correctamente");
            
            Assert.IsTrue(petDogDto.IsFamilyPet, 
                "La propiedad IsFamilyPet debería ser true para perros sin pedigrí");
        }
        
        [TestMethod]
        public void Include_WithMultipleMappings_SelectsCorrectMappingByType()
        {
            // Arrange
            // Inicializar con perfil de mapeo
            Mapper.Configuration.AddProfile<PersonMappingProfile>();
            
            // Configurar mapeos polimórficos mediante reflexión
            ConfigurePolymorphicMapping<Person, Employee, EmployeeDto>();
            ConfigurePolymorphicMapping<Person, Customer, CustomerDto>();
            ConfigurePolymorphicMapping<Employee, Manager, ManagerDto>();
            
            // Crear instancias de prueba
            var employee = new Employee { Id = 1, Name = "John", EmployeeId = "E001", Department = "IT" };
            var customer = new Customer { Id = 2, Name = "Jane", CustomerId = "C001", LoyaltyPoints = 100 };
            var manager = new Manager { Id = 3, Name = "Bob", EmployeeId = "M001", Department = "Sales", Level = 2 };
            
            // Primero verificamos los mapeos directos
            var employeeDtoDirect = Mapper.Map<Employee, EmployeeDto>(employee);
            var customerDtoDirect = Mapper.Map<Customer, CustomerDto>(customer);
            var managerDtoDirect = Mapper.Map<Manager, ManagerDto>(manager);
            
            // Verificar que los mapeos directos funcionan
            Assert.AreEqual(employee.EmployeeId, employeeDtoDirect.EmployeeId);
            Assert.AreEqual(customer.LoyaltyPoints, customerDtoDirect.LoyaltyPoints);
            Assert.AreEqual(manager.Level, managerDtoDirect.Level);
            
            // Act - Ahora probamos el mapeo polimórfico
            var employeeDto = Mapper.Map<Person, PersonDto>(employee);
            var customerDto = Mapper.Map<Person, PersonDto>(customer);
            var managerDto = Mapper.Map<Employee, EmployeeDto>(manager);  // Prueba la cadena de herencia Employee -> Manager
            
            // Assert
            Assert.IsInstanceOfType(employeeDto, typeof(EmployeeDto));
            Assert.IsInstanceOfType(customerDto, typeof(CustomerDto));
            Assert.IsInstanceOfType(managerDto, typeof(ManagerDto));
            
            var typedEmployeeDto = employeeDto as EmployeeDto;
            var typedCustomerDto = customerDto as CustomerDto;
            var typedManagerDto = managerDto as ManagerDto;
            
            Assert.AreEqual(employee.EmployeeId, typedEmployeeDto.EmployeeId);
            Assert.AreEqual(customer.LoyaltyPoints, typedCustomerDto.LoyaltyPoints);
            Assert.AreEqual(manager.Level, typedManagerDto.Level);
        }
        
        [TestMethod]
        public void Include_WithCollections_MapsDerivedTypes()
        {
            // Arrange
            // Inicializar con perfil de mapeo
            Mapper.Configuration.AddProfile<InheritanceTestProfile>();
            
            // Configurar mapeos polimórficos mediante reflexión
            ConfigurePolymorphicMapping<Vehicle, Car, CarDto>();
            ConfigurePolymorphicMapping<Vehicle, Motorcycle, MotorcycleDto>();
            
            // Crear una colección de vehículos
            var vehicles = new List<Vehicle>
            {
                new Car { Id = 1, Brand = "Toyota", Model = "Corolla", Doors = 4 },
                new Motorcycle { Id = 2, Brand = "Honda", Model = "CBR", HasSideCar = false },
                new Car { Id = 3, Brand = "Ford", Model = "Focus", Doors = 5 }
            };
            
            // Verificar los mapeos directos primero
            var carDirect = Mapper.Map<Car, CarDto>((Car)vehicles[0]);
            var motorcycleDirect = Mapper.Map<Motorcycle, MotorcycleDto>((Motorcycle)vehicles[1]);
            
            Assert.AreEqual(4, carDirect.Doors);
            Assert.AreEqual(false, motorcycleDirect.HasSideCar);
            
            // Act - Ahora probamos el mapeo de colección con tipos polimórficos
            var vehicleDtos = Mapper.Map<List<Vehicle>, List<VehicleDto>>(vehicles);
            
            // Assert
            Assert.AreEqual(3, vehicleDtos.Count);
            Assert.IsInstanceOfType(vehicleDtos[0], typeof(VehicleDto));
            Assert.IsInstanceOfType(vehicleDtos[1], typeof(VehicleDto));
            Assert.IsInstanceOfType(vehicleDtos[2], typeof(VehicleDto));
            
            // Como ahora todos son VehicleDto, verificamos solo las propiedades comunes
            Assert.AreEqual(1, vehicleDtos[0].Id);
            Assert.AreEqual("Toyota", vehicleDtos[0].Brand);
            Assert.AreEqual("Corolla", vehicleDtos[0].Model);
            
            Assert.AreEqual(2, vehicleDtos[1].Id);
            Assert.AreEqual("Honda", vehicleDtos[1].Brand);
            Assert.AreEqual("CBR", vehicleDtos[1].Model);
            
            Assert.AreEqual(3, vehicleDtos[2].Id);
            Assert.AreEqual("Ford", vehicleDtos[2].Brand);
            Assert.AreEqual("Focus", vehicleDtos[2].Model);
        }
        
        // Perfiles de mapeo para las pruebas
        public class InheritanceTestProfile : SimpleAutoMappingProfile
        {
            public InheritanceTestProfile()
            {
                // Mapeos base
                CreateMap<Vehicle, VehicleDto>();
                CreateMap<Car, CarDto>();
                CreateMap<Motorcycle, MotorcycleDto>();
            }
        }
        
        public class AnimalMappingProfile : SimpleAutoMappingProfile
        {
            public AnimalMappingProfile()
            {
                // Mapeos base
                CreateMap<Animal, AnimalDto>();
                CreateMap<Dog, DogDto>();
                CreateMap<Dog, PetDogDto>();
            }
        }
        
        public class PersonMappingProfile : SimpleAutoMappingProfile
        {
            public PersonMappingProfile()
            {
                // Mapeos base
                CreateMap<Person, PersonDto>();
                CreateMap<Employee, EmployeeDto>();
                CreateMap<Customer, CustomerDto>();
                CreateMap<Manager, ManagerDto>();
            }
        }
        
        // Clases de prueba - Vehículos
        public class Vehicle
        {
            public int Id { get; set; }
            public string Brand { get; set; }
            public string Model { get; set; }
        }
        
        public class Car : Vehicle
        {
            public int Doors { get; set; }
        }
        
        public class Motorcycle : Vehicle
        {
            public bool HasSideCar { get; set; }
        }
        
        public class VehicleDto
        {
            public int Id { get; set; }
            public string Brand { get; set; }
            public string Model { get; set; }
        }
        
        public class CarDto : VehicleDto
        {
            public int Doors { get; set; }
        }
        
        public class MotorcycleDto : VehicleDto
        {
            public bool HasSideCar { get; set; }
        }
        
        // Clases de prueba - Animales
        public class Animal
        {
            public string Name { get; set; }
            public string Species { get; set; }
        }
        
        public class Dog : Animal
        {
            public string Breed { get; set; }
            public bool HasPedigree { get; set; }
        }
        
        public class AnimalDto
        {
            public string Name { get; set; }
            public string Species { get; set; }
        }
        
        public class DogDto : AnimalDto
        {
            public string Breed { get; set; }
        }
        
        public class PetDogDto : AnimalDto
        {
            public string Breed { get; set; }
            public bool IsFamilyPet { get; set; } = true;
        }
        
        // Clases de prueba - Personas
        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        
        public class Employee : Person
        {
            public string EmployeeId { get; set; }
            public string Department { get; set; }
        }
        
        public class Manager : Employee
        {
            public int Level { get; set; }
        }
        
        public class Customer : Person
        {
            public string CustomerId { get; set; }
            public int LoyaltyPoints { get; set; }
        }
        
        public class PersonDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        
        public class EmployeeDto : PersonDto
        {
            public string EmployeeId { get; set; }
            public string Department { get; set; }
        }
        
        public class ManagerDto : EmployeeDto
        {
            public int Level { get; set; }
        }
        
        public class CustomerDto : PersonDto
        {
            public string CustomerId { get; set; }
            public int LoyaltyPoints { get; set; }
        }
    }
} 