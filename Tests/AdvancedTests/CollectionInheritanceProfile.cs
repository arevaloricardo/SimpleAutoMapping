using SimpleAutoMapping;

namespace SimpleAutoMapping.Tests.AdvancedTests
{
    public class CollectionInheritanceProfile : SimpleAutoMappingProfile
    {
        public CollectionInheritanceProfile()
        {
            CreateMap<BaseItem, BaseItemDto>();
            CreateMap<DerivedItem, DerivedItemDto>();
            CreateMap<ContainerWithCollection, ContainerWithCollectionDto>();
            
            // Configuración polimórfica explícita
            Mapper.Configuration.Include<BaseItem, DerivedItem, DerivedItemDto>(item => item is DerivedItem);
        }
    }
} 