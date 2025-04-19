using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SimpleAutoMapping
{
    /// <summary>
    /// Interfaz para inyección del mapper
    /// </summary>
    public interface ISimpleAutoMapping
    {
        // Mapeo con tipos explícitos
        TDestination Map<TSource, TDestination>(TSource source, TDestination? destination = default)
            where TSource : class
            where TDestination : class;
        
        TDestination PartialMap<TSource, TDestination>(TSource source, TDestination destination)
            where TSource : class
            where TDestination : class;
        
        // Mapeo con inferencia de tipos
        TDestination? Map<TDestination>(object source)
            where TDestination : class;
        
        object? Map(object source);
        
        // Mapeo parcial con inferencia
        TDestination PartialMap<TDestination>(object source, TDestination destination)
            where TDestination : class;
            
        // Métodos simplificados de mapeo con inferencia de tipo origen
        TDestination MapTo<TDestination>(object source)
            where TDestination : class;
            
        TDestination MapTo<TDestination>(object source, TDestination destination)
            where TDestination : class;
            
        // Método para mapeo parcial a un nuevo objeto
        TDestination PartialMapTo<TDestination>(object source)
            where TDestination : class;
            
        // Método para mapeo parcial a un objeto existente
        TDestination PartialMapTo<TDestination>(object source, TDestination destination)
            where TDestination : class;
    }
    
    /// <summary>
    /// Opciones de configuración para mapeo
    /// </summary>
    public class MappingOptions<TSource, TDestination>
        where TSource : class
        where TDestination : class
    {
        internal bool IgnoreNullValues { get; set; } = false;
        internal bool MapNestedObjects { get; set; } = true;
        internal bool PreserveNestedNullValues { get; set; } = false;
        internal Dictionary<string, string> PropertyMappings { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, Func<object, object>> ValueTransformers { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal List<string> IgnoreProperties { get; } = new();
        internal Dictionary<string, Func<object, object>> CustomResolvers { get; } = new(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Configura el mapeo para ignorar valores nulos (comportamiento PATCH)
        /// </summary>
        public MappingOptions<TSource, TDestination> IgnoreNulls()
        {
            IgnoreNullValues = true;
            return this;
        }
        
        /// <summary>
        /// Configura el mapeo inverso (de TDestination a TSource) automáticamente
        /// </summary>
        /// <returns>Opciones de configuración para el mapeo inverso</returns>
        public MappingOptions<TDestination, TSource> ReverseMap()
        {
            // Crear las opciones para el mapeo inverso
            var reverseOptions = new MappingOptions<TDestination, TSource>();
            
            // Invertir las asignaciones de propiedades
            foreach (var mapping in PropertyMappings)
            {
                reverseOptions.PropertyMappings[mapping.Value] = mapping.Key;
            }
            
            // Registrar en la configuración global
            Mapper.Configuration.RegisterMapping(typeof(TDestination), typeof(TSource), reverseOptions);
            
            return reverseOptions;
        }
        
        /// <summary>
        /// Configura el mapeo para propagar valores nulos (comportamiento PUT)
        /// </summary>
        public MappingOptions<TSource, TDestination> PropagateNulls()
        {
            IgnoreNullValues = false;
            return this;
        }
        
        /// <summary>
        /// Configura mapeo entre propiedades con nombres diferentes
        /// </summary>
        public MappingOptions<TSource, TDestination> ConfigProperty(
            Expression<Func<TSource, object>> sourceProp, 
            Expression<Func<TDestination, object>> destProp)
        {
            PropertyMappings[GetPropertyName(sourceProp)] = GetPropertyName(destProp);
            return this;
        }
        
        /// <summary>
        /// Configura mapeo entre propiedades con nombres diferentes
        /// </summary>
        public MappingOptions<TSource, TDestination> ConfigProperty(string sourceProp, string destProp)
        {
            if (string.IsNullOrEmpty(sourceProp))
                throw new ArgumentException("El nombre de la propiedad origen no puede estar vacío", nameof(sourceProp));
            if (string.IsNullOrEmpty(destProp))
                throw new ArgumentException("El nombre de la propiedad destino no puede estar vacío", nameof(destProp));
                
            PropertyMappings[sourceProp] = destProp;
            return this;
        }
        
        /// <summary>
        /// Ignora una propiedad en el mapeo
        /// </summary>
        public MappingOptions<TSource, TDestination> IgnoreProperty(Expression<Func<TSource, object>> property)
        {
            IgnoreProperties.Add(GetPropertyName(property));
            return this;
        }
        
        /// <summary>
        /// Ignora una propiedad usando su nombre
        /// </summary>
        public MappingOptions<TSource, TDestination> IgnoreProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("El nombre de la propiedad no puede estar vacío", nameof(propertyName));
                
            IgnoreProperties.Add(propertyName);
            return this;
        }
        
        /// <summary>
        /// Transforma el valor de una propiedad durante el mapeo
        /// </summary>
        public MappingOptions<TSource, TDestination> ConfigTransform<TValue, TResult>(
            Expression<Func<TSource, TValue>> property,
            Func<TValue, TResult> transformer)
        {
            if (transformer == null)
                throw new ArgumentNullException(nameof(transformer), "La función transformadora no puede ser null");
                
            var propName = GetPropertyName(property);
            ValueTransformers[propName] = value => value == null ? default : transformer((TValue)value);
            return this;
        }
        
        /// <summary>
        /// Transforma el valor de una propiedad durante el mapeo usando su nombre
        /// </summary>
        public MappingOptions<TSource, TDestination> ConfigTransform<TValue, TResult>(
            string propertyName,
            Func<TValue, TResult> transformer)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("El nombre de la propiedad no puede estar vacío", nameof(propertyName));
            if (transformer == null)
                throw new ArgumentNullException(nameof(transformer), "La función transformadora no puede ser null");
                
            ValueTransformers[propertyName] = value => value == null ? default : transformer((TValue)value);
            return this;
        }
        
        /// <summary>
        /// Configura un resolutor personalizado para una propiedad del destino
        /// </summary>
        public MappingOptions<TSource, TDestination> AddResolver(
            Expression<Func<TDestination, object>> destProp,
            Func<TSource, object> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver), "El resolutor no puede ser null");
                
            var destPropName = GetPropertyName(destProp);
            CustomResolvers[destPropName] = source => resolver((TSource)source);
            return this;
        }
        
        /// <summary>
        /// Configura un resolutor personalizado para una propiedad del destino usando su nombre
        /// </summary>
        public MappingOptions<TSource, TDestination> AddResolver(
            string destPropName,
            Func<TSource, object> resolver)
        {
            if (string.IsNullOrEmpty(destPropName))
                throw new ArgumentException("El nombre de la propiedad destino no puede estar vacío", nameof(destPropName));
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver), "El resolutor no puede ser null");
                
            CustomResolvers[destPropName] = source => resolver((TSource)source);
            return this;
        }
        
        /// <summary>
        /// Extrae el nombre de propiedad de una expresión
        /// </summary>
        private string GetPropertyName<T, TProp>(Expression<Func<T, TProp>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression), "La expresión no puede ser null");
                
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            
            if (expression.Body is UnaryExpression unaryExpression && 
                unaryExpression.Operand is MemberExpression operandExpr)
            {
                return operandExpr.Member.Name;
            }
            
            throw new ArgumentException("La expresión debe acceder a una propiedad", nameof(expression));
        }
        
        /// <summary>
        /// Valida que la configuración sea correcta
        /// </summary>
        internal void Validate()
        {
            // Validar que las propiedades mapeadas existan en ambos tipos
            foreach (var mapping in PropertyMappings)
            {
                var sourceProps = typeof(TSource).GetProperties().Select(p => p.Name).ToList();
                if (!sourceProps.Contains(mapping.Key, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"La propiedad de origen '{mapping.Key}' no existe en el tipo {typeof(TSource).Name}");
                }
                
                var destProps = typeof(TDestination).GetProperties().Select(p => p.Name).ToList();
                if (!destProps.Contains(mapping.Value, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"La propiedad de destino '{mapping.Value}' no existe en el tipo {typeof(TDestination).Name}");
                }
            }
            
            // Validar transformadores
            foreach (var transformer in ValueTransformers)
            {
                var sourceProps = typeof(TSource).GetProperties().Select(p => p.Name).ToList();
                if (!sourceProps.Contains(transformer.Key, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"La propiedad '{transformer.Key}' para transformación no existe en el tipo {typeof(TSource).Name}");
                }
            }
            
            // Validar propiedades ignoradas
            foreach (var ignoredProp in IgnoreProperties)
            {
                var sourceProps = typeof(TSource).GetProperties().Select(p => p.Name).ToList();
                if (!sourceProps.Contains(ignoredProp, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"La propiedad ignorada '{ignoredProp}' no existe en el tipo {typeof(TSource).Name}");
                }
            }
            
            // Validar resolutores personalizados
            foreach (var resolver in CustomResolvers)
            {
                var destProps = typeof(TDestination).GetProperties().Select(p => p.Name).ToList();
                if (!destProps.Contains(resolver.Key, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"La propiedad de destino '{resolver.Key}' para el resolutor no existe en el tipo {typeof(TDestination).Name}");
                }
            }
        }
    }
    
    /// <summary>
    /// Clase base para perfiles de mapeo
    /// </summary>
    public abstract class SimpleAutoMappingProfile
    {
        private readonly List<Action<SimpleAutoMappingConfiguration>> _mappingConfigurations = new();
        
        protected SimpleAutoMappingProfile() { }
        
        /// <summary>
        /// Crea un mapeo completo entre dos tipos (propaga nulos por defecto)
        /// </summary>
        protected MappingOptions<TSource, TDestination> CreateMap<TSource, TDestination>()
            where TSource : class
            where TDestination : class
        {
            var options = new MappingOptions<TSource, TDestination> { IgnoreNullValues = false };
            _mappingConfigurations.Add(config => 
                config.RegisterMapping(typeof(TSource), typeof(TDestination), options));
            return options;
        }
        
        /// <summary>
        /// Crea un mapeo parcial entre dos tipos (ignora nulos por defecto)
        /// </summary>
        /// <remarks>
        /// Nota: No es necesario usar este método si ya has definido un mapeo con CreateMap.
        /// El método PartialMap usará automáticamente la configuración de CreateMap pero ignorando valores nulos.
        /// </remarks>
        protected MappingOptions<TSource, TDestination> CreatePartialMap<TSource, TDestination>()
            where TSource : class
            where TDestination : class
        {
            var options = new MappingOptions<TSource, TDestination> { IgnoreNullValues = true };
            _mappingConfigurations.Add(config => 
                config.RegisterMapping(typeof(TSource), typeof(TDestination), options));
            return options;
        }
        
        // Método para aplicar configuraciones al contenedor global
        internal void ApplyMappings(SimpleAutoMappingConfiguration configuration)
        {
            foreach (var config in _mappingConfigurations)
            {
                config(configuration);
            }
        }
    }
    
    /// <summary>
    /// Configuración global del mapper
    /// </summary>
    public class SimpleAutoMappingConfiguration
    {
        private readonly ConcurrentDictionary<(Type source, Type dest), object> _mappingOptions = new();
        private readonly ConcurrentDictionary<Type, Type> _defaultDestinations = new();
        private readonly ConcurrentDictionary<(Type source, Type dest), Func<object, object>> _typeConverters = new();
        private readonly ConcurrentDictionary<Type, Dictionary<Func<object, bool>, Type>> _typeMapConditions = new();
        
        /// <summary>
        /// Obtiene todas las configuraciones de mapeo registradas
        /// </summary>
        public IEnumerable<(Type sourceType, Type destType)> GetAllMappingConfigurations()
        {
            return _mappingOptions.Keys;
        }
        
        /// <summary>
        /// Registra una configuración de mapeo
        /// </summary>
        public void RegisterMapping(Type sourceType, Type destType, object options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
                
            // Validar la configuración antes de registrarla
            var validateMethod = options.GetType().GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic);
            validateMethod?.Invoke(options, null);
            
            _mappingOptions[(sourceType, destType)] = options;
            _defaultDestinations[sourceType] = destType;
        }
        
        /// <summary>
        /// Registra un conversor de tipos personalizado
        /// </summary>
        public void RegisterTypeConverter<TSource, TTarget>(Func<TSource, TTarget> converter)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter), "El conversor no puede ser null");
                
            _typeConverters[(typeof(TSource), typeof(TTarget))] = source => converter((TSource)source);
        }
        
        /// <summary>
        /// Obtiene el conversor de tipos registrado
        /// </summary>
        internal Func<object, object> GetTypeConverter(Type sourceType, Type targetType)
        {
            if (_typeConverters.TryGetValue((sourceType, targetType), out var converter))
                return converter;
            return null;
        }
        
        /// <summary>
        /// Obtiene las opciones de mapeo para un tipo origen y destino específicos
        /// </summary>
        public object GetMappingOptions(Type sourceType, Type destType)
        {
            if (_mappingOptions.TryGetValue((sourceType, destType), out var options))
            {
                return options;
            }
            return null;
        }
        
        /// <summary>
        /// Encuentra el tipo de destino para un mapeo predeterminado
        /// </summary>
        public Type FindDestinationType(Type sourceType)
        {
            if (_defaultDestinations.TryGetValue(sourceType, out var destType))
                return destType;
            
            // Verificar si es una colección y tenemos un mapeo para el tipo de elemento
            if (Mapper.IsCollection(sourceType))
            {
                Type sourceElementType = Mapper.GetElementType(sourceType);
                if (sourceElementType != null && _defaultDestinations.TryGetValue(sourceElementType, out var elementDestType))
                {
                    // Intentar construir el tipo de colección correspondiente (List<T> como predeterminado)
                    return typeof(List<>).MakeGenericType(elementDestType);
                }
            }
                
            return null;
        }
        
        /// <summary>
        /// Agrega un perfil de mapeo
        /// </summary>
        public SimpleAutoMappingConfiguration AddProfile<TProfile>() where TProfile : SimpleAutoMappingProfile, new()
        {
            var profile = new TProfile();
            profile.ApplyMappings(this);
            return this;
        }
        
        /// <summary>
        /// Agrega múltiples perfiles de mapeo
        /// </summary>
        public SimpleAutoMappingConfiguration AddProfiles(params Type[] profileTypes)
        {
            if (profileTypes == null)
                throw new ArgumentNullException(nameof(profileTypes));
                
            foreach (var profileType in profileTypes)
            {
                if (!typeof(SimpleAutoMappingProfile).IsAssignableFrom(profileType))
                    throw new ArgumentException($"El tipo {profileType.Name} no hereda de SimpleAutoMappingProfile");
                    
                if (Activator.CreateInstance(profileType) is SimpleAutoMappingProfile profile)
                {
                    profile.ApplyMappings(this);
                }
            }
            
            return this;
        }
        
        /// <summary>
        /// Agrega todos los perfiles de un ensamblado
        /// </summary>
        public SimpleAutoMappingConfiguration AddProfilesFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
                
            var profileTypes = assembly.GetTypes()
                .Where(t => typeof(SimpleAutoMappingProfile).IsAssignableFrom(t) && 
                            !t.IsAbstract && 
                            t.GetConstructor(Type.EmptyTypes) != null);
                            
            foreach (var profileType in profileTypes)
            {
                if (Activator.CreateInstance(profileType) is SimpleAutoMappingProfile profile)
                {
                    profile.ApplyMappings(this);
                }
            }
            
            return this;
        }
        
        /// <summary>
        /// Registra un mapeo condicional para tipos derivados
        /// </summary>
        /// <typeparam name="TBaseSource">Tipo base de origen</typeparam>
        /// <typeparam name="TDerivedSource">Tipo derivado de origen</typeparam>
        /// <typeparam name="TDestination">Tipo de destino para el mapeo</typeparam>
        /// <param name="condition">Condición opcional para aplicar este mapeo</param>
        /// <returns>Opciones de configuración para el mapeo</returns>
        public MappingOptions<TDerivedSource, TDestination> Include<TBaseSource, TDerivedSource, TDestination>(
            Func<TBaseSource, bool> condition = null) 
            where TBaseSource : class
            where TDerivedSource : class, TBaseSource
            where TDestination : class
        {
            var derivedType = typeof(TDerivedSource);
            var baseType = typeof(TBaseSource);
            var destType = typeof(TDestination);
            
            // Crear opciones para este mapeo
            var options = new MappingOptions<TDerivedSource, TDestination>();
            
            // Registrar el mapeo
            RegisterMapping(derivedType, destType, options);
            
            // Registrar la condición para el mapeo polimórfico
            if (condition != null)
            {
                // Adaptador para la condición que funciona con el tipo base
                Func<object, bool> typedCondition = obj => 
                {
                    if (obj is TBaseSource baseObj)
                    {
                        return condition(baseObj);
                    }
                    return false;
                };
                
                // Registrar la condición en el diccionario
                if (!_typeMapConditions.TryGetValue(baseType, out var conditions))
                {
                    conditions = new Dictionary<Func<object, bool>, Type>();
                    _typeMapConditions[baseType] = conditions;
                }
                
                // Si ya existe una condición para este tipo derivado, sobrescribirla
                foreach (var existingCondition in conditions.Keys.ToList())
                {
                    if (conditions[existingCondition] == derivedType)
                    {
                        conditions.Remove(existingCondition);
                    }
                }
                
                conditions[typedCondition] = derivedType;
            }
            else
            {
                // Sin condición, solo verificar el tipo
                Func<object, bool> typedCondition = obj => obj.GetType() == derivedType || obj.GetType().IsSubclassOf(derivedType);
                
                // Registrar la condición en el diccionario
                if (!_typeMapConditions.TryGetValue(baseType, out var conditions))
                {
                    conditions = new Dictionary<Func<object, bool>, Type>();
                    _typeMapConditions[baseType] = conditions;
                }
                
                // Si ya existe una condición para este tipo derivado, sobrescribirla
                foreach (var existingCondition in conditions.Keys.ToList())
                {
                    if (conditions[existingCondition] == derivedType)
                    {
                        conditions.Remove(existingCondition);
                    }
                }
                
                conditions[typedCondition] = derivedType;
            }
            
            return options;
        }
        
        /// <summary>
        /// Devuelve el tipo concreto que se debe usar para el mapeo basado en condiciones registradas
        /// </summary>
        /// <param name="sourceObject">Objeto origen</param>
        /// <param name="baseSourceType">Tipo base del objeto origen</param>
        /// <returns>Tipo concreto a usar, o null si no hay condiciones</returns>
        internal Type GetDerivedTypeForMapping(object sourceObject, Type baseSourceType)
        {
            if (sourceObject == null || !_typeMapConditions.TryGetValue(baseSourceType, out var conditions))
                return null;
                
            // Dividir las condiciones en explícitas e implícitas
            var explicitConditions = new Dictionary<Func<object, bool>, Type>();
            var implicitConditions = new Dictionary<Func<object, bool>, Type>();
            
            foreach (var condition in conditions)
            {
                // Si la condición usa un lambda personalizado, es explícita
                // Si usa la comprobación de tipo por defecto, es implícita
                if (condition.Key.Method.Name.Contains("TypedCondition"))
                {
                    explicitConditions.Add(condition.Key, condition.Value);
                }
                else
                {
                    implicitConditions.Add(condition.Key, condition.Value);
                }
            }
            
            // Primero evaluar condiciones explícitas (mayor prioridad)
            foreach (var condition in explicitConditions)
            {
                if (condition.Key(sourceObject))
                {
                    return condition.Value;
                }
            }
            
            // Luego evaluar las condiciones implícitas
            foreach (var condition in implicitConditions)
            {
                if (condition.Key(sourceObject))
                {
                    return condition.Value;
                }
            }
            
            // Si no se cumple ninguna condición, devolver null
            return null;
        }
    }
    
    /// <summary>
    /// Servicio de mapeo para inyección de dependencias
    /// </summary>
    public class SimpleAutoMappingService : ISimpleAutoMapping
    {
        private readonly SimpleAutoMappingConfiguration _configuration;
        
        public SimpleAutoMappingService(SimpleAutoMappingConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        
        /// <summary>
        /// Mapeo completo entre objetos
        /// </summary>
        public TDestination Map<TSource, TDestination>(TSource source, TDestination? destination = default)
            where TSource : class
            where TDestination : class
        {
            var options = _configuration.GetMappingOptions(typeof(TSource), typeof(TDestination)) 
                as MappingOptions<TSource, TDestination> ?? new MappingOptions<TSource, TDestination>();
                
            return Mapper.MapInternal(source, destination, options, _configuration);
        }
        
        /// <summary>
        /// Mapeo parcial entre objetos
        /// </summary>
        public TDestination PartialMap<TSource, TDestination>(TSource source, TDestination destination)
            where TSource : class
            where TDestination : class
        {
            // Buscar configuración predefinida específica para PartialMap
            var options = _configuration.GetMappingOptions(typeof(TSource), typeof(TDestination)) 
                as MappingOptions<TSource, TDestination> ?? new MappingOptions<TSource, TDestination>();
                
            // Forzar IgnoreNullValues para PATCH
            options.IgnoreNullValues = true;
            
            return Mapper.MapInternal(source, destination, options, _configuration);
        }
        
        /// <summary>
        /// Mapeo completo con inferencia de tipo destino
        /// </summary>
        public TDestination? Map<TDestination>(object source)
            where TDestination : class
        {
            if (source == null)
                return default;
                
            var sourceType = source.GetType();
            var destType = typeof(TDestination);
            
            var options = _configuration.GetMappingOptions(sourceType, destType);
            if (options == null)
                throw new InvalidOperationException($"No se encontró un mapeo configurado de {sourceType.Name} a {destType.Name}");
                
            return (TDestination)Mapper.MapObject(source, destType, options, _configuration);
        }
        
        /// <summary>
        /// Mapeo con inferencia de ambos tipos
        /// </summary>
        public object? Map(object source)
        {
            if (source == null)
                return null;
                
            var sourceType = source.GetType();
            var destType = _configuration.FindDestinationType(sourceType);
            
            if (destType == null)
                throw new InvalidOperationException($"No se encontró un mapeo predeterminado para {sourceType.Name}");
                
            var options = _configuration.GetMappingOptions(sourceType, destType);
            return Mapper.MapObject(source, destType, options, _configuration);
        }
        
        /// <summary>
        /// Mapeo parcial con inferencia de tipo origen
        /// </summary>
        public TDestination PartialMap<TDestination>(object source, TDestination destination)
            where TDestination : class
        {
            if (source == null || destination == null)
                return destination;
                
            var sourceType = source.GetType();
            var destType = typeof(TDestination);
            
            // Buscar configuración predefinida (ya sea para Map o PartialMap)
            var options = _configuration.GetMappingOptions(sourceType, destType);
            if (options == null)
                throw new InvalidOperationException($"No se encontró un mapeo configurado de {sourceType.Name} a {destType.Name}");
                
            // Forzar IgnoreNullValues para PATCH, independientemente de la configuración original
            dynamic optionsInstance = options;
            optionsInstance.IgnoreNullValues = true;
            optionsInstance.PreserveNestedNullValues = true;
            
            return (TDestination)Mapper.MapObject(source, destination, options, _configuration);
        }
        
        /// <summary>
        /// Mapeo simplificado a un tipo destino
        /// </summary>
        public TDestination MapTo<TDestination>(object source)
            where TDestination : class
        {
            return Map<TDestination>(source);
        }
        
        /// <summary>
        /// Mapeo simplificado a un objeto destino existente
        /// </summary>
        public TDestination MapTo<TDestination>(object source, TDestination destination)
            where TDestination : class
        {
            var sourceType = source.GetType();
            var destType = typeof(TDestination);
            
            var options = _configuration.GetMappingOptions(sourceType, destType);
            if (options == null)
                throw new InvalidOperationException($"No se encontró un mapeo configurado de {sourceType.Name} a {destType.Name}");
                
            return (TDestination)Mapper.MapObject(source, destination, options, _configuration);
        }
        
        /// <summary>
        /// Mapeo parcial simplificado a un nuevo objeto
        /// </summary>
        public TDestination PartialMapTo<TDestination>(object source)
            where TDestination : class
        {
            if (source == null)
                return default;
                
            var sourceType = source.GetType();
            var destType = typeof(TDestination);
            
            // Buscar configuración predefinida
            var options = _configuration.GetMappingOptions(sourceType, destType);
            if (options == null)
                throw new InvalidOperationException($"No se encontró un mapeo configurado de {sourceType.Name} a {destType.Name}");
                
            // Forzar IgnoreNullValues para PATCH
            dynamic optionsInstance = options;
            optionsInstance.IgnoreNullValues = true;
            
            // Crear un nuevo objeto destino
            var destination = Activator.CreateInstance<TDestination>();
            
            return (TDestination)Mapper.MapObject(source, destination, options, _configuration);
        }
        
        /// <summary>
        /// Mapeo parcial simplificado a un objeto destino existente
        /// </summary>
        public TDestination PartialMapTo<TDestination>(object source, TDestination destination)
            where TDestination : class
        {
            return PartialMap(source, destination);
        }
    }
    
    /// <summary>
    /// Clase principal de mapeo con métodos estáticos
    /// </summary>
    public static class Mapper
    {
        // Cache mejorado con estructuras optimizadas
        // Usamos lazily-initialized para mejor rendimiento en la primera operación
        private static readonly Lazy<ConcurrentDictionary<Type, PropertyInfo[]>> _propertyCache = 
            new(() => new ConcurrentDictionary<Type, PropertyInfo[]>());
            
        private static readonly Lazy<ConcurrentDictionary<PropertyInfo, Func<object, object>>> _getterCache = 
            new(() => new ConcurrentDictionary<PropertyInfo, Func<object, object>>());
            
        private static readonly Lazy<ConcurrentDictionary<PropertyInfo, Action<object, object>>> _setterCache = 
            new(() => new ConcurrentDictionary<PropertyInfo, Action<object, object>>());
            
        // Acceso inmutable a los cachés  
        private static ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache => _propertyCache.Value;
        private static ConcurrentDictionary<PropertyInfo, Func<object, object>> GetterCache => _getterCache.Value;
        private static ConcurrentDictionary<PropertyInfo, Action<object, object>> SetterCache => _setterCache.Value;
        
        // Configuración global
        private static SimpleAutoMappingConfiguration _configuration = new();
        public static SimpleAutoMappingConfiguration Configuration => _configuration;
        
        /// <summary>
        /// Mapeo completo entre objetos
        /// </summary>
        public static TDestination Map<TSource, TDestination>(
            TSource source,
            TDestination? destination = default,
            Action<MappingOptions<TSource, TDestination>>? configOptions = null)
            where TSource : class
            where TDestination : class
        {
            // Buscar configuración predefinida
            var options = _configuration.GetMappingOptions(typeof(TSource), typeof(TDestination)) 
                as MappingOptions<TSource, TDestination> ?? new MappingOptions<TSource, TDestination>();
                
            // Aplicar configuración adicional
            configOptions?.Invoke(options);
            
            // Validar configuración
            options.Validate();
            
            return MapInternal(source, destination, options, _configuration);
        }
        
        /// <summary>
        /// Mapeo completo con inferencia de tipo destino
        /// </summary>
        public static TDestination? Map<TDestination>(object source)
            where TDestination : class
        {
            if (source == null)
                return default;
                
            var sourceType = source.GetType();
            var destType = typeof(TDestination);
            
            var options = _configuration.GetMappingOptions(sourceType, destType);
            if (options == null)
                throw new InvalidOperationException($"No se encontró un mapeo configurado de {sourceType.Name} a {destType.Name}");
                
            return (TDestination)MapObject(source, destType, options, _configuration);
        }
        
        /// <summary>
        /// Mapeo con inferencia de ambos tipos
        /// </summary>
        public static object? Map(object source)
        {
            if (source == null)
                return null;
                
            var sourceType = source.GetType();
            var destType = _configuration.FindDestinationType(sourceType);
            
            if (destType == null)
                throw new InvalidOperationException($"No se encontró un mapeo predeterminado para {sourceType.Name}");
                
            var options = _configuration.GetMappingOptions(sourceType, destType);
            return MapObject(source, destType, options, _configuration);
        }
        
        /// <summary>
        /// Mapeo parcial entre objetos (ideal para PATCH)
        /// </summary>
        public static TDestination PartialMap<TSource, TDestination>(
            TSource source, 
            TDestination destination,
            Action<MappingOptions<TSource, TDestination>>? configOptions = null) 
            where TSource : class 
            where TDestination : class
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination), "El objeto destino no puede ser null en un mapeo parcial");
                
            // Buscar configuración predefinida
            var options = _configuration.GetMappingOptions(typeof(TSource), typeof(TDestination)) 
                as MappingOptions<TSource, TDestination> ?? new MappingOptions<TSource, TDestination>();
                
            // Forzar IgnoreNullValues para PATCH
            options.IgnoreNullValues = true;
            
            // Aplicar configuración adicional
            configOptions?.Invoke(options);
            
            // Validar configuración
            options.Validate();
            
            return MapInternal(source, destination, options, _configuration);
        }
        
        /// <summary>
        /// Mapeo parcial con inferencia de tipo origen
        /// </summary>
        public static TDestination PartialMap<TDestination>(object source, TDestination destination)
            where TDestination : class
        {
            if (source == null || destination == null)
                return destination;
                
            var sourceType = source.GetType();
            var destType = typeof(TDestination);
            
            // Buscar configuración predefinida (ya sea para Map o PartialMap)
            var options = _configuration.GetMappingOptions(sourceType, destType);
            if (options == null)
                throw new InvalidOperationException($"No se encontró un mapeo configurado de {sourceType.Name} a {destType.Name}");
                
            // Forzar IgnoreNullValues para PATCH, independientemente de la configuración original
            dynamic optionsInstance = options;
            optionsInstance.IgnoreNullValues = true;
            optionsInstance.PreserveNestedNullValues = true;
            
            return (TDestination)Mapper.MapObject(source, destination, options, _configuration);
        }
        
        /// <summary>
        /// Implementación interna del mapeo
        /// </summary>
        internal static TDestination MapInternal<TSource, TDestination>(
            TSource source,
            TDestination destination,
            MappingOptions<TSource, TDestination> options,
            SimpleAutoMappingConfiguration configuration)
            where TSource : class
            where TDestination : class
        {
            if (source == null)
                return destination;
            
            // Verificar si estamos tratando con un tipo derivado
            var sourceType = source.GetType();
            if (sourceType != typeof(TSource))
            {
                // Verificar si hay un mapeo específico para el tipo derivado real
                var derivedType = configuration.GetDerivedTypeForMapping(source, typeof(TSource));
                if (derivedType != null)
                {
                    // Buscar el tipo de destino correspondiente
                    var baseDestType = typeof(TDestination);
                    Type derivedDestType = null;
                    
                    // Buscar primero todas las configuraciones de mapeo para el tipo derivado
                    foreach (var mapping in configuration.GetAllMappingConfigurations())
                    {
                        if (mapping.sourceType == derivedType && baseDestType.IsAssignableFrom(mapping.destType))
                        {
                            derivedDestType = mapping.destType;
                            break;
                        }
                    }
                    
                    if (derivedDestType != null)
                    {
                        // Buscar opciones para el mapeo específico del tipo derivado
                        var derivedOptions = configuration.GetMappingOptions(derivedType, derivedDestType);
                        
                        if (derivedOptions != null)
                        {
                            // Crear una instancia del tipo destino derivado
                            object derivedDest;
                            if (destination == null || destination.GetType() != derivedDestType)
                            {
                                derivedDest = Activator.CreateInstance(derivedDestType);
                            }
                            else
                            {
                                derivedDest = destination;
                            }
                            
                            // Usar el mapeo específico del tipo derivado mediante reflexión
                            var mapMethod = typeof(Mapper).GetMethod("MapObject", 
                                BindingFlags.NonPublic | BindingFlags.Static,
                                null,
                                new[] { typeof(object), typeof(object), typeof(object), typeof(SimpleAutoMappingConfiguration) },
                                null);
                                
                            var result = mapMethod.Invoke(null, new[] { source, derivedDest, derivedOptions, configuration });
                            
                            // Importante: Devolver el objeto resultado con el tipo correcto
                            return (TDestination)result;
                        }
                    }
                    else
                    {
                        // Si no se encontró un tipo destino específico, buscar opciones para el tipo base
                        var derivedBaseOptions = configuration.GetMappingOptions(derivedType, typeof(TDestination));
                        if (derivedBaseOptions != null)
                        {
                            // Crear o usar la instancia de destino
                            if (destination == null)
                            {
                                destination = Activator.CreateInstance<TDestination>();
                            }
                            
                            // Usar el mapeo desde el tipo derivado al tipo base
                            var mapMethod = typeof(Mapper).GetMethod("MapObject", 
                                BindingFlags.NonPublic | BindingFlags.Static,
                                null,
                                new[] { typeof(object), typeof(object), typeof(object), typeof(SimpleAutoMappingConfiguration) },
                                null);
                                
                            var result = mapMethod.Invoke(null, new[] { source, destination, derivedBaseOptions, configuration });
                            return (TDestination)result;
                        }
                    }
                }
            }
            
            // Si llegamos aquí, no hubo manejo especial para tipos derivados
            
            if (destination == null)
                destination = Activator.CreateInstance<TDestination>();

            // Manejo especial para colecciones
            if (Mapper.IsCollection(typeof(TSource)) && Mapper.IsCollection(typeof(TDestination)))
            {
                return MapCollection(source, destination, options, configuration);
            }
                
            // Obtener propiedades destino (con caché)
            var destPropsArray = PropertyCache.GetOrAdd(typeof(TDestination), 
                t => t.GetProperties().Where(p => p.CanWrite).ToArray());
                
            foreach (var destProp in destPropsArray)
            {
                // Verificar si hay un resolutor personalizado para la propiedad destino
                if (options.CustomResolvers.TryGetValue(destProp.Name, out var resolver))
                {
                    var resolvedValue = resolver(source);
                    if (resolvedValue != null || !options.IgnoreNullValues)
                    {
                        SetPropertyValue(destination, destProp, resolvedValue);
                    }
                    continue;
                }
                
                // Determinar el nombre de la propiedad fuente correspondiente
                string sourcePropName = destProp.Name;
                var mapping = options.PropertyMappings.FirstOrDefault(
                    m => m.Value.Equals(destProp.Name, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(mapping.Key))
                {
                    sourcePropName = mapping.Key;
                }
                
                // Obtener propiedades fuente (con caché)
                var sourceProps = PropertyCache.GetOrAdd(typeof(TSource), 
                    t => t.GetProperties().Where(p => p.CanRead).ToArray());
                
                // Buscar la propiedad fuente
                var sourceProp = sourceProps.FirstOrDefault(
                    p => p.Name.Equals(sourcePropName, StringComparison.OrdinalIgnoreCase));
                
                // Si no existe la propiedad o está ignorada, continuar
                if (sourceProp == null || options.IgnoreProperties.Contains(sourceProp.Name))
                    continue;
                
                // Obtener valor fuente usando getter optimizado
                var getter = GetOrCreateGetter(sourceProp);
                var value = getter(source);
                
                // Aplicar transformación si existe
                if (options.ValueTransformers.TryGetValue(sourceProp.Name, out var transformer))
                {
                    value = transformer(value);
                }
                
                // Saltar nulos si está configurado
                if (value == null && options.IgnoreNullValues)
                    continue;

                // Mapeo de colecciones
                if (options.MapNestedObjects && value != null && 
                    IsCollection(value.GetType()) && IsCollection(destProp.PropertyType))
                {
                    var sourceCollection = value as IEnumerable;
                    var destCollection = destProp.GetValue(destination);
                    
                    // Crear la colección destino si es nula
                    if (destCollection == null)
                    {
                        destCollection = CreateCollection(destProp.PropertyType);
                        if (destCollection != null)
                        {
                            SetPropertyValue(destination, destProp, destCollection);
                        }
                        else
                        {
                            continue; // No se pudo crear la colección
                        }
                    }
                    
                    // Mapear la colección
                    MapCollectionProperty(sourceCollection, destCollection, destProp.PropertyType, 
                        options.IgnoreNullValues, configuration);
                    continue;
                }
                
                // Mapeo de objetos anidados
                if (options.MapNestedObjects && value != null && 
                    !IsPrimitiveType(value.GetType()) && !IsCollection(value.GetType()) && 
                    !IsPrimitiveType(destProp.PropertyType) && !IsCollection(destProp.PropertyType))
                {
                    var destValue = destProp.GetValue(destination);
                    
                    if (destValue == null && destProp.PropertyType.GetConstructor(Type.EmptyTypes) != null)
                    {
                        destValue = Activator.CreateInstance(destProp.PropertyType);
                        SetPropertyValue(destination, destProp, destValue);
                    }
                    
                    if (destValue != null)
                    {
                        var nestedOptions = configuration.GetMappingOptions(value.GetType(), destValue.GetType());
                        
                        if (nestedOptions != null)
                        {
                            // Copiar configuración de preservar nulos anidados
                            if (options.GetType().GetProperty("PreserveNestedNullValues") != null && 
                                nestedOptions.GetType().GetProperty("PreserveNestedNullValues") != null)
                            {
                                var preserveNulls = options.GetType().GetProperty("PreserveNestedNullValues").GetValue(options);
                                nestedOptions.GetType().GetProperty("PreserveNestedNullValues").SetValue(nestedOptions, preserveNulls);
                            }
                            
                            MapObject(value, destValue, nestedOptions, configuration);
                        }
                        else
                        {
                            // Mapeo automático por convención
                            var preserveNulls = false;
                            if (options.GetType().GetProperty("PreserveNestedNullValues") != null)
                            {
                                preserveNulls = (bool)options.GetType().GetProperty("PreserveNestedNullValues").GetValue(options);
                            }
                            
                            AutoMapByConvention(value, destValue, options.IgnoreNullValues, preserveNulls);
                        }
                    }
                    continue;
                }
                
                // Asignar si los tipos son compatibles
                if (destProp.PropertyType.IsAssignableFrom(value?.GetType()))
                {
                    SetPropertyValue(destination, destProp, value);
                }
                else
                {
                    // Intentar usar conversor de tipos registrado
                    var converter = configuration.GetTypeConverter(value?.GetType(), destProp.PropertyType);
                    if (converter != null && value != null)
                    {
                        try
                        {
                            var convertedValue = converter(value);
                            SetPropertyValue(destination, destProp, convertedValue);
                        }
                        catch { /* Ignorar errores de conversión */ }
                    }
                    else
                    {
                        // Intentar conversión estándar
                        try
                        {
                            var convertedValue = Convert.ChangeType(value, destProp.PropertyType);
                            SetPropertyValue(destination, destProp, convertedValue);
                        }
                        catch { /* Ignorar errores de conversión */ }
                    }
                }
            }
            
            return destination;
        }

        /// <summary>
        /// Mapea una colección completa de elementos
        /// </summary>
        private static TDestination MapCollection<TSource, TDestination>(
            TSource source, 
            TDestination destination, 
            MappingOptions<TSource, TDestination> options,
            SimpleAutoMappingConfiguration configuration)
            where TSource : class
            where TDestination : class
        {
            var sourceCollection = source as IEnumerable;
            
            // Si la colección de destino es nula, hay que crearla
            if (destination == null)
            {
                // Manejo especial para arrays
                if (typeof(TDestination).IsArray)
                {
                    // Determinar tipo de elemento para arrays
                    var destElementType = typeof(TDestination).GetElementType();
                    if (destElementType == null)
                        return null;
                    
                    // Contar elementos en la colección fuente
                    int count = 0;
                    foreach (var _ in sourceCollection)
                        count++;
                    
                    // Crear array del tamaño correcto
                    var array = Array.CreateInstance(destElementType, count);
                    
                    // Mapear cada elemento
                    int index = 0;
                    foreach (var sourceItem in sourceCollection)
                    {
                        if (sourceItem == null && options.IgnoreNullValues)
                            continue;
                        
                        var destItem = MapElement(sourceItem, destElementType, configuration);
                        array.SetValue(destItem, index++);
                    }
                    
                    return array as TDestination;
                }
                else
                {
                    // Para otros tipos de colecciones
                    destination = CreateCollection(typeof(TDestination)) as TDestination;
                    if (destination == null)
                    {
                        return null; // No se pudo crear la colección
                    }
                }
            }
            
            // Mapear elementos de la colección
            MapCollectionProperty(sourceCollection, destination, typeof(TDestination), 
                options.IgnoreNullValues, configuration);
            
            return destination;
        }
        
        /// <summary>
        /// Mapea un elemento individual dentro de una colección
        /// </summary>
        private static object MapElement(object sourceItem, Type destElementType, SimpleAutoMappingConfiguration configuration)
        {
            if (sourceItem == null)
                return null;
                
            if (IsPrimitiveType(destElementType))
            {
                // Para tipos primitivos, hacer conversión directa
                try 
                {
                    // Intentar usar conversor de tipos registrado
                    var converter = configuration.GetTypeConverter(sourceItem.GetType(), destElementType);
                    if (converter != null)
                    {
                        return converter(sourceItem);
                    }
                    
                    // Intentar conversión estándar
                    return Convert.ChangeType(sourceItem, destElementType);
                }
                catch
                {
                    return GetDefaultValue(destElementType);
                }
            }
            else
            {
                // Para objetos complejos, hacer mapping recursivo
                var sourceType = sourceItem.GetType();
                var options = configuration.GetMappingOptions(sourceType, destElementType);
                
                if (options != null)
                {
                    // Crear instancia del elemento destino
                    var destItem = destElementType.GetConstructor(Type.EmptyTypes) != null 
                        ? Activator.CreateInstance(destElementType) 
                        : null;
                    
                    if (destItem != null)
                    {
                        return MapObject(sourceItem, destItem, options, configuration);
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Devuelve el valor predeterminado para un tipo
        /// </summary>
        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
        
        /// <summary>
        /// Mapeo de colecciones internas (propiedades tipo lista/array)
        /// </summary>
        private static void MapCollectionProperty(
            IEnumerable sourceCollection, 
            object destCollection, 
            Type destCollectionType, 
            bool ignoreNulls,
            SimpleAutoMappingConfiguration configuration)
        {
            // Obtener información del tipo de elemento de destino
            Type destElementType = GetElementType(destCollectionType);
            if (destElementType == null)
                return;
                
            // Obtener métodos para manipular la colección de destino
            var addMethod = GetAddMethod(destCollection);
            var clearMethod = GetClearMethod(destCollection);
            
            if (addMethod == null)
                return;
                
            // Limpiar colección destino si es posible
            clearMethod?.Invoke(destCollection, null);
            
            // Mapear elementos
            foreach (var sourceItem in sourceCollection)
            {
                if (sourceItem == null && ignoreNulls)
                    continue;
                    
                object destItem;
                
                if (IsPrimitiveType(destElementType))
                {
                    // Para tipos primitivos, hacer conversión directa
                    try 
                    {
                        // Intentar usar conversor de tipos registrado
                        if (sourceItem != null)
                        {
                            var converter = configuration.GetTypeConverter(sourceItem.GetType(), destElementType);
                            if (converter != null)
                            {
                                destItem = converter(sourceItem);
                            }
                            else
                            {
                                destItem = Convert.ChangeType(sourceItem, destElementType);
                            }
                        }
                        else
                        {
                            destItem = null;
                        }
                    }
                    catch
                    {
                        continue; // Skip si no se puede convertir
                    }
                }
                else
                {
                    // Para objetos complejos, crear instancia y mapear
                    if (destElementType.GetConstructor(Type.EmptyTypes) == null)
                        continue; // Skip si no tiene constructor sin parámetros
                        
                    destItem = Activator.CreateInstance(destElementType);
                    
                    if (sourceItem != null)
                    {
                        var sourceItemType = sourceItem.GetType();
                        
                        // Verificar si existe mapeo configurado
                        var itemOptions = configuration.GetMappingOptions(sourceItemType, destElementType);
                        
                        if (itemOptions != null)
                        {
                            MapObject(sourceItem, destItem, itemOptions, configuration);
                        }
                        else
                        {
                            // Mapeo automático por convención
                            AutoMapByConvention(sourceItem, destItem, ignoreNulls);
                        }
                    }
                }
                
                // Agregar elemento a la colección destino
                addMethod.Invoke(destCollection, new[] { destItem });
            }
        }
        
        /// <summary>
        /// Verifica si un tipo es primitivo o simple
        /// </summary>
        private static bool IsPrimitiveType(Type type)
        {
            return type.IsPrimitive || 
                   type == typeof(string) || 
                   type == typeof(decimal) || 
                   type == typeof(DateTime) || 
                   type == typeof(DateTimeOffset) || 
                   type == typeof(TimeSpan) || 
                   type == typeof(Guid) ||
                   type.IsEnum ||
                   Nullable.GetUnderlyingType(type) != null;
        }
        
        /// <summary>
        /// Mapeo dinámico basado en tipos en runtime
        /// </summary>
        internal static object MapObject(object source, Type destType, object options, SimpleAutoMappingConfiguration configuration)
        {
            var destObject = Activator.CreateInstance(destType);
            return MapObject(source, destObject, options, configuration);
        }
        
        /// <summary>
        /// Mapeo dinámico a objeto existente
        /// </summary>
        internal static object MapObject(object source, object destination, object options, SimpleAutoMappingConfiguration configuration)
        {
            if (source == null)
                return destination;
                
            var sourceType = source.GetType();
            var destType = destination.GetType();
            
            // Verificar si ambos son colecciones
            if (IsCollection(sourceType) && IsCollection(destType))
            {
                var sourceElementType = GetElementType(sourceType);
                var destElementType = GetElementType(destType);
                
                // Crear una versión genérica del método MapCollection
                var mapCollectionMethod = typeof(Mapper).GetMethod("MapCollectionObjects", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                    
                if (mapCollectionMethod != null)
                {
                    var collectionGenericMethod = mapCollectionMethod.MakeGenericMethod(sourceElementType, destElementType);
                    return collectionGenericMethod.Invoke(null, new[] { source, destination, options, configuration });
                }
            }
            
            // Invocar MapInternal con tipos correctos por reflexión
            var mapMethod = typeof(Mapper).GetMethod("MapInternal", 
                BindingFlags.NonPublic | BindingFlags.Static);
                
            var genericMethod = mapMethod.MakeGenericMethod(sourceType, destType);
            return genericMethod.Invoke(null, new[] { source, destination, options, configuration });
        }
        
        /// <summary>
        /// Mapeo de colección de objetos usando opciones de mapeo para los elementos
        /// </summary>
        private static IEnumerable<TDestElement> MapCollectionObjects<TSourceElement, TDestElement>(
            IEnumerable sourceCollection, 
            IEnumerable destCollection,
            object elementOptions,
            SimpleAutoMappingConfiguration configuration)
            where TSourceElement : class
            where TDestElement : class
        {
            // Si la colección destino es null, crear una nueva
            var result = destCollection as IList<TDestElement> ?? new List<TDestElement>();
            
            // Limpiar la colección destino si es posible
            if (result is IList list && list.Count > 0)
            {
                list.Clear();
            }
            
            // Mapear cada elemento usando las opciones de mapeo de elementos
            foreach (var sourceItem in sourceCollection)
            {
                if (sourceItem == null)
                    continue;
                    
                if (!(sourceItem is TSourceElement typedSourceItem))
                    continue;
                    
                // Crear destino y mapearlo
                var destItem = Activator.CreateInstance<TDestElement>();
                
                // Invocar MapInternal con tipos correctos por reflexión
                var mapMethod = typeof(Mapper).GetMethod("MapInternal", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                    
                var genericMethod = mapMethod.MakeGenericMethod(typeof(TSourceElement), typeof(TDestElement));
                destItem = (TDestElement)genericMethod.Invoke(null, new[] { typedSourceItem, destItem, elementOptions, configuration });
                
                // Agregar al resultado
                if (result is IList<TDestElement> resultList)
                {
                    resultList.Add(destItem);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Mapeo automático por convención (nombres iguales)
        /// </summary>
        private static void AutoMapByConvention(object source, object destination, bool ignoreNulls, bool preserveNestedNulls = false)
        {
            if (source == null || destination == null)
                return;
                
            var sourceType = source.GetType();
            var destType = destination.GetType();
            
            // Obtener propiedades fuente y destino
            var sourceProps = PropertyCache.GetOrAdd(sourceType, t => t.GetProperties().Where(p => p.CanRead).ToArray());
            var destProps = PropertyCache.GetOrAdd(destType, t => t.GetProperties().Where(p => p.CanWrite).ToArray());
            
            // Mapear propiedades por nombre
            foreach (var sourceProp in sourceProps)
            {
                // Buscar propiedad destino con el mismo nombre
                var destProp = destProps.FirstOrDefault(
                    p => p.Name.Equals(sourceProp.Name, StringComparison.OrdinalIgnoreCase));
                    
                if (destProp == null)
                    continue;
                    
                // Obtener valor usando getter optimizado
                var getter = GetOrCreateGetter(sourceProp);
                var value = getter(source);
                
                // Ignorar valores nulos si está configurado
                if (value == null && ignoreNulls)
                    continue;
                    
                // Mapeo de objetos anidados
                if (value != null && !IsPrimitiveType(sourceProp.PropertyType) && !IsCollection(sourceProp.PropertyType))
                {
                    var destValue = destProp.GetValue(destination);
                    
                    if (destValue != null)
                    {
                        // Si el objeto ya existe, mapear recursivamente
                        AutoMapByConvention(value, destValue, ignoreNulls, preserveNestedNulls);
                        continue;
                    }
                }
                
                // Mapeo directo para tipos primitivos y cuando no hay objeto anidado existente
                if (destProp.PropertyType.IsAssignableFrom(value?.GetType()))
                {
                    SetPropertyValue(destination, destProp, value);
                }
                else if (value != null)
                {
                    // Intentar conversión
                    try
                    {
                        var convertedValue = Convert.ChangeType(value, destProp.PropertyType);
                        SetPropertyValue(destination, destProp, convertedValue);
                    }
                    catch { /* Ignorar errores de conversión */ }
                }
            }
        }
        
        /// <summary>
        /// Obtiene un getter optimizado para una propiedad
        /// </summary>
        private static Func<object, object> GetOrCreateGetter(PropertyInfo property)
        {
            return GetterCache.GetOrAdd(property, prop => 
            {
                // Compilar expresión lambda optimizada para el getter
                var instance = Expression.Parameter(typeof(object), "instance");
                var instanceCast = Expression.Convert(instance, prop.DeclaringType);
                var propertyAccess = Expression.Property(instanceCast, prop);
                var propertyCast = Expression.Convert(propertyAccess, typeof(object));
                
                return Expression.Lambda<Func<object, object>>(propertyCast, instance).Compile();
            });
        }
        
        /// <summary>
        /// Configura un valor de propiedad usando setters compilados
        /// </summary>
        private static void SetPropertyValue(object target, PropertyInfo property, object value)
        {
            var setter = SetterCache.GetOrAdd(property, prop => 
            {
                if (!prop.CanWrite)
                    return (_, __) => { }; // No-op
                    
                // Compilar expresión lambda optimizada para el setter
                var targetParam = Expression.Parameter(typeof(object), "target");
                var valueParam = Expression.Parameter(typeof(object), "value");
                
                var targetCast = Expression.Convert(targetParam, prop.DeclaringType);
                var valueCast = Expression.Convert(valueParam, prop.PropertyType);
                
                var setterCall = Expression.Call(targetCast, prop.SetMethod, valueCast);
                
                return Expression.Lambda<Action<object, object>>(setterCall, targetParam, valueParam).Compile();
            });
            
            setter(target, value);
        }
        
        /// <summary>
        /// Verifica si un tipo es una colección
        /// </summary>
        public static bool IsCollection(Type type)
        {
            if (type == typeof(string))
                return false;
                
            return typeof(IEnumerable).IsAssignableFrom(type);
        }
        
        /// <summary>
        /// Obtiene el tipo de elemento de una colección
        /// </summary>
        public static Type GetElementType(Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();
                
            // Buscar implementaciones genéricas de IEnumerable<T>
            foreach (var iface in collectionType.GetInterfaces())
            {
                if (iface.IsGenericType && 
                    iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return iface.GetGenericArguments()[0];
                }
            }
            
            // Verificar si la clase implementa IEnumerable<T> directamente
            if (collectionType.IsGenericType && 
                collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return collectionType.GetGenericArguments()[0];
            }
            
            // Si no se encuentra un tipo genérico, asumir Object
            return typeof(object);
        }
        
        /// <summary>
        /// Crea una instancia de colección del tipo especificado
        /// </summary>
        private static object CreateCollection(Type collectionType)
        {
            // Para Arrays (no se pueden crear directamente, se usa List<T>)
            if (collectionType.IsArray)
            {
                Type elementType = collectionType.GetElementType();
                Type listType = typeof(List<>).MakeGenericType(elementType);
                return Activator.CreateInstance(listType);
            }
            
            // Interfaces comunes y sus implementaciones concretas
            if (collectionType.IsInterface)
            {
                if (collectionType.IsGenericType)
                {
                    Type genericTypeDefinition = collectionType.GetGenericTypeDefinition();
                    Type[] genericArgs = collectionType.GetGenericArguments();
                    
                    if (genericTypeDefinition == typeof(IList<>) || 
                        genericTypeDefinition == typeof(ICollection<>) || 
                        genericTypeDefinition == typeof(IEnumerable<>))
                    {
                        Type listType = typeof(List<>).MakeGenericType(genericArgs);
                        return Activator.CreateInstance(listType);
                    }
                    
                    if (genericTypeDefinition == typeof(ISet<>))
                    {
                        Type setType = typeof(HashSet<>).MakeGenericType(genericArgs);
                        return Activator.CreateInstance(setType);
                    }
                    
                    if (genericTypeDefinition == typeof(IDictionary<,>))
                    {
                        Type dictType = typeof(Dictionary<,>).MakeGenericType(genericArgs);
                        return Activator.CreateInstance(dictType);
                    }
                }
                
                // Colecciones no genéricas
                if (collectionType == typeof(IList) || 
                    collectionType == typeof(ICollection) || 
                    collectionType == typeof(IEnumerable))
                {
                    return new List<object>();
                }
                
                if (collectionType == typeof(IDictionary))
                {
                    return new Dictionary<object, object>();
                }
                
                // No se pudo determinar una implementación concreta
                return null;
            }
            
            // Si es un tipo concreto con constructor sin parámetros
            if (collectionType.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(collectionType);
            }
            
            return null;
        }
        
        /// <summary>
        /// Obtiene el método Add de una colección
        /// </summary>
        private static MethodInfo GetAddMethod(object collection)
        {
            var collectionType = collection.GetType();
            
            // Si es List<T> o Collection<T> usar método Add genérico
            MethodInfo addMethod = collectionType.GetMethod("Add");
            if (addMethod != null && addMethod.GetParameters().Length == 1)
                return addMethod;
                
            // Para ICollection<T>
            var interfaces = collectionType.GetInterfaces();
            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && 
                    iface.GetGenericTypeDefinition() == typeof(ICollection<>))
                {
                    addMethod = iface.GetMethod("Add");
                    if (addMethod != null)
                        return addMethod;
                }
            }
            
            // Para IDictionary<TKey, TValue>
            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && 
                    iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    addMethod = iface.GetMethod("Add");
                    if (addMethod != null)
                        return addMethod;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Obtiene el método Clear de una colección
        /// </summary>
        private static MethodInfo GetClearMethod(object collection)
        {
            var collectionType = collection.GetType();
            
            // Intentar obtener método Clear directamente
            MethodInfo clearMethod = collectionType.GetMethod("Clear", Type.EmptyTypes);
            if (clearMethod != null)
                return clearMethod;
                
            // Buscar en interfaces
            var interfaces = collectionType.GetInterfaces();
            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && 
                    (iface.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                     iface.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
                {
                    clearMethod = iface.GetMethod("Clear", Type.EmptyTypes);
                    if (clearMethod != null)
                        return clearMethod;
                }
            }
            
            // Para colecciones no genéricas
            if (typeof(ICollection).IsAssignableFrom(collectionType) ||
                typeof(IDictionary).IsAssignableFrom(collectionType))
            {
                return typeof(ICollection).GetMethod("Clear");
            }
            
            return null;
        }
        
        /// <summary>
        /// Limpia todas las cachés para liberar memoria
        /// </summary>
        public static void ClearCaches()
        {
            PropertyCache.Clear();
            GetterCache.Clear();
            SetterCache.Clear();
        }
    }
    
    /// <summary>
    /// Extensiones para configurar el servicio en DI
    /// </summary>
    public static class SimpleAutoMappingExtensions
    {
        /// <summary>
        /// Agrega SimpleAutoMapping como servicio
        /// </summary>
        public static IServiceCollection AddSimpleAutoMapping(this IServiceCollection services, Action<SimpleAutoMappingConfiguration>? configure = null)
        {
            // Configurar el mapper global
            if (configure != null)
            {
                configure(Mapper.Configuration);
            }
            
            // Registrar como singleton
            services.AddSingleton(Mapper.Configuration);
            services.AddSingleton<ISimpleAutoMapping, SimpleAutoMappingService>();
            
            return services;
        }
        
        /// <summary>
        /// Extensión para simplificar el mapeo de un objeto a otro tipo usando perfiles configurados
        /// </summary>
        public static TDestination MapTo<TDestination>(this object source) where TDestination : class
        {
            return Mapper.Map<TDestination>(source);
        }
        
        /// <summary>
        /// Extensión para simplificar el mapeo entre objetos usando perfiles configurados
        /// </summary>
        public static TDestination MapTo<TDestination>(this object source, TDestination destination) where TDestination : class
        {
            var sourceType = source.GetType();
            var destType = typeof(TDestination);
            
            var options = Mapper.Configuration.GetMappingOptions(sourceType, destType);
            if (options == null)
                throw new InvalidOperationException($"No se encontró un mapeo configurado de {sourceType.Name} a {destType.Name}");
                
            return (TDestination)Mapper.MapObject(source, destination, options, Mapper.Configuration);
        }
        
        /// <summary>
        /// Extensión para simplificar el mapeo parcial a un nuevo objeto usando perfiles configurados
        /// </summary>
        public static TDestination PartialMapTo<TDestination>(this object source) where TDestination : class
        {
            var destination = Activator.CreateInstance<TDestination>();
            return PartialMapTo(source, destination);
        }
        
        /// <summary>
        /// Extensión para simplificar el mapeo parcial entre objetos usando perfiles configurados
        /// </summary>
        public static TDestination PartialMapTo<TDestination>(this object source, TDestination destination) where TDestination : class
        {
            return Mapper.PartialMap<TDestination>(source, destination);
        }
        
        /// <summary>
        /// Extiende IQueryable para proyectar a un tipo destino
        /// </summary>
        /// <typeparam name="TSource">Tipo origen</typeparam>
        /// <typeparam name="TDestination">Tipo destino</typeparam>
        /// <param name="source">IQueryable de tipo origen</param>
        /// <returns>IQueryable proyectado al tipo destino</returns>
        public static IQueryable<TDestination> ProjectTo<TSource, TDestination>(
            this IQueryable<TSource> source)
            where TSource : class
            where TDestination : class
        {
            return ProjectTo<TSource, TDestination>(source, Mapper.Configuration);
        }
        
        /// <summary>
        /// Extiende IQueryable para proyectar a un tipo destino usando una configuración específica
        /// </summary>
        public static IQueryable<TDestination> ProjectTo<TSource, TDestination>(
            this IQueryable<TSource> source,
            SimpleAutoMappingConfiguration configuration)
            where TSource : class
            where TDestination : class
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
                
            // Construir expresión de proyección
            // x => new TDestination { Prop1 = x.Prop1, Prop2 = x.Prop2, ... }
            var parameter = Expression.Parameter(typeof(TSource), "x");
            var bindings = new List<MemberBinding>();
            
            // Obtener propiedades de destino
            var destProps = typeof(TDestination).GetProperties()
                .Where(p => p.CanWrite)
                .ToArray();
                
            // Obtener opciones de mapeo si existen
            var options = configuration.GetMappingOptions(typeof(TSource), typeof(TDestination)) 
                as MappingOptions<TSource, TDestination>;
                
            // Obtener propiedades de origen
            var sourceProps = typeof(TSource).GetProperties()
                .Where(p => p.CanRead)
                .ToArray();
                
            foreach (var destProp in destProps)
            {
                // Determinar el nombre de la propiedad origen
                string sourcePropName = destProp.Name;
                
                // Usar mapeo personalizado si está configurado
                if (options != null && options.PropertyMappings.Any(m => m.Value.Equals(destProp.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    var mapping = options.PropertyMappings.FirstOrDefault(m => m.Value.Equals(destProp.Name, StringComparison.OrdinalIgnoreCase));
                    sourcePropName = mapping.Key;
                }
                
                // Obtener propiedad origen
                var sourceProp = sourceProps.FirstOrDefault(p => p.Name.Equals(sourcePropName, StringComparison.OrdinalIgnoreCase));
                
                // Ignorar si la propiedad origen no existe o está en la lista de ignorados
                if (sourceProp == null || (options?.IgnoreProperties.Contains(sourceProp.Name) == true))
                    continue;
                    
                // Crear acceso a la propiedad: x.Property
                var propertyAccess = Expression.Property(parameter, sourceProp);
                
                // Aplicar transformaciones si hay alguna configurada
                Expression valueExpression = propertyAccess;
                if (options?.ValueTransformers.TryGetValue(sourceProp.Name, out var transformer) == true)
                {
                    // Para transformaciones, debemos usar una llamada a método
                    // No podemos aplicar las transformaciones directamente en la expresión LINQ
                    // En su lugar, registramos una propiedad calculada
                    // Nota: Esto carga el objeto en memoria, así que es menos eficiente para bases de datos
                    valueExpression = Expression.Call(
                        Expression.Constant(new ProjectionHelper()), 
                        typeof(ProjectionHelper).GetMethod("Transform"),
                        propertyAccess,
                        Expression.Constant(transformer));
                }
                
                // Aplicar conversiones si los tipos no coinciden
                if (!destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                {
                    // Intentar conversión si los tipos son compatibles
                    try
                    {
                        valueExpression = Expression.Convert(valueExpression, destProp.PropertyType);
                    }
                    catch
                    {
                        // Si no se puede convertir, ignorar esta propiedad
                        continue;
                    }
                }
                
                // Crear enlace de propiedad: Property = x.Property
                var binding = Expression.Bind(destProp, valueExpression);
                bindings.Add(binding);
            }
            
            // Crear expresión para el constructor del objeto destino
            var newExpression = Expression.New(typeof(TDestination));
            
            // Crear inicializador con todos los bindings: new TDestination { Prop1 = x.Prop1, ... }
            var initExpression = Expression.MemberInit(newExpression, bindings);
            
            // Crear lambda: x => new TDestination { ... }
            var lambda = Expression.Lambda<Func<TSource, TDestination>>(initExpression, parameter);
            
            // Aplicar proyección al IQueryable
            return source.Select(lambda);
        }
        
        /// <summary>
        /// Versión simplificada de ProjectTo usando inferencia de tipos
        /// </summary>
        public static IQueryable<TDestination> ProjectTo<TDestination>(
            this IQueryable source) where TDestination : class
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
                
            // Obtener tipo de origen del IQueryable
            Type sourceType = source.ElementType;
            Type destType = typeof(TDestination);
            
            // Obtener método genérico ProjectTo
            var methodInfo = typeof(SimpleAutoMappingExtensions).GetMethods()
                .First(m => m.Name == "ProjectTo" && m.GetParameters().Length == 1)
                .MakeGenericMethod(sourceType, destType);
                
            // Invocar método genérico
            return (IQueryable<TDestination>)methodInfo.Invoke(null, new[] { source });
        }
    }
    
    /// <summary>
    /// Clase auxiliar para aplicar transformaciones en proyecciones
    /// </summary>
    internal class ProjectionHelper
    {
        /// <summary>
        /// Aplica una transformación a un valor en una proyección
        /// </summary>
        public TResult Transform<TValue, TResult>(TValue value, Func<object, object> transformer)
        {
            if (value == null)
                return default;
                
            return (TResult)transformer(value);
        }
    }
}