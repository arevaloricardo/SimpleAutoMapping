using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleAutoMapping.Tests.TestModels;
using System;

namespace SimpleAutoMapping.Tests.UnitTests
{
    [TestClass]
    public class DependencyInjectionTests
    {
        [TestMethod]
        public void AddSimpleAutoMapping_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddSimpleAutoMapping(config =>
            {
                config.AddProfile<TestMappingProfile>();
            });
            
            // Build service provider
            var serviceProvider = services.BuildServiceProvider();
            
            // Assert
            var mapper = serviceProvider.GetService<ISimpleAutoMapping>();
            Assert.IsNotNull(mapper);
        }
        
        [TestMethod]
        public void InjectedMapper_ShouldWorkCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            
            services.AddSimpleAutoMapping(config =>
            {
                config.AddProfile<TestMappingProfile>();
            });
            
            var serviceProvider = services.BuildServiceProvider();
            var mapper = serviceProvider.GetRequiredService<ISimpleAutoMapping>();
            
            var source = new SimpleSource { Id = 1, Name = "Test" };
            
            // Act
            var result = mapper.Map<SimpleSource, SimpleDestination>(source);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(source.Id, result.Id);
            Assert.AreEqual(source.Name, result.Name);
        }
    }
} 