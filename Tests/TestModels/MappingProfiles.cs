namespace SimpleAutoMapping.Tests.TestModels
{
    public class TestMappingProfile : SimpleAutoMappingProfile
    {
        public TestMappingProfile()
        {
            // Mapeo básico
            CreateMap<SimpleSource, SimpleDestination>();
            
            // Mapeo con nombres diferentes
            CreateMap<SimpleSource, SimpleDestinationWithDifferentNames>()
                .ConfigProperty(src => src.Id, dest => dest.Identifier)
                .ConfigProperty(src => src.Name, dest => dest.FullName)
                .ConfigProperty(src => src.CreatedDate, dest => dest.Created)
                .ConfigProperty(src => src.IsActive, dest => dest.Status);
                
            // Mapeos complejos
            CreateMap<Address, AddressDto>();
            CreateMap<Person, PersonDto>();
            
            // Mapeos de colecciones
            CreateMap<Category, CategoryDto>();
            CreateMap<Product, ProductDto>();
            CreateMap<Order, OrderDto>();
        }
    }
} 