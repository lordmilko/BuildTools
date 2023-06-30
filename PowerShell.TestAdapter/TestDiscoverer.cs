using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace PowerShell.TestAdapter
{
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
    public class TestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            GetTests(sources, discoverySink, logger);
        }

        public static List<TestCase> GetTests(IEnumerable<string> sources, ITestCaseDiscoverySink discoverySink, IMessageLogger logger = null)
        {
            var tests = new List<TestCase>();

            foreach (var source in sources)
                DiscoverPesterTests(discoverySink, logger, source, tests);

            return tests;
        }

        private static void DiscoverPesterTests(ITestCaseDiscoverySink discoverySink, IMessageLogger logger, string source,
            List<TestCase> tests)
        {
            Log(TestMessageLevel.Informational, $"Searching for tests in {source}" , logger);

            ParseError[] errors;

            var script = Parser.ParseFile(source, out _, out errors);

            if (errors.Length > 0)
            {
                foreach (var error in errors)
                    Log(TestMessageLevel.Error, $"Parser error. {error.Message}", logger);

                return;
            }

            var describeBlocks = script.FindAll(
                m => (m is CommandAst ast) && string.Equals("Describe", ast.GetCommandName(), StringComparison.OrdinalIgnoreCase), 
                true
            );

            foreach (var describeBlock in describeBlocks)
            {
                var functionName = GetFunctionName(logger, describeBlock, "describe");

                var describeTags = GetDescribeTags(logger, describeBlock).ToArray();

                var itBlocks = describeBlock.FindAll(
                    m => m is CommandAst ast && ast.GetCommandName() != null && ast.GetCommandName().Equals("it", StringComparison.OrdinalIgnoreCase),
                    true
                ).Cast<CommandAst>();

                foreach (var itBlock in itBlocks)
                {
                    var itBlockName = GetFunctionName(logger, itBlock, "it");
                    var parentContextName = GetParentContextName(logger, itBlock);

                    if (string.IsNullOrEmpty(itBlockName))
                    {
                        Log(TestMessageLevel.Informational, "Test name was empty. Skipping test.", logger);
                    }
                    else
                    {
                        var testCase = new TestCase($"{functionName}.{parentContextName}.{itBlockName}", TestExecutor.ExecutorUri, source)
                        {
                            DisplayName = itBlockName,
                            CodeFilePath = source,
                            LineNumber = itBlock.Extent.StartLineNumber
                        };

                        foreach (var text in describeTags)
                            testCase.Traits.Add(text, string.Empty);

                        Log(TestMessageLevel.Informational, $"Adding test [{functionName}] in {source} at {testCase.LineNumber}.", logger);

                        discoverySink?.SendTestCase(testCase);

                        tests.Add(testCase);
                    }
                }
            }
        }

        private static string GetParentContextName(IMessageLogger logger, Ast ast)
        {
            if (ast.Parent is CommandAst commandAst && string.Equals("context", commandAst.GetCommandName(), StringComparison.OrdinalIgnoreCase))
                return GetFunctionName(logger, commandAst, "context");

            if (ast.Parent != null)
                return GetParentContextName(logger, ast.Parent);

            return "No Context";
        }

        private static string GetFunctionName(IMessageLogger logger, Ast context, string functionName)
        {
            var contextAst = (CommandAst)context;
            var contextName = string.Empty;
            var nextElementIsName1 = false;

            foreach (var element in contextAst.CommandElements)
            {
                if (element is StringConstantExpressionAst expressionAst && !expressionAst.Value.Equals(functionName, StringComparison.OrdinalIgnoreCase))
                {
                    contextName = expressionAst.Value;
                    break;
                }

                if (nextElementIsName1 && element is StringConstantExpressionAst)
                {
                    contextName = (element as StringConstantExpressionAst).Value;
                    break;
                }

                if (element is CommandParameterAst ast && ast.ParameterName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    nextElementIsName1 = true;
            }

            return contextName;
        }

        private static IEnumerable<string> GetDescribeTags(IMessageLogger logger, Ast context)
        {
            var contextAst = (CommandAst)context;
            var nextElementIsName1 = false;

            foreach (var element in contextAst.CommandElements)
            {
                if (nextElementIsName1)
                {
                    var tagStrings = element.FindAll(m => m is StringConstantExpressionAst, true).Cast<StringConstantExpressionAst>();

                    foreach (var tag in tagStrings)
                        yield return tag.Value;

                    break;
                }

                if (element is CommandParameterAst ast && "tags".Contains(ast.ParameterName.ToLower()))
                    nextElementIsName1 = true;
            }
        }

        private static void Log(TestMessageLevel level, string message, IMessageLogger logger) =>
            logger?.SendMessage(level, message);
    }
}
