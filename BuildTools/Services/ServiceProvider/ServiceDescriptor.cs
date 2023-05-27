using System;

namespace BuildTools
{
    public class ServiceDescriptor
    {
        public Type ServiceType { get; }

        public Type ImplementationType { get; }

        internal object Value { get; set; }

        internal Func<IServiceProvider, object> Factory { get; }

        internal ServiceDescriptor(Type serviceType, Type implementationType, object implementation = null, Func<IServiceProvider, object> factory = null)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Value = implementation;
            Factory = factory;
        }

        public override string ToString()
        {
            if (ServiceType == ImplementationType)
                return ServiceType.Name;

            return $"{ServiceType.Name} ({ImplementationType.Name})";
        }
    }
}