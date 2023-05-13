using System;
using System.Collections;
using System.Collections.Generic;

namespace BuildTools
{
    public class ServiceCollection : IEnumerable<ServiceDescriptor>
    {
        private readonly Dictionary<Type, ServiceDescriptor> services = new Dictionary<Type, ServiceDescriptor>();

        public void AddSingleton(Type serviceType, Type implementationType)
        {
            if (implementationType.IsInterface)
                throw new ArgumentException($"Cannot create service using implementation type '{implementationType.Name}': type is an interface.", nameof(implementationType));

            if (services.TryGetValue(serviceType, out _))
                throw new InvalidOperationException($"Cannot create service '{serviceType.Name}': service has already been added to the {nameof(ServiceCollection)}.");

            services[serviceType] = new ServiceDescriptor(serviceType, implementationType);
        }

        public void Add(Type serviceType) => AddSingleton(serviceType, serviceType);

        public void Add(Type serviceType, Type implementationType) => AddSingleton(serviceType, implementationType);

        public IServiceProvider Build()
        {
            var serviceProvider = new ServiceProvider();

            foreach (var service in services)
                serviceProvider.AddSingleton(service.Value.ServiceType, service.Value.ImplementationType);

            return serviceProvider;
        }

        public IEnumerator<ServiceDescriptor> GetEnumerator() => services.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}