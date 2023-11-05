using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace BuildTools
{
    class NegatedEnumValueCompleter<T> : IArgumentCompleter where T : Enum
    {
        private string[] items;

        public NegatedEnumValueCompleter()
        {
            items = NegatedEnumValueValidator<T>.Instance.GetValidValues();
        }

        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            return items
                .Where(v => v.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                .Select(m => new CompletionResult(m, m, CompletionResultType.ParameterValue, m))
                .ToArray();
        }
    }
}
