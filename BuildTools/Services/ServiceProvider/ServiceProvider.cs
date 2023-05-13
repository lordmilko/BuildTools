using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildTools
{
    public class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, ServiceDescriptor> services = new Dictionary<Type, ServiceDescriptor>();

        public void AddSingleton<TService>() => AddSingleton<TService, TService>();

        public void AddSingleton<TService, TImplementation>() => AddSingleton(typeof(TService), typeof(TImplementation));

        public void AddSingleton(Type serviceType, Type implementationType)
        {
            if (implementationType.IsInterface)
                throw new ArgumentException($"Cannot create service using implementation type '{implementationType.Name}': type is an interface.", nameof(implementationType));

            if (services.TryGetValue(serviceType, out _))
                throw new InvalidOperationException($"Cannot create service '{serviceType.Name}': service has already been added to the {nameof(ServiceCollection)}.");

            services[serviceType] = new ServiceDescriptor(serviceType, implementationType);
        }

        public object GetService(Type serviceType) => GetServiceInternal(serviceType, new Stack<Type>());

        private object GetServiceInternal(Type serviceType, Stack<Type> resolutionScope)
        {
            if (!services.TryGetValue(serviceType, out var descriptor))
                throw new InvalidOperationException($"Cannot retrieve service '{serviceType.Name}': service has not been registered with the service provider.");

            if (descriptor.Value != null)
                return descriptor.Value;

            if (resolutionScope.Contains(serviceType))
            {
                var str = string.Join(" -> ", resolutionScope.Select(r => r.Name));

                throw new InvalidOperationException($"Cannot resolve service '{serviceType.Name}': a recursive reference was found in hierarchy {str}.");
            }

            resolutionScope.Push(serviceType);

            try
            {
                descriptor.Value = ResolveService(descriptor.ImplementationType, resolutionScope);

                return descriptor.Value;
            }
            finally
            {
                resolutionScope.Pop();
            }
        }

        private object ResolveService(Type type, Stack<Type> resolutionScope)
        {
            var ctors = type.GetConstructors();

            if (ctors.Length > 1)
                throw new InvalidOperationException($"Cannot resolve service '{type.Name}': more than one constructor was found.");

            if (ctors.Length == 0)
                return Activator.CreateInstance(type);

            var ctor = ctors.Single();

            var parameters = ctor.GetParameters().Select(p => GetServiceInternal(p.ParameterType, resolutionScope)).ToArray();

            return ctor.Invoke(parameters);
        }
    }
}
