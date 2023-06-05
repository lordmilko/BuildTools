using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace BuildTools
{
    abstract class SupportedLangTypeCompleter<TEnvironment> : IArgumentCompleter
    {
        private Func<ProjectConfig, LangType[]> getItems;

        protected SupportedLangTypeCompleter(Func<ProjectConfig, LangType[]> getItems)
        {
            this.getItems = getItems;
        }

        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var provider = BuildToolsSessionState.ServiceProvider<TEnvironment>().GetService<IProjectConfigProvider>();

            var items = getItems(provider.Config);

            var values = items.Select(v => v.ToString()).ToArray();

            return values
                .Where(v => v.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                .Select(m => new CompletionResult(m, m, CompletionResultType.ParameterValue, m))
                .ToArray();
        }
    }

    class SupportedPackageTypeCompleter<TEnvironment> : SupportedLangTypeValidator<TEnvironment>
    {
        public SupportedPackageTypeCompleter() : base(c => c.PackageTypes)
        {
        }
    }

    class SupportedTestTypeCompleter<TEnvironment> : SupportedLangTypeValidator<TEnvironment>
    {
        public SupportedTestTypeCompleter() : base(c => c.TestTypes)
        {
        }
    }
}