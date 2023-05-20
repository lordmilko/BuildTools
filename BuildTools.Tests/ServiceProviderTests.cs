using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
// ReSharper disable ObjectCreationAsStatement

namespace BuildTools.Tests
{
    interface IIFace
    {
    }

    class IImpl : IIFace
    {
    }

    class ServiceWithCtor
    {
        public IIFace Value { get; }

        public ServiceWithCtor(IIFace iface)
        {
            Value = iface;
        }
    }

    class ServiceWithRecursive
    {
        public ServiceWithRecursive(ServiceWithRecursive service)
        {
        }
    }

    [TestClass]
    public class ServiceProviderTests
    {
        #region ServiceCollection

        [TestMethod]
        public void ServiceCollection_InterfaceImplementation_Throws()
        {
            AssertEx.Throws<ArgumentException>(
                () => new ServiceCollection { {typeof(IIFace)} },
                "Cannot create service using implementation type 'IIFace': type is an interface."
            );
        }

        [TestMethod]
        public void ServiceCollection_ExistingService_Throws()
        {
            var services = new ServiceCollection
            {
                {typeof(IIFace), typeof(IImpl)}
            };

            AssertEx.Throws<InvalidOperationException>(
                () => services.Add(typeof(IIFace), typeof(IImpl)),
                "Cannot create service 'IIFace': service has already been added to the ServiceCollection."
            );
        }

        #endregion
        #region ServiceProvider

        [TestMethod]
        public void ServiceProvider_GetService_NoCtor()
        {
            var serviceProvider = new ServiceCollection
            {
                {typeof(IIFace), typeof(IImpl)}
            }.Build();

            var service = serviceProvider.GetService<IIFace>();

            Assert.IsNotNull(service);
        }

        [TestMethod]
        public void ServiceProvider_GetService_CtorWithKnownService()
        {
            var serviceProvider = new ServiceCollection
            {
                {typeof(ServiceWithCtor)},
                {typeof(IIFace), typeof(IImpl)}
            }.Build();

            var service = serviceProvider.GetService<ServiceWithCtor>();

            Assert.IsNotNull(service);
            Assert.IsNotNull(service.Value);
        }

        [TestMethod]
        public void ServiceProvider_GetService_CtorWithUnknownService_Throws()
        {
            var serviceProvider = new ServiceCollection
            {
                {typeof(ServiceWithCtor)}
            }.Build();

            AssertEx.Throws<InvalidOperationException>(
                () => serviceProvider.GetService<ServiceWithCtor>(),
                "Cannot retrieve service 'IIFace': service has not been registered with the service provider."
            );
        }

        [TestMethod]
        public void ServiceProvider_GetService_NotExists_Throws()
        {
            var serviceProvider = new ServiceCollection().Build();

            AssertEx.Throws<InvalidOperationException>(
                () => serviceProvider.GetService<IIFace>(),
                "Cannot retrieve service 'IIFace': service has not been registered with the service provider."
            );
        }

        [TestMethod]
        public void ServiceProvider_GetService_Recursive_Throws()
        {
            var serviceProvider = new ServiceCollection
            {
                {typeof(ServiceWithRecursive)}
            }.Build();

            AssertEx.Throws<InvalidOperationException>(
                () => serviceProvider.GetService<ServiceWithRecursive>(),
                "Cannot resolve service 'ServiceWithRecursive': a recursive reference was found in hierarchy ServiceWithRecursive"
            );
        }

        [TestMethod]
        public void ServiceProvider_GetService_ExplicitLazy()
        {
            var serviceProvider = new ServiceCollection
            {
                {typeof(IIFace), typeof(IImpl)},
                provider => new Lazy<IIFace>(provider.GetService<IIFace>)
            }.Build();

            var lazyValue = serviceProvider.GetService<Lazy<IIFace>>();
            var realValue = lazyValue.Value;

            Assert.IsNotNull(realValue);
        }

        [TestMethod]
        public void ServiceProvider_GetService_DynamicLazy()
        {
            var serviceProvider = new ServiceCollection
            {
                {typeof(IIFace), typeof(IImpl)}
            }.Build();

            var lazyValue1 = serviceProvider.GetService<Lazy<IIFace>>();
            var lazyValue2 = serviceProvider.GetService<Lazy<IIFace>>();
            var realValue = lazyValue1.Value;

            Assert.IsTrue(ReferenceEquals(lazyValue1, lazyValue2));
            Assert.IsNotNull(realValue);
        }

        #endregion
    }
}
