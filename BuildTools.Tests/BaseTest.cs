using System;
using System.Linq;
using System.Reflection;

namespace BuildTools.Tests
{
    public abstract class BaseTest
    {
        protected void Test<TService>(Action<TService> action) => Test((Delegate) action);

        protected void Test<T1, T2>(Action<T1, T2> action) => Test((Delegate)action);

        protected void Test<T1, T2, T3>(Action<T1, T2, T3> action) => Test((Delegate)action);

        protected void Test<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action) => Test((Delegate)action);

        protected void Test(Delegate action)
        {
            CreateServices(out var serviceCollection);

            var serviceProvider = serviceCollection.Build();

            var parametersTypes = action.Method.GetParameters();

            var parameters = parametersTypes.Select(p => serviceProvider.GetService(p.ParameterType)).ToArray();

            try
            {
                action.DynamicInvoke(parameters);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        protected abstract void CreateServices(out ServiceCollection serviceCollection);
    }
}