using System;
using System.Linq;

namespace BuildTools
{
    abstract class SupportedLangTypeValidator<TEnvironment> : IValidateSetValuesGenerator
    {
        private Func<ProjectConfig, string[]> getItems;

        protected SupportedLangTypeValidator(Func<ProjectConfig, string[]> getItems)
        {
            this.getItems = getItems;
        }

        public string[] GetValidValues()
        {
            var provider = BuildToolsSessionState.ServiceProvider<TEnvironment>().GetService<IProjectConfigProvider>();

            var items = getItems(provider.Config);

            return items.Select(v => v.ToString()).ToArray();
        }
    }

    class SupportedPackageTypeValidator<TEnvironment> : SupportedLangTypeValidator<TEnvironment>
    {
        public SupportedPackageTypeValidator() : base(c => c.PackageTypes.Select(t => t.GetDescription(false)).ToArray())
        {
        }
    }

    class SupportedTestTypeValidator<TEnvironment> : SupportedLangTypeValidator<TEnvironment>
    {
        public SupportedTestTypeValidator() : base(c => c.TestTypes.Select(t => t.GetDescription(false)).ToArray())
        {
        }
    }
}