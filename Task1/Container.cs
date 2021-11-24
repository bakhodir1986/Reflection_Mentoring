using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Task1.DoNotChange;
using System.Reflection.Emit;

namespace Task1
{
    public interface IActivator
    {
        object CreateInstance(Type type, params object[] parameters);
    }

    public class EmitActivator : IActivator
    {
        public object CreateInstance(Type type, params object[] parameters)
        {
            Type[] parametersTypes = parameters.Select(p => p.GetType()).ToArray();
            DynamicMethod createMethod = new DynamicMethod(
                string.Empty, type, parametersTypes);
            ILGenerator il = createMethod.GetILGenerator();
            for (int i = 0; i < parameters.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }

            ConstructorInfo ctor = type.GetConstructor(parametersTypes);
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            return createMethod.Invoke(null, parameters);
        }
    }
    public class Container
    {
        private readonly IDictionary<Type, Type> _typesDictionary;
        private readonly IActivator _activator;

        public Container()
        {
            _activator = new EmitActivator();
            _typesDictionary = new Dictionary<Type, Type>();
        }

        public void AddAssembly(Assembly assembly)
        {
            var types = assembly.ExportedTypes;
            foreach (var type in types)
            {
                var constructorImportAttribute = type.GetCustomAttribute<ImportConstructorAttribute>();
                if (constructorImportAttribute != null || HasImportProperties(type))
                {
                    _typesDictionary.Add(type, type);
                }

                var exportAttributes = type.GetCustomAttributes<ExportAttribute>();
                foreach (var exportAttribute in exportAttributes)
                {
                    _typesDictionary.Add(exportAttribute.Contract ?? type, type);
                }
            }
        }

        public void AddType(Type type)
        {
            _typesDictionary.Add(type, type);
        }

        public void AddType(Type type, Type baseType)
        {
            _typesDictionary.Add(baseType, type);
        }

        public T Get<T>()
        {
            var type = typeof(T);
            var instance = (T)ConstructInstanceOfType(type);
            return instance;
        }

        private object ConstructInstanceOfType(Type type)
        {
            if (!_typesDictionary.ContainsKey(type))
            {
                throw new DIException($"Cannot create instance of {type.FullName}. Dependency is not provided");
            }

            Type dependendType = _typesDictionary[type];
            ConstructorInfo constructorInfo = GetConstructor(dependendType);
            object instance = CreateFromConstructor(dependendType, constructorInfo);

            if (dependendType.GetCustomAttribute<ImportConstructorAttribute>() != null)
            {
                return instance;
            }

            ResolveProperties(dependendType, instance);
            return instance;
        }

        private bool HasImportProperties(Type type)
        {
            var propertiesInfo = GetPropertiesRequiedImport(type);
            return propertiesInfo.Any();
        }

        private IEnumerable<PropertyInfo> GetPropertiesRequiedImport(Type type)
        {
            return type.GetProperties().Where(p => p.GetCustomAttribute<ImportAttribute>() != null);
        }

        private void ResolveProperties(Type type, object instance)
        {
            var propertiesInfo = GetPropertiesRequiedImport(type);
            foreach (var property in propertiesInfo)
            {
                var resolvedProperty = ConstructInstanceOfType(property.PropertyType);
                property.SetValue(instance, resolvedProperty);
            }
        }

        private ConstructorInfo GetConstructor(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors();

            if (constructors.Length == 0)
            {
                throw new DIException($"There are no public constructors for type {type.FullName}");
            }

            return constructors.First();
        }

        private object CreateFromConstructor(Type type, ConstructorInfo constructorInfo)
        {
            ParameterInfo[] parameters = constructorInfo.GetParameters();
            List<object> parametersInstances = new List<object>(parameters.Length);
            Array.ForEach(parameters, p => parametersInstances.Add(ConstructInstanceOfType(p.ParameterType)));

            object instance = _activator.CreateInstance(type, parametersInstances.ToArray());
            return instance;
        }
    }
}