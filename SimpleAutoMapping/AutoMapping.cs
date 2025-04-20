using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
        // Cache optimizado con estructuras de alto rendimiento
        // Usamos dictionaries estáticos para mejor rendimiento
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();
        private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object>> _getterCache = new();
        private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object>> _setterCache = new();
        
        // Cache para tipos más comunes
        private static readonly ConcurrentDictionary<Type, bool> _isPrimitiveTypeCache = new();
        private static readonly ConcurrentDictionary<Type, bool> _isCollectionTypeCache = new();
        private static readonly ConcurrentDictionary<Type, Type> _elementTypeCache = new();
        private static readonly ConcurrentDictionary<(Type sourceType, Type destType), bool> _isAssignableCache = new();
        
        // Cache para expresiones compiladas de mapeo
        private static readonly ConcurrentDictionary<(Type sourceType, Type destType), Delegate> _mappingDelegateCache = new();
        
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
            
            // Verificar si hay un mapeo compilado en caché para este par de tipos
            var key = (typeof(TSource), typeof(TDestination));
            if (_mappingDelegateCache.TryGetValue(key, out var cachedDelegate) && 
                !options.IgnoreNullValues && // Solo usar caché para mapeo completo, no parcial
                source.GetType() == typeof(TSource)) // No usar caché para tipos derivados
            {
                // Si existe un delegado compilado, usarlo directamente
                if (destination == null)
                    destination = Activator.CreateInstance<TDestination>();
                    
                var mappingFunc = (Func<TSource, TDestination, TDestination>)cachedDelegate;
                return mappingFunc(source, destination);
            }

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
            if (IsCollection(typeof(TSource)) && IsCollection(typeof(TDestination)))
            {
                return MapCollection<TSource, TDestination>(source, destination, options, configuration);
            }
            
            // Para mapeo directo sin condiciones especiales, crear y cachear un delegado optimizado
            if (!options.IgnoreNullValues && 
                options.CustomResolvers.Count == 0 && 
                options.ValueTransformers.Count == 0 &&
                source.GetType() == typeof(TSource))
            {
                // En lugar de intentar usar el método que falta, usaremos MapInternalWithReflection
                // var mappingFunc = CompileMappingDelegate<TSource, TDestination>(options);
                // _mappingDelegateCache[key] = mappingFunc;
                // return mappingFunc(source, destination);
                
                // Usar el enfoque basado en reflexión para todos los casos
                return MapInternalWithReflection(source, destination, options, configuration);
            }
            
            // Si no se puede optimizar con delegados compilados, usar el enfoque basado en reflexión
            return MapInternalWithReflection(source, destination, options, configuration);
        }

        /// <summary>
        /// Mapeo específico para colecciones
        /// </summary>
        private static TDestination MapCollection<TSource, TDestination>(
            TSource source,
            TDestination destination,
            MappingOptions<TSource, TDestination> options,
            SimpleAutoMappingConfiguration configuration)
            where TSource : class
            where TDestination : class
        {
            if (source == null)
                return destination;

            // Obtener tipo de elemento para la colección origen y destino
            Type sourceElementType = GetElementType(typeof(TSource));
            Type destElementType = GetElementType(typeof(TDestination));
            
            // Convertir a IEnumerable para iteración genérica
            var sourceCollection = source as IEnumerable;
            
            // Asegurarse que la colección destino es válida
            if (destination == null)
            {
                destination = Activator.CreateInstance<TDestination>();
            }
            
            // Si el destino es un array, necesitamos un enfoque especial
            if (typeof(TDestination).IsArray)
            {
                // Convertir cada elemento y crear un nuevo array
                var items = new List<object>();
                foreach (var item in sourceCollection)
                {
                    if (item == null && options.IgnoreNullValues)
                        continue;
                        
                    if (item == null)
                    {
                        items.Add(null);
                        continue;
                    }
                    
                    // Mapear el elemento según su tipo
                    var sourceItemType = item.GetType();
                    object destinationItem;
                    
                    if (destElementType.IsAssignableFrom(sourceItemType))
                    {
                        // Asignación directa si los tipos son compatibles
                        destinationItem = item;
                    }
                    else
                    {
                        // Intentar mapeo complejo
                        var itemOptions = configuration.GetMappingOptions(sourceItemType, destElementType);
                        if (itemOptions != null)
                        {
                            destinationItem = MapObject(item, destElementType, itemOptions, configuration);
                        }
                        else
                        {
                            // Buscar si hay opciones para cualquier tipo base
                            destinationItem = TryMapWithBaseTypeOptions(item, destElementType, configuration);
                            
                            if (destinationItem == null)
                            {
                                // Intentar conversión simple
                                try
                                {
                                    destinationItem = Convert.ChangeType(item, destElementType);
                                }
                                catch
                                {
                                    // Si no se puede convertir, crear una nueva instancia y mapear por convención
                                    if (!destElementType.IsValueType && destElementType.GetConstructor(Type.EmptyTypes) != null)
                                    {
                                        var destItem = Activator.CreateInstance(destElementType);
                                        AutoMapByConvention(item, destItem, options.IgnoreNullValues);
                                        destinationItem = destItem;
                                    }
                                    else
                                    {
                                        // Si todo lo demás falla, usar valor predeterminado
                                        destinationItem = destElementType.IsValueType ? 
                                            Activator.CreateInstance(destElementType) : null;
                                    }
                                }
                            }
                        }
                    }
                    
                    items.Add(destinationItem);
                }
                
                // Crear el array final
                Array array = Array.CreateInstance(destElementType, items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    array.SetValue(items[i], i);
                }
                
                return (TDestination)(object)array;
            }
            else
            {
                // Limpiar la colección destino existente si es posible
                var clearMethod = GetClearMethod(destination);
                clearMethod?.Invoke(destination, null);
                
                // Obtener el método Add de la colección destino
                var addMethod = GetAddMethod(destination);
                if (addMethod == null)
                    return destination; // No se puede agregar elementos
                    
                // Buscar opciones específicas para los elementos
                var elementMappingOptions = TryGetElementMappingOptions(sourceElementType, destElementType, configuration);
                
                // Agregar cada elemento
                foreach (var item in sourceCollection)
                {
                    if (item == null && options.IgnoreNullValues)
                        continue;
                        
                    if (item == null)
                    {
                        addMethod.Invoke(destination, new object[] { null });
                        continue;
                    }
                    
                    // Mapear el elemento según su tipo
                    var sourceItemType = item.GetType();
                    object destinationItem;
                    
                    if (destElementType.IsAssignableFrom(sourceItemType))
                    {
                        // Asignación directa si los tipos son compatibles
                        destinationItem = item;
                    }
                    else if (elementMappingOptions != null)
                    {
                        // Usar las opciones para mapear el elemento
                        var destItem = Activator.CreateInstance(destElementType);
                        destinationItem = MapObject(item, destItem, elementMappingOptions, configuration);
                    }
                    else
                    {
                        // Buscar si hay opciones para cualquier tipo base
                        destinationItem = TryMapWithBaseTypeOptions(item, destElementType, configuration);
                        
                        if (destinationItem == null)
                        {
                            // Intentar conversión simple
                            try
                            {
                                destinationItem = Convert.ChangeType(item, destElementType);
                            }
                            catch
                            {
                                // Intentar crear y mapear un objeto complejo
                                if (!destElementType.IsValueType && destElementType.GetConstructor(Type.EmptyTypes) != null)
                                {
                                    var destItem = Activator.CreateInstance(destElementType);
                                    AutoMapByConvention(item, destItem, options.IgnoreNullValues, options.PreserveNestedNullValues);
                                    destinationItem = destItem;
                                }
                                else
                                {
                                    // Si no se puede convertir ni mapear, usar valor predeterminado
                                    destinationItem = destElementType.IsValueType ? 
                                        Activator.CreateInstance(destElementType) : null;
                                }
                            }
                        }
                    }
                    
                    // Agregar a la colección destino
                    addMethod.Invoke(destination, new object[] { destinationItem });
                }
            }
            
            return destination;
        }
        
        /// <summary>
        /// Intenta obtener las opciones de mapeo para los elementos de una colección
        /// </summary>
        private static object TryGetElementMappingOptions(Type sourceElementType, Type destElementType, SimpleAutoMappingConfiguration configuration)
        {
            // Primero intentar obtener opciones directas
            var options = configuration.GetMappingOptions(sourceElementType, destElementType);
            if (options != null)
                return options;
                
            // Si no hay opciones directas, buscar en tipos base
            Type currentSourceType = sourceElementType;
            while (currentSourceType != null && currentSourceType != typeof(object))
            {
                var baseOptions = configuration.GetMappingOptions(currentSourceType, destElementType);
                if (baseOptions != null)
                {
                    // Adaptar opciones para el tipo concreto
                    var adaptedOptionsType = typeof(MappingOptions<,>).MakeGenericType(sourceElementType, destElementType);
                    var adaptedOptions = Activator.CreateInstance(adaptedOptionsType);
                    
                    // Copiar configuraciones básicas
                    CopyBasicMappingOptions(baseOptions, adaptedOptions);
                    
                    return adaptedOptions;
                }
                
                currentSourceType = currentSourceType.BaseType;
            }
            
            return null;
        }
        
        /// <summary>
        /// Intenta mapear un objeto buscando opciones para cualquier tipo base
        /// </summary>
        private static object TryMapWithBaseTypeOptions(object sourceItem, Type destType, SimpleAutoMappingConfiguration configuration)
        {
            var sourceItemType = sourceItem.GetType();
            Type currentSourceType = sourceItemType;
            
            while (currentSourceType != null && currentSourceType != typeof(object))
            {
                var baseOptions = configuration.GetMappingOptions(currentSourceType, destType);
                if (baseOptions != null)
                {
                    // Crear objeto destino
                    var destItem = Activator.CreateInstance(destType);
                    
                    // Adaptar opciones para el tipo concreto si es necesario
                    if (currentSourceType != sourceItemType)
                    {
                        var adaptedOptionsType = typeof(MappingOptions<,>).MakeGenericType(sourceItemType, destType);
                        var adaptedOptions = Activator.CreateInstance(adaptedOptionsType);
                        
                        // Copiar configuraciones básicas
                        CopyBasicMappingOptions(baseOptions, adaptedOptions);
                        
                        // Usar opciones adaptadas
                        return MapObject(sourceItem, destItem, adaptedOptions, configuration);
                    }
                    
                    // Usar opciones encontradas
                    return MapObject(sourceItem, destItem, baseOptions, configuration);
                }
                
                currentSourceType = currentSourceType.BaseType;
            }
            
            return null;
        }

        /// <summary>
        /// Implementación del mapeo usando reflexión (para casos que no se pueden optimizar)
        /// </summary>
        private static TDestination MapInternalWithReflection<TSource, TDestination>(
            TSource source,
            TDestination destination,
            MappingOptions<TSource, TDestination> options,
            SimpleAutoMappingConfiguration configuration)
            where TSource : class
            where TDestination : class
        {
            // Obtener propiedades destino (con caché)
            var destPropsArray = _propertyCache.GetOrAdd(typeof(TDestination), 
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
                var sourceProps = _propertyCache.GetOrAdd(source.GetType(), 
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
                    MapCollectionOptimized(value, destination, destProp, options.IgnoreNullValues, configuration);
                    continue;
                }
                
                // Mapeo de objetos anidados
                if (options.MapNestedObjects && value != null && 
                    !IsPrimitiveType(value.GetType()) && !IsCollection(value.GetType()) && 
                    !IsPrimitiveType(destProp.PropertyType) && !IsCollection(destProp.PropertyType))
                {
                    MapNestedObject(value, destination, destProp, options, configuration);
                    continue;
                }
                
                // Asignar si los tipos son compatibles
                if (destProp.PropertyType.IsAssignableFrom(value?.GetType()))
                {
                    SetPropertyValue(destination, destProp, value);
                }
                else
                {
                    TryConvertAndSetValue(value, destination, destProp, configuration);
                }
            }
            
            return destination;
        }

        /// <summary>
        /// Versión optimizada del mapeo de colecciones
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MapCollectionOptimized(
            object sourceValue,
            object destination,
            PropertyInfo destProp,
            bool ignoreNulls,
            SimpleAutoMappingConfiguration configuration)
        {
            var sourceCollection = sourceValue as IEnumerable;
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
                    return; // No se pudo crear la colección
                }
            }
            
            // Mapear la colección optimizada
            MapCollectionFast(sourceCollection, destCollection, destProp.PropertyType, ignoreNulls, configuration);
        }

        /// <summary>
        /// Implementación rápida para mapeo de colecciones
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MapCollectionFast(
            IEnumerable sourceCollection, 
            object destCollection, 
            Type destCollectionType, 
            bool ignoreNulls,
            SimpleAutoMappingConfiguration configuration)
        {
            if (sourceCollection == null || destCollection == null)
                return;
            
            // Obtener el tipo de elemento de la colección destino
            var destElementType = GetElementType(destCollectionType);
            
            // Limpiar la colección destino existente si es posible
            var clearMethod = GetClearMethod(destCollection);
            clearMethod?.Invoke(destCollection, null);
            
            // Obtener el método Add de la colección destino
            var addMethod = GetAddMethod(destCollection);
            if (addMethod == null)
                return; // No se puede agregar elementos
            
            // Agregar cada elemento con conversión rápida
            foreach (var sourceItem in sourceCollection)
            {
                if (sourceItem == null && ignoreNulls)
                    continue;
                
                if (sourceItem == null)
                {
                    addMethod.Invoke(destCollection, new object[] { null });
                    continue;
                }
                
                // Mapear el elemento según su tipo
                var sourceItemType = sourceItem.GetType();
                object destinationItem;
                
                if (destElementType.IsAssignableFrom(sourceItemType))
                {
                    // Asignación directa si los tipos son compatibles
                    destinationItem = sourceItem;
                }
                else
                {
                    // Verificar si hay mapeo específico para el tipo del elemento
                    var specificOptions = configuration.GetMappingOptions(sourceItemType, destElementType);
                    
                    if (specificOptions != null)
                    {
                        // Crear una instancia del elemento destino
                        var destItem = Activator.CreateInstance(destElementType);
                        // Usar MapObject en lugar de un convertidor específico
                        destinationItem = MapObject(sourceItem, destItem, specificOptions, configuration);
                    }
                    else
                    {
                        // Intentar conversión simple
                        var converter = configuration.GetTypeConverter(sourceItemType, destElementType);
                        if (converter != null)
                        {
                            try
                            {
                                destinationItem = converter(sourceItem);
                            }
                            catch
                            {
                                try
                                {
                                    destinationItem = Convert.ChangeType(sourceItem, destElementType);
                                }
                                catch
                                {
                                    if (!destElementType.IsValueType && destElementType.GetConstructor(Type.EmptyTypes) != null)
                                    {
                                        var destItem = Activator.CreateInstance(destElementType);
                                        AutoMapByConvention(sourceItem, destItem, ignoreNulls);
                                        destinationItem = destItem;
                                    }
                                    else
                                    {
                                        destinationItem = destElementType.IsValueType ? 
                                            Activator.CreateInstance(destElementType) : null;
                                    }
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                destinationItem = Convert.ChangeType(sourceItem, destElementType);
                            }
                            catch
                            {
                                if (!destElementType.IsValueType && destElementType.GetConstructor(Type.EmptyTypes) != null)
                                {
                                    var destItem = Activator.CreateInstance(destElementType);
                                    AutoMapByConvention(sourceItem, destItem, ignoreNulls);
                                    destinationItem = destItem;
                                }
                                else
                                {
                                    destinationItem = destElementType.IsValueType ? 
                                        Activator.CreateInstance(destElementType) : null;
                                }
                            }
                        }
                    }
                }
                
                // Agregar a la colección destino
                addMethod.Invoke(destCollection, new object[] { destinationItem });
            }
        }

        /// <summary>
        /// Versión optimizada del mapeo de objetos anidados
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MapNestedObject(
            object value, 
            object destination, 
            PropertyInfo destProp,
            object options,
            SimpleAutoMappingConfiguration configuration)
        {
            if (value == null)
                return;
            
            var destValue = destProp.GetValue(destination);
            
            // Si el valor destino es null, necesitamos crear una instancia
            if (destValue == null)
            {
                if (destProp.PropertyType.GetConstructor(Type.EmptyTypes) != null)
                {
                    destValue = Activator.CreateInstance(destProp.PropertyType);
                    SetPropertyValue(destination, destProp, destValue);
                }
                else
                {
                    // No podemos crear una instancia del tipo destino - posiblemente un tipo de interface
                    return;
                }
            }
            
            var sourceType = value.GetType();
            var destType = destValue.GetType();
            
            // Buscar opciones específicas para los tipos de los objetos anidados
            var nestedOptions = configuration.GetMappingOptions(sourceType, destType);
            
            if (nestedOptions != null)
            {
                // Copiar configuración de preservar nulos anidados
                CopyNestedNullsConfig(options, nestedOptions);
                
                // Mapear usando las opciones específicas
                MapObject(value, destValue, nestedOptions, configuration);
            }
            else
            {
                // Verificar si hay opciones para cualquier tipo base del tipo fuente
                Type currentSourceBaseType = sourceType.BaseType;
                while (nestedOptions == null && currentSourceBaseType != null && currentSourceBaseType != typeof(object))
                {
                    nestedOptions = configuration.GetMappingOptions(currentSourceBaseType, destType);
                    if (nestedOptions != null)
                    {
                        // Encontramos opciones para un tipo base, adaptarlas para el tipo actual
                        var adaptedOptionsType = typeof(MappingOptions<,>).MakeGenericType(sourceType, destType);
                        var adaptedOptions = Activator.CreateInstance(adaptedOptionsType);
                        
                        // Copiar configuraciones básicas
                        CopyBasicMappingOptions(nestedOptions, adaptedOptions);
                        // Copiar configuración de nulos anidados
                        CopyNestedNullsConfig(options, adaptedOptions);
                        
                        // Mapear usando las opciones adaptadas
                        MapObject(value, destValue, adaptedOptions, configuration);
                        return;
                    }
                    
                    currentSourceBaseType = currentSourceBaseType.BaseType;
                }
                
                // Si no encontramos ninguna opción específica, usar mapeo automático por convención
                var preserveNulls = GetPreserveNestedNullsValue(options);
                AutoMapByConvention(value, destValue, GetIgnoreNullsValue(options), preserveNulls);
            }
        }

        /// <summary>
        /// Obtiene el valor de IgnoreNullValues de opciones usando reflection
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetIgnoreNullsValue(object options)
        {
            var prop = options.GetType().GetProperty("IgnoreNullValues");
            return prop != null && (bool)prop.GetValue(options);
        }

        /// <summary>
        /// Obtiene el valor de PreserveNestedNullValues de opciones usando reflection
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetPreserveNestedNullsValue(object options)
        {
            var prop = options.GetType().GetProperty("PreserveNestedNullValues");
            return prop != null && (bool)prop.GetValue(options);
        }

        /// <summary>
        /// Copia la configuración de PreserveNestedNullValues entre opciones
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyNestedNullsConfig(object sourceOptions, object destOptions)
        {
            var sourceProp = sourceOptions.GetType().GetProperty("PreserveNestedNullValues");
            var destProp = destOptions.GetType().GetProperty("PreserveNestedNullValues");
            
            if (sourceProp != null && destProp != null)
            {
                var preserveNulls = sourceProp.GetValue(sourceOptions);
                destProp.SetValue(destOptions, preserveNulls);
            }
        }

        /// <summary>
        /// Intenta convertir y asignar un valor a una propiedad
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryConvertAndSetValue(
            object value, 
            object destination, 
            PropertyInfo destProp,
            SimpleAutoMappingConfiguration configuration)
        {
            if (value == null)
                return;
                
            // Intentar usar conversor de tipos registrado
            var converter = configuration.GetTypeConverter(value.GetType(), destProp.PropertyType);
            if (converter != null)
            {
                try
                {
                    var convertedValue = converter(value);
                    SetPropertyValue(destination, destProp, convertedValue);
                }
                catch { /* Ignorar errores de conversión */ }
                return;
            }
            
            // Intentar conversión estándar
            try
            {
                var convertedValue = Convert.ChangeType(value, destProp.PropertyType);
                SetPropertyValue(destination, destProp, convertedValue);
            }
            catch { /* Ignorar errores de conversión */ }
        }

        /// <summary>
        /// Verifica si un tipo es primitivo o simple
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPrimitiveType(Type type)
        {
            if (_isPrimitiveTypeCache.TryGetValue(type, out var isPrimitive))
                return isPrimitive;
                
            return _isPrimitiveTypeCache[type] = type.IsPrimitive || 
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
            
            // Manejar tipos derivados: si las opciones son para un tipo base pero estamos mapeando un tipo derivado
            var optionsType = options.GetType();
            if (optionsType.IsGenericType && optionsType.GetGenericTypeDefinition() == typeof(MappingOptions<,>))
            {
                var optionsGenericArgs = optionsType.GetGenericArguments();
                var optionsSourceType = optionsGenericArgs[0];
                var optionsDestType = optionsGenericArgs[1];
                
                // Si el tipo de opciones no coincide exactamente con el tipo de origen real
                if (optionsSourceType != sourceType && optionsSourceType.IsAssignableFrom(sourceType))
                {
                    // Buscar si existe un mapeo específico para el tipo derivado
                    var derivedOptions = configuration.GetMappingOptions(sourceType, destType);
                    if (derivedOptions != null)
                    {
                        options = derivedOptions; // Usar las opciones específicas del tipo derivado
                    }
                    else
                    {
                        // Si no hay opciones específicas, crear una versión adaptada de las opciones existentes
                        // Esto evita el error "Cannot be converted to type..."
                        var adaptedOptionsType = typeof(MappingOptions<,>).MakeGenericType(sourceType, destType);
                        var adaptedOptions = Activator.CreateInstance(adaptedOptionsType);
                        
                        // Copiar propiedades básicas de configuración
                        CopyBasicMappingOptions(options, adaptedOptions);
                        
                        options = adaptedOptions;
                    }
                }
            }
            
            // Invocar MapInternal con tipos correctos por reflexión
            var mapMethod = typeof(Mapper).GetMethod("MapInternal", 
                BindingFlags.NonPublic | BindingFlags.Static);
                
            var genericMethod = mapMethod.MakeGenericMethod(sourceType, destType);
            return genericMethod.Invoke(null, new[] { source, destination, options, configuration });
        }
        
        /// <summary>
        /// Copia las propiedades básicas de configuración entre dos objetos MappingOptions de tipos diferentes
        /// </summary>
        private static void CopyBasicMappingOptions(object source, object destination)
        {
            if (source == null || destination == null)
                return;
                
            // Copiar configuración de IgnoreNullValues
            var sourceIgnoreNullsProp = source.GetType().GetProperty("IgnoreNullValues");
            var destIgnoreNullsProp = destination.GetType().GetProperty("IgnoreNullValues");
            if (sourceIgnoreNullsProp != null && destIgnoreNullsProp != null)
            {
                var ignoreNulls = sourceIgnoreNullsProp.GetValue(source);
                destIgnoreNullsProp.SetValue(destination, ignoreNulls);
            }
            
            // Copiar configuración de MapNestedObjects
            var sourceMapNestedProp = source.GetType().GetProperty("MapNestedObjects");
            var destMapNestedProp = destination.GetType().GetProperty("MapNestedObjects");
            if (sourceMapNestedProp != null && destMapNestedProp != null)
            {
                var mapNested = sourceMapNestedProp.GetValue(source);
                destMapNestedProp.SetValue(destination, mapNested);
            }
            
            // Copiar configuración de PreserveNestedNullValues
            var sourcePreserveNestedProp = source.GetType().GetProperty("PreserveNestedNullValues");
            var destPreserveNestedProp = destination.GetType().GetProperty("PreserveNestedNullValues");
            if (sourcePreserveNestedProp != null && destPreserveNestedProp != null)
            {
                var preserveNested = sourcePreserveNestedProp.GetValue(source);
                destPreserveNestedProp.SetValue(destination, preserveNested);
            }
            
            // No copiamos PropertyMappings, ValueTransformers, etc. ya que esas configuraciones son específicas del tipo
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
            
            // Verificar si podemos usar un delegado compilado
            if (!ignoreNulls && !preserveNestedNulls)
            {
                var key = (sourceType, destType);
                if (_mappingDelegateCache.TryGetValue(key, out var mappingDelegate))
                {
                    // Ejecutar el delegado compilado
                    ((Action<object, object>)mappingDelegate)(source, destination);
                    return;
                }
                
                // Compilar un nuevo delegado para este par de tipos
                var conversionDelegate = CompileAutoMapDelegate(sourceType, destType);
                _mappingDelegateCache[key] = conversionDelegate;
                conversionDelegate(source, destination);
                return;
            }
            
            // Si no podemos usar un delegado, usar el enfoque basado en reflexión
            // Obtener propiedades fuente y destino
            var sourceProps = _propertyCache.GetOrAdd(sourceType, t => t.GetProperties().Where(p => p.CanRead).ToArray());
            var destProps = _propertyCache.GetOrAdd(destType, t => t.GetProperties().Where(p => p.CanWrite).ToArray());
            
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
        /// Compila un delegado optimizado para mapeo automático por convención
        /// </summary>
        private static Action<object, object> CompileAutoMapDelegate(Type sourceType, Type destType)
        {
            var sourceParam = Expression.Parameter(typeof(object), "source");
            var destParam = Expression.Parameter(typeof(object), "dest");
            
            var typedSourceParam = Expression.Convert(sourceParam, sourceType);
            var typedDestParam = Expression.Convert(destParam, destType);
            
            var expressions = new List<Expression>();
            
            // Obtener propiedades fuente y destino
            var sourceProps = _propertyCache.GetOrAdd(sourceType, t => t.GetProperties().Where(p => p.CanRead).ToArray());
            var destProps = _propertyCache.GetOrAdd(destType, t => t.GetProperties().Where(p => p.CanWrite).ToArray());
            
            // Mapear propiedades por nombre
            foreach (var sourceProp in sourceProps)
            {
                // Buscar propiedad destino con el mismo nombre
                var destProp = destProps.FirstOrDefault(
                    p => p.Name.Equals(sourceProp.Name, StringComparison.OrdinalIgnoreCase));
                    
                if (destProp == null)
                    continue;
                    
                // Si los tipos son compatibles directamente
                if (destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                {
                    // source.Property
                    var sourceAccess = Expression.Property(typedSourceParam, sourceProp);
                    // dest.Property = source.Property
                    var assignment = Expression.Assign(
                        Expression.Property(typedDestParam, destProp),
                        sourceAccess);
                    expressions.Add(assignment);
                }
                else
                {
                    // Intentar conversión si es simple
                    try
                    {
                        // source.Property
                        var sourceAccess = Expression.Property(typedSourceParam, sourceProp);
                        // (DestPropType)source.Property
                        var converted = Expression.Convert(sourceAccess, destProp.PropertyType);
                        // dest.Property = (DestPropType)source.Property
                        var assignment = Expression.Assign(
                            Expression.Property(typedDestParam, destProp),
                            converted);
                        expressions.Add(assignment);
                    }
                    catch { /* Ignorar errores en la generación de expresiones */ }
                }
            }
            
            // Si no hay expresiones, devolver un delegado vacío
            if (expressions.Count == 0)
            {
                return (_, __) => { };
            }
            
            // Crear bloque con todas las expresiones
            var block = Expression.Block(expressions);
            
            // Compilar lambda
            return Expression.Lambda<Action<object, object>>(block, sourceParam, destParam).Compile();
        }
        
        /// <summary>
        /// Obtiene un getter optimizado para una propiedad
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<object, object> GetOrCreateGetter(PropertyInfo property)
        {
            if (_getterCache.TryGetValue(property, out var getter))
                return getter;
                
            return _getterCache.GetOrAdd(property, prop => 
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetPropertyValue(object target, PropertyInfo property, object value)
        {
            if (_setterCache.TryGetValue(property, out var setter))
            {
                setter(target, value);
                return;
            }
            
            setter = _setterCache.GetOrAdd(property, prop => 
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollection(Type type)
        {
            if (_isCollectionTypeCache.TryGetValue(type, out var isCollection))
                return isCollection;
                
            if (type == typeof(string))
                return _isCollectionTypeCache[type] = false;
                
            return _isCollectionTypeCache[type] = typeof(IEnumerable).IsAssignableFrom(type);
        }
        
        /// <summary>
        /// Obtiene el tipo de elemento de una colección
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetElementType(Type collectionType)
        {
            if (_elementTypeCache.TryGetValue(collectionType, out var elementType))
                return elementType;
                
            if (collectionType.IsArray)
                return _elementTypeCache[collectionType] = collectionType.GetElementType();
                
            // Buscar implementaciones genéricas de IEnumerable<T>
            foreach (var iface in collectionType.GetInterfaces())
            {
                if (iface.IsGenericType && 
                    iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return _elementTypeCache[collectionType] = iface.GetGenericArguments()[0];
                }
            }
            
            // Verificar si la clase implementa IEnumerable<T> directamente
            if (collectionType.IsGenericType && 
                collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return _elementTypeCache[collectionType] = collectionType.GetGenericArguments()[0];
            }
            
            // Si no se encuentra un tipo genérico, asumir Object
            return _elementTypeCache[collectionType] = typeof(object);
        }
        
        /// <summary>
        /// Crea una instancia de colección del tipo especificado
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            // Verificación de seguridad para que no se produzcan errores si se llama múltiples veces
            if (_propertyCache != null)
                _propertyCache.Clear();
                
            if (_getterCache != null)
                _getterCache.Clear();
                
            if (_setterCache != null)
                _setterCache.Clear();
                
            if (_isPrimitiveTypeCache != null)
                _isPrimitiveTypeCache.Clear();
                
            if (_isCollectionTypeCache != null)
                _isCollectionTypeCache.Clear();
                
            if (_elementTypeCache != null)
                _elementTypeCache.Clear();
                
            if (_isAssignableCache != null)
                _isAssignableCache.Clear();
                
            if (_mappingDelegateCache != null)
                _mappingDelegateCache.Clear();
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