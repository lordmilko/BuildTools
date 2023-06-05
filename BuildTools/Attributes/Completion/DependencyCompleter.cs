using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace BuildTools
{
    //CompletionCompleters.ProcessParameter() special cases ValidateSetAttribute and uses the valid values for completion purposes.
    //As we have our own custom validate set attribute, it won't be used for completion and we need to provide our own argument completer
    class DependencyCompleter<TEnvironment> : IArgumentCompleter
    {
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            var provider = BuildToolsSessionState.ServiceProvider<TEnvironment>().GetService<DependencyProvider>();

            var values = provider.GetDependencies().Select(d => d.DisplayName ?? d.Name).ToArray();

            var matches = values.Where(v => v.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase)).ToArray();

            return matches.Select(m => new CompletionResult(m, m, CompletionResultType.ParameterValue, m));
        }
    }
}