using System;
using System.Linq;

namespace BuildTools
{
    abstract class SupportedLangTypeValidator<TEnvironment> : IValidateSetValuesGenerator
    {
        private Func<ProjectConfig, LangType[]> getItems;

        protected SupportedLangTypeValidator(Func<ProjectConfig, LangType[]> getItems)
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
        public SupportedPackageTypeValidator() : base(c => c.PackageTypes)
        {
        }
    }

    class SupportedTestTypeValidator<TEnvironment> : SupportedLangTypeValidator<TEnvironment>
    {
        public SupportedTestTypeValidator() : base(c => c.TestTypes)
        {
        }
    }
}