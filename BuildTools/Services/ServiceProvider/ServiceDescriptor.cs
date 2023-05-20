using System;

namespace BuildTools
{
    public class ServiceDescriptor
    {
        public Type ServiceType { get; }

        public Type ImplementationType { get; }

        internal object Value { get; set; }

        internal Func<IServiceProvider, object> Factory { get; }

        internal ServiceDescriptor(Type serviceType, Type implementationType, Func<IServiceProvider, object> factory = null)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Factory = factory;
        }
    }
}