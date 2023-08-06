using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static System.Linq.Expressions.Expression;

namespace BuildTools
{
    public class ServiceProvider : IServiceProvider
    {
        private static MethodInfo getServiceMethod;

        private readonly Dictionary<Type, ServiceDescriptor> services = new Dictionary<Type, ServiceDescriptor>();

        static ServiceProvider()
        {
            getServiceMethod = typeof(ServiceProviderExtensions).GetMethod(nameof(ServiceProviderExtensions.GetService));

            if (getServiceMethod == null)
                throw new InvalidOperationException($"Failed to find method {nameof(ServiceProviderExtensions)}.{nameof(ServiceProviderExtensions.GetService)}");
        }

        internal ServiceProvider(ServiceDescriptor[] serviceDescriptors)
        {
            foreach (var service in serviceDescriptors)
                services[service.ServiceType] = service;

            AddSingleton<IServiceProvider>(this);
        }

        public void AddSingleton<TService>() => AddSingleton<TService, TService>();

        public void AddSingleton<TService, TImplementation>() => AddSingleton(typeof(TService), typeof(TImplementation));

        public void AddSingleton<TService>(TService implementation)
        {
            if (implementation == null)
                throw new ArgumentNullException(nameof(implementation));

            services[typeof(TService)] = new ServiceDescriptor(typeof(TService), implementation.GetType(), implementation: implementation);
        }

        public void AddSingleton(Type serviceType, Type implementationType)
        {
            if (implementationType.IsInterface)
                throw new ArgumentException($"Cannot create service using implementation type '{implementationType.Name}': type is an interface.", nameof(implementationType));

            if (services.TryGetValue(serviceType, out _))
                throw new InvalidOperationException($"Cannot create service '{serviceType.Name}': service has already been added to the {nameof(ServiceCollection)}.");

            services[serviceType] = new ServiceDescriptor(serviceType, implementationType);
        }

        public object GetService(Type serviceType) => GetServiceInternal(serviceType, new Stack<ServiceDescriptor>());

        private object GetServiceInternal(Type serviceType, Stack<ServiceDescriptor> resolutionScope)
        {
            if (!services.TryGetValue(serviceType, out var descriptor))
            {
                if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(Lazy<>))
                {
                    var factory = MakeLazyFactory(serviceType);

                    descriptor = new ServiceDescriptor(serviceType, serviceType, factory: factory);

                    services[serviceType] = descriptor;
                }
                else
                    throw new InvalidOperationException($"Cannot retrieve service '{serviceType.Name}': service has not been registered with the service provider.");
            }

            if (descriptor.Value != null)
                return descriptor.Value;

            if (resolutionScope.Contains(descriptor))
            {
                var str = string.Join(" -> ", resolutionScope.Reverse().Select(r => r.ImplementationType.Name));

                throw new InvalidOperationException($"Cannot resolve service '{serviceType.Name}': a recursive reference was found in hierarchy {str} -> {descriptor.ImplementationType.Name}.");
            }

            resolutionScope.Push(descriptor);

            try
            {
                if (descriptor.Factory != null)
                    descriptor.Value = descriptor.Factory(this);
                else
                {
                    if (descriptor.Value == null)
                        descriptor.Value = ResolveService(descriptor.ImplementationType, resolutionScope);
                }

                return descriptor.Value;
            }
            finally
            {
                resolutionScope.Pop();
            }
        }

        private Func<IServiceProvider, object> MakeLazyFactory(Type lazyType)
        {
            var realServiceType = lazyType.GetGenericArguments()[0];

            var funcType = typeof(Func<>).MakeGenericType(realServiceType);

            var realGetServiceMethod = getServiceMethod.MakeGenericMethod(realServiceType);

            var providerParameter = Parameter(typeof(IServiceProvider), "provider");
            var lazyCtor = lazyType.GetConstructor(new[] {funcType});

            if (lazyCtor == null)
                throw new InvalidOperationException($"Failed to find required constructor on type {lazyType.Name}");

            var call = Call(realGetServiceMethod, providerParameter);
            var innerLambda = Lambda(call);

            var newLazy = New(lazyCtor, innerLambda);

            var outerLambda = Lambda(newLazy, providerParameter);

            var func = outerLambda.Compile();

            return (Func<IServiceProvider, object>) func;
        }

        private object ResolveService(Type type, Stack<ServiceDescriptor> resolutionScope)
        {
            try
            {
                var ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (ctors.Length > 1)
                    throw new InvalidOperationException($"Cannot resolve service '{type.Name}': more than one constructor was found.");

                if (ctors.Length == 0)
                    return Activator.CreateInstance(type);

                var ctor = ctors.Single();

                var parameters = ctor.GetParameters().Select(p => GetServiceInternal(p.ParameterType, resolutionScope)).ToArray();

                return ctor.Invoke(parameters);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                //This should be unreachable
                throw ex.InnerException;
            }
        }
    }
}
