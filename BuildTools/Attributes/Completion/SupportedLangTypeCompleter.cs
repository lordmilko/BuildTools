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
        private Func<ProjectConfig, string[]> getItems;

        protected SupportedLangTypeCompleter(Func<ProjectConfig, string[]> getItems)
        {
            this.getItems = getItems;
        }

        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var provider = BuildToolsSessionState.ServiceProvider<TEnvironment>().GetService<IProjectConfigProvider>();

            var items = getItems(provider.Config);

            return items
                .Where(v => v.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                .Select(m => new CompletionResult(m, m, CompletionResultType.ParameterValue, m))
                .ToArray();
        }
    }

    class SupportedPackageTypeCompleter<TEnvironment> : SupportedLangTypeCompleter<TEnvironment>
    {
        public SupportedPackageTypeCompleter() : base(c => c.PackageTypes.Select(t => t.GetDescription(false)).ToArray())
        {
        }
    }

    class SupportedTestTypeCompleter<TEnvironment> : SupportedLangTypeCompleter<TEnvironment>
    {
        public SupportedTestTypeCompleter() : base(c => c.TestTypes.Select(t => t.GetDescription(false)).ToArray())
        {
        }
    }
}