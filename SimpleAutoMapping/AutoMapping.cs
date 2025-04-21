using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleAutoMapping
{
    public interface ISimpleAutoMapping
    {
        TDestination Map<TDestination>(object source);
        TDestination Map<TSource, TDestination>(TSource source);
        TDestination MapTo<TDestination>(object source);
        TDestination MapTo<TSource, TDestination>(TSource source);
        TDestination PartialMapTo<TDestination>(object source);
        TDestination PartialMap<TDestination>(object source, TDestination destination);
        TDestination PartialMap<TSource, TDestination>(TSource source, TDestination destination);
        void PartialMapTo<T>(T source, T destination);
        IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source);
    }

    public class MappingConfiguration
    {
        internal Dictionary<TypePair, TypeMapping> TypeMappings { get; } = new();
        internal HashSet<Type> Profiles { get; } = new();
        internal bool ValidateMappings { get; set; }
        internal ConcurrentDictionary<TypePair, Delegate> CompiledExpressions { get; } = new();

        public void AddProfile<TProfile>() where TProfile : MappingProfile, new()
        {
            var profile = new TProfile();
            profile.Configure(this);
            Profiles.Add(typeof(TProfile));
        }

        public void Validate()
        {
            foreach (var mapping in TypeMappings.Values)
            {
                var destinationProperties = mapping.DestinationType.GetProperties();
                foreach (var prop in destinationProperties)
                {
                    if (!prop.CanWrite) continue;

                    if (!mapping.PropertyMappings.Any(pm => pm.DestinationProperty.Name == prop.Name) &&
                        !mapping.IgnoredProperties.Contains(prop.Name))
                    {
                        throw new InvalidOperationException(
                            $"Unmapped property: {mapping.DestinationType.Name}.{prop.Name}");
                    }
                }
            }
        }
    }

    public abstract class MappingProfile
    {
        public void Configure(MappingConfiguration config)
        {
            CreateMappings(config);
            if (config.ValidateMappings)
            {
                config.Validate();
            }
        }

        protected abstract void CreateMappings(MappingConfiguration config);
    }

    public static class Mapper
    {
        private static readonly SimpleAutoMapping Instance = new();

        public static TDest Map<TDest>(object source) => Instance.Map<TDest>(source);
        public static TDest Map<TSource, TDest>(TSource source) => Instance.Map<TSource, TDest>(source);
        public static TDest PartialMap<T, TDest>(T source, TDest destination) => Instance.PartialMap(source, destination);
    }

    public class SimpleAutoMapping : ISimpleAutoMapping
    {
        private readonly MappingConfiguration _configuration;
        private static readonly ConcurrentDictionary<TypePair, Delegate> Cache = new();

        public SimpleAutoMapping(Action<MappingConfiguration> configure = null)
        {
            _configuration = new MappingConfiguration();
            configure?.Invoke(_configuration);
            PrecompileMappings();
        }

        private void PrecompileMappings()
        {
            foreach (var typePair in _configuration.TypeMappings.Keys)
            {
                GetOrCreateMapperForTypes(typePair.Source, typePair.Destination, true, false);
            }
        }

        private Delegate GetOrCreateMapperForTypes(Type sourceType, Type destinationType, bool useProfiles, bool partialMapping)
        {
            var typePair = new TypePair(sourceType, destinationType);
            return Cache.GetOrAdd(typePair, _ =>
            {
                var config = _configuration.TypeMappings.TryGetValue(typePair, out var mappingConfig)
                    ? mappingConfig
                    : CreateDefaultMapping(sourceType, destinationType, useProfiles);

                var method = typeof(SimpleAutoMapping).GetMethod(nameof(CompileMappingFunction), 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var genericMethod = method.MakeGenericMethod(sourceType, destinationType);
                return genericMethod.Invoke(this, new object[] { config, partialMapping }) as Delegate;
            });
        }

        public TDestination Map<TDestination>(object source) => MapTo<TDestination>(source);

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            var typePair = new TypePair(typeof(TSource), typeof(TDestination));
            var mapper = GetOrCreateMapper<TSource, TDestination>(typePair, false);
            return mapper(source, default);
        }

        public TDestination MapTo<TDestination>(object source)
        {
            if (source == null) return default;
            var sourceType = source.GetType();
            var typePair = new TypePair(sourceType, typeof(TDestination));
            var mapper = GetOrCreateMapper<object, TDestination>(typePair, true);
            return mapper(source, default);
        }

        public TDestination MapTo<TSource, TDestination>(TSource source)
        {
            var typePair = new TypePair(typeof(TSource), typeof(TDestination));
            var mapper = GetOrCreateMapper<TSource, TDestination>(typePair, true);
            return mapper(source, default);
        }

        public TDestination PartialMap<TDestination>(object source, TDestination destination)
        {
            if (source == null) return destination;
            var sourceType = source.GetType();
            var typePair = new TypePair(sourceType, typeof(TDestination));
            var mapper = GetOrCreateMapper<object, TDestination>(typePair, true, true);
            return mapper(source, destination);
        }

        public TDestination PartialMap<TSource, TDestination>(TSource source, TDestination destination)
        {
            var typePair = new TypePair(typeof(TSource), typeof(TDestination));
            var mapper = GetOrCreateMapper<TSource, TDestination>(typePair, true, true);
            return mapper(source, destination);
        }

        public TDestination PartialMapTo<TDestination>(object source) => PartialMap(source, default(TDestination));

        public void PartialMapTo<T>(T source, T destination)
        {
            var typePair = new TypePair(typeof(T), typeof(T));
            var mapper = GetOrCreateMapper<T, T>(typePair, true, true);
            mapper(source, destination);
        }

        public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source)
        {
            var sourceType = source.ElementType;
            var typePair = new TypePair(sourceType, typeof(TDestination));
            var expression = GetProjectionExpression(sourceType, typeof(TDestination));
            return source.Provider.CreateQuery<TDestination>(
                Expression.Call(typeof(Queryable), "Select",
                    new[] { sourceType, typeof(TDestination) },
                    source.Expression, expression));
        }

        private LambdaExpression GetProjectionExpression(Type sourceType, Type destinationType)
        {
            var typePair = new TypePair(sourceType, destinationType);
            
            if (!_configuration.CompiledExpressions.TryGetValue(typePair, out var compiledExpression))
            {
                var mappingConfig = _configuration.TypeMappings.TryGetValue(typePair, out var config)
                    ? config
                    : CreateDefaultMapping(sourceType, destinationType, true);
                var lambdaExpression = CompileProjectionExpression(mappingConfig);
                
                // Almacenar la expresión como delegado compilado
                _configuration.CompiledExpressions[typePair] = lambdaExpression.Compile();
                
                return lambdaExpression;
            }
            
            // Si ya existe en el caché, necesitamos obtener la expresión original
            // Para esto podemos crear una expresión equivalente
            var sourceParam = Expression.Parameter(sourceType, "src");
            var call = Expression.Invoke(Expression.Constant(compiledExpression), sourceParam);
            return Expression.Lambda(call, sourceParam);
        }

        private LambdaExpression CompileProjectionExpression(TypeMapping mapping)
        {
            var sourceParam = Expression.Parameter(mapping.SourceType, "src");
            var destinationType = mapping.DestinationType;

            var bindings = new List<MemberBinding>();
            foreach (var propMap in mapping.PropertyMappings)
            {
                var sourceExpr = BuildMemberExpression(sourceParam, propMap);
                bindings.Add(Expression.Bind(propMap.DestinationProperty, sourceExpr));
            }

            var newExpr = Expression.New(destinationType);
            var memberInit = Expression.MemberInit(newExpr, bindings);
            return Expression.Lambda(memberInit, sourceParam);
        }

        private Expression BuildMemberExpression(Expression source, PropertyMapping propMap)
        {
            Expression expr = Expression.Property(source, propMap.SourceProperty);

            if (propMap.ValueResolver != null)
            {
                expr = Expression.Invoke(Expression.Constant(propMap.ValueResolver), source);
            }
            else if (propMap.SourceProperty.PropertyType != propMap.DestinationProperty.PropertyType)
            {
                if (propMap.SourceProperty.PropertyType.IsClass &&
                    propMap.DestinationProperty.PropertyType.IsClass)
                {
                    expr = Expression.Call(typeof(SimpleAutoMapping), "Map",
                        new[] { propMap.SourceProperty.PropertyType, propMap.DestinationProperty.PropertyType },
                        expr);
                }
            }

            if (propMap.ValueTransformer != null)
            {
                expr = Expression.Invoke(Expression.Constant(propMap.ValueTransformer), expr);
            }

            return expr;
        }

        private Func<TSource, TDestination, TDestination> GetOrCreateMapper<TSource, TDestination>(
            TypePair typePair, bool useProfiles, bool partialMapping = false)
        {
            return (Func<TSource, TDestination, TDestination>)Cache.GetOrAdd(typePair, _ =>
            {
                var config = _configuration.TypeMappings.TryGetValue(typePair, out var mappingConfig)
                    ? mappingConfig
                    : CreateDefaultMapping<TSource, TDestination>(useProfiles, partialMapping);
                return CompileMappingFunction<TSource, TDestination>(config, partialMapping);
            });
        }

        private TypeMapping CreateDefaultMapping<TSource, TDestination>(bool useProfiles, bool partialMapping)
        {
            var config = new TypeMapping(typeof(TSource), typeof(TDestination));
            if (useProfiles && _configuration.Profiles.Count > 0)
            {
                ApplyProfileMappings(config);
            }
            else
            {
                CreateConventionMappings<TSource, TDestination>(config);
            }
            return config;
        }

        private TypeMapping CreateDefaultMapping(Type sourceType, Type destinationType, bool useProfiles)
        {
            var config = new TypeMapping(sourceType, destinationType);
            if (useProfiles && _configuration.Profiles.Count > 0)
            {
                ApplyProfileMappings(config);
            }
            else
            {
                CreateConventionMappings(sourceType, destinationType, config);
            }
            return config;
        }

        private void ApplyProfileMappings(TypeMapping config)
        {
            // Logic to apply profile configurations can be expanded here
        }

        private void CreateConventionMappings<TSource, TDestination>(TypeMapping config)
        {
            var sourceProperties = typeof(TSource).GetProperties();
            var destProperties = typeof(TDestination).GetProperties();

            foreach (var destProp in destProperties)
            {
                var sourceProp = sourceProperties.FirstOrDefault(p => p.Name == destProp.Name);
                if (sourceProp != null && sourceProp.PropertyType == destProp.PropertyType)
                {
                    config.PropertyMappings.Add(new PropertyMapping
                    {
                        SourceProperty = sourceProp,
                        DestinationProperty = destProp
                    });
                }
            }
        }

        private void CreateConventionMappings(Type sourceType, Type destinationType, TypeMapping config)
        {
            var sourceProperties = sourceType.GetProperties();
            var destProperties = destinationType.GetProperties();

            foreach (var destProp in destProperties)
            {
                var sourceProp = sourceProperties.FirstOrDefault(p => p.Name == destProp.Name);
                if (sourceProp != null && sourceProp.PropertyType == destProp.PropertyType)
                {
                    config.PropertyMappings.Add(new PropertyMapping
                    {
                        SourceProperty = sourceProp,
                        DestinationProperty = destProp
                    });
                }
            }
        }

        private Func<TSource, TDestination, TDestination> CompileMappingFunction<TSource, TDestination>(
            TypeMapping config, bool partialMapping)
        {
            var sourceParam = Expression.Parameter(typeof(TSource), "src");
            var destParam = Expression.Parameter(typeof(TDestination), "dest");
            var variable = Expression.Variable(typeof(TDestination), "result");

            var expressions = new List<Expression>
            {
                Expression.Assign(variable, Expression.New(typeof(TDestination)))
            };

            foreach (var propMap in config.PropertyMappings)
            {
                var sourceProp = Expression.Property(sourceParam, propMap.SourceProperty);
                var destProp = Expression.Property(variable, propMap.DestinationProperty);
                Expression assignment = Expression.Assign(destProp, sourceProp);

                if (partialMapping)
                {
                    var nullCheck = Expression.NotEqual(sourceProp, Expression.Default(sourceProp.Type));
                    assignment = Expression.IfThen(nullCheck, assignment);
                }

                expressions.Add(assignment);
            }

            expressions.Add(variable);

            var block = Expression.Block(new[] { variable }, expressions);
            return Expression.Lambda<Func<TSource, TDestination, TDestination>>(
                block, sourceParam, destParam).Compile();
        }

        public static IEnumerable<TDest> MapCollection<TSrc, TDest>(IEnumerable<TSrc> source)
        {
            if (source == null) yield break;

            var mapper = Cache.GetOrAdd(new TypePair(typeof(TSrc), typeof(TDest)), _ =>
            {
                var mapping = new TypeMapping(typeof(TSrc), typeof(TDest));
                var instance = new SimpleAutoMapping();
                return instance.CompileMappingFunction<TSrc, TDest>(mapping, false);
            });

            foreach (var item in source)
            {
                yield return ((Func<TSrc, TDest, TDest>)mapper)(item, default);
            }
        }

        public static IEnumerable<TDest> ParallelMap<TSrc, TDest>(IEnumerable<TSrc> source, int? degreeOfParallelism = null)
        {
            var mapper = (Func<TSrc, TDest, TDest>)Cache.GetOrAdd(new TypePair(typeof(TSrc), typeof(TDest)), _ =>
            {
                var mapping = new TypeMapping(typeof(TSrc), typeof(TDest));
                var instance = new SimpleAutoMapping();
                return instance.CompileMappingFunction<TSrc, TDest>(mapping, false);
            });

            var result = new TDest[source.Count()];
            Parallel.ForEach(source, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism ?? Environment.ProcessorCount },
                (item, _, index) =>
                {
                    result[index] = mapper(item, default);
                });

            return result;
        }
    }

    public static class ServiceExtensions
    {
        public static IServiceCollection AddSimpleAutoMapping(this IServiceCollection services,
            Action<MappingConfiguration> configure)
        {
            services.AddSingleton<ISimpleAutoMapping>(_ => new SimpleAutoMapping(configure));
            return services;
        }
    }

    public static class MappingExtensions
    {
        public static TDestination MapTo<TDestination>(this object source) =>
            Mapper.Map<TDestination>(source);

        public static TDestination Map<TDestination>(this object source) =>
            Mapper.Map<TDestination>(source);

        public static IMappingConfigurator<TSource, TDestination> CreateMap<TSource, TDestination>(
            this MappingConfiguration config)
        {
            var mapping = new TypeMapping(typeof(TSource), typeof(TDestination));
            config.TypeMappings[new TypePair(typeof(TSource), typeof(TDestination))] = mapping;
            return new MappingConfigurator<TSource, TDestination>(mapping);
        }

        public static IMappingConfigurator<TSource, TDestination> CustomProperty<TSource, TDestination>(
            this IMappingConfigurator<TSource, TDestination> configurator,
            Expression<Func<TSource, object>> sourceMember,
            Expression<Func<TDestination, object>> destinationMember)
        {
            var sourceProp = ReflectionHelper.GetPropertyInfo(sourceMember);
            var destProp = ReflectionHelper.GetPropertyInfo(destinationMember);
            configurator.Mapping.PropertyMappings.Add(new PropertyMapping
            {
                SourceProperty = sourceProp,
                DestinationProperty = destProp
            });
            return configurator;
        }

        public static IMappingConfigurator<TSource, TDestination> AddResolver<TSource, TDestination, TMember>(
            this IMappingConfigurator<TSource, TDestination> configurator,
            Expression<Func<TDestination, TMember>> destinationMember,
            Func<TSource, TMember> resolver)
        {
            var destProp = ReflectionHelper.GetPropertyInfo(destinationMember);
            configurator.Mapping.PropertyMappings.Add(new PropertyMapping
            {
                DestinationProperty = destProp,
                ValueResolver = src => resolver((TSource)src)
            });
            return configurator;
        }
    }

    public interface IMappingConfigurator<TSource, TDestination>
    {
        TypeMapping Mapping { get; }
    }

    public class MappingConfigurator<TSource, TDestination> : IMappingConfigurator<TSource, TDestination>
    {
        public TypeMapping Mapping { get; }

        public MappingConfigurator(TypeMapping mapping)
        {
            Mapping = mapping;
        }

        public MappingConfigurator<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<MemberConfiguration<TSource, TDestination, TMember>> options)
        {
            var config = new MemberConfiguration<TSource, TDestination, TMember>(Mapping);
            options(config);
            return this;
        }
    }

    public class MemberConfiguration<TSource, TDestination, TMember>
    {
        private readonly TypeMapping _mapping;

        public MemberConfiguration(TypeMapping mapping)
        {
            _mapping = mapping;
        }

        public void MapFrom(Expression<Func<TSource, TMember>> sourceMember)
        {
            var sourceProp = ReflectionHelper.GetPropertyInfo(sourceMember);
            var destProp = ReflectionHelper.GetPropertyInfo<TDestination, TMember>(_mapping.DestinationType);

            _mapping.PropertyMappings.Add(new PropertyMapping
            {
                SourceProperty = sourceProp,
                DestinationProperty = destProp
            });
        }

        public void ResolveUsing(Func<TSource, TMember> resolver)
        {
            var destProp = ReflectionHelper.GetPropertyInfo<TDestination, TMember>(_mapping.DestinationType);

            _mapping.PropertyMappings.Add(new PropertyMapping
            {
                DestinationProperty = destProp,
                ValueResolver = src => resolver((TSource)src)
            });
        }
    }

    public class TypePair : IEquatable<TypePair>
    {
        public Type Source { get; }
        public Type Destination { get; }

        public TypePair(Type source, Type destination)
        {
            Source = source;
            Destination = destination;
        }

        public bool Equals(TypePair other) =>
            Source == other?.Source && Destination == other.Destination;

        public override int GetHashCode() =>
            HashCode.Combine(Source, Destination);
    }

    public class TypeMapping
    {
        public Type SourceType { get; }
        public Type DestinationType { get; }
        public List<PropertyMapping> PropertyMappings { get; } = new();
        public HashSet<string> IgnoredProperties { get; } = new();
        public List<Condition> Conditions { get; } = new();

        public TypeMapping(Type source, Type dest)
        {
            SourceType = source;
            DestinationType = dest;
        }
    }

    public class PropertyMapping
    {
        public PropertyInfo SourceProperty { get; set; }
        public PropertyInfo DestinationProperty { get; set; }
        public Func<object, object> ValueResolver { get; set; }
        public Func<object, object> ValueTransformer { get; set; }
        public Func<object, bool> Condition { get; set; }
    }

    public class Condition
    {
        public Func<object, bool> Predicate { get; set; }
        public PropertyMapping Mapping { get; set; }
    }

    internal static class ReflectionHelper
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache = new();

        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(
            Expression<Func<TSource, TProperty>> expression)
        {
            if (expression.Body is MemberExpression member)
            {
                return (PropertyInfo)member.Member;
            }
            throw new ArgumentException("Expression is not a property access", nameof(expression));
        }

        public static PropertyInfo GetPropertyInfo<T, TProperty>(Type type)
        {
            var properties = PropertyCache.GetOrAdd(type, t => t.GetProperties().ToDictionary(p => p.Name));
            return properties.Values.FirstOrDefault(p => p.PropertyType == typeof(TProperty));
        }

        public static Func<object, object> CreateGetter(PropertyInfo property)
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var access = Expression.Property(Expression.Convert(objParam, property.DeclaringType), property);
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(access, typeof(object)), objParam).Compile();
        }

        public static Action<object, object> CreateSetter(PropertyInfo property)
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var setter = Expression.Call(
                Expression.Convert(objParam, property.DeclaringType),
                property.GetSetMethod(),
                Expression.Convert(valueParam, property.PropertyType));
            return Expression.Lambda<Action<object, object>>(setter, objParam, valueParam).Compile();
        }
    }
}