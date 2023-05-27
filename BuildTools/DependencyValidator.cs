using System.Linq;

namespace BuildTools
{
    class DependencyValidator<TEnvironment> : IValidateSetValuesGenerator
    {
        public string[] GetValidValues()
        {
            var provider = BuildToolsSessionState.ServiceProvider<TEnvironment>().GetService<DependencyProvider>();

            var values = provider.GetDependencies().Select(d => d.DisplayName ?? d.Name).ToArray();

            return values;
        }
    }
}