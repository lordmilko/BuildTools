﻿using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "TestResult", DefaultParameterSetName = ParameterSet.Default)]
    [BuildCommand(CommandKind.TestResult, CommandCategory.Test, Feature.Test)]
    public abstract class GetTestResult<TEnvironment> : BuildCmdlet<TEnvironment>, IIntegrationProvider
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string Name
        {
            get => testResultConfig.Name;
            set => testResultConfig.Name = value;
        }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Default, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get => testResultConfig.Path;
            set => testResultConfig.Path = value;
        }

        [ArgumentCompleter(typeof(SupportedTestTypeCompleter<>))]
        [ValidateSetEx(typeof(SupportedTestTypeValidator<>))]
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Default)]
        public string[] Type
        {
            get => testResultConfig.Type?.Select(v => v.ToString()).ToArray();
            set => testResultConfig.Type = value?.Select(v => v.DescriptionToEnum<TestType>()).ToArray();
        }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Default)]
        public TestOutcome Outcome
        {
            get => testResultConfig.Outcome ?? default;
            set => testResultConfig.Outcome = value;
        }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.ListAvailable)]
        public SwitchParameter ListAvailable { get; set; }

        [DynamicParameter]
        public SwitchParameter Integration
        {
            get => testResultConfig.Integration;
            set => testResultConfig.Integration = value;
        }

        private TestResultConfig testResultConfig;

        protected GetTestResult()
        {
            var configProvider = BuildToolsSessionState.ServiceProvider<TEnvironment>().GetService<IProjectConfigProvider>();

            testResultConfig = new TestResultConfig(configProvider.Config.TestTypes);
        }

        public static void CreateHelp(HelpConfig help, IProjectConfigProvider configProvider, ICommandService commandService)
        {
            var invokeTestName = commandService.GetCommandNameOrDefault(CommandKind.InvokeTest);

            var description = new StringBuilder();

            description.Append(
                $"The {help.Command} cmdlet retrieves test results from the last invocation of {invokeTestName}. By default, test results for all supported test languages will be displayed. " +
                "Results can be limited to either of the two by specifying a value to the -Type parameter. The -Name parameter allows results to be further limited based on a wildcard expression that matches part of the results' name. " +
                "Results can also be filtered to those that had a particular status (such as Failed) using the -Outcome parameter."
            ).AppendLine().AppendLine();

            description.Append($"To view results from one or more previous test invocations, the -ListAvailable parameter can be used to enumerate all available test files. These files can then be piped into {help.Command} to view their contents.");

            if (configProvider.GetProjects(false).Any(a => (a.Kind & ProjectKind.IntegrationTest) != 0))
                description.Append(" To view available integration tests, specify the -Integration parameter in conjunction with the -ListAvailable parameter.");

            var testTypes = configProvider.GetProjects(false).Where(v => (v.Kind & ProjectKind.Test) != 0).ToArray();

            if (testTypes.Length > 0)
            {
                description.AppendLine().AppendLine();

                var str = string.Join(" and ", testTypes.OrderByDescending(v => (v.Kind & ProjectKind.UnitTest) != 0).Select(v => v.NormalizedName));

                description.Append($"Note that whenever the {str} projects are built, all previous test results may automatically be cleared.");
            }

            help.Synopsis = $"Retrieve test results from the last invocation of {invokeTestName}";
            help.Description = description.ToString();

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Name), "Wildcard specifying the tests to view the results of. If no value is specified, all test results will be displayed."),
                new HelpParameter(nameof(Path), "One or more test files to view the results of. Accepts values by pipeline."),
                new HelpParameter(nameof(Type), "Type of test results to view. By default test results for all supported languages will be displayed."),
                new HelpParameter(nameof(Outcome), "Limits test results to only those with a specified outcome."),
                new HelpParameter(nameof(ListAvailable), "Lists all test files that are available within the test results directory."),
                new ConditionalHelpParameter(NeedIntegrationParameter, nameof(Integration), "Indicates to retrieve test results from the last integration test run rather than unit test run.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, "View all test results"),
                new HelpExample($"{help.Command} *dynamic*", "View all test results whose name contains the word \"dynamic\""),
                new HelpExample($"{help.Command} -Outcome Failed", $"View all tests that failed in the last invocation of {invokeTestName}"),
                new HelpExample($"{help.Command} -ListAvailable", "List all unit test results that are available"),
                new HelpExample($"{help.Command} *2019* -ListAvailable | {help.Command} *dynamic* -Type C#", "Get all C# test results from 2019 whose test name contains the word \"dynamic\""),
                new ConditionalHelpExample(NeedIntegrationParameter, $"{help.Command} -Integration", $"View all test results from the last invocation of {invokeTestName} -Integration") //todo
            };

            help.RelatedLinks = new[]
            {
                commandService.GetOptionalCommand(CommandKind.InvokeTest)
            };
        }

        protected override void ProcessRecordEx()
        {
            var service = GetService<GetTestResultService>();

            IEnumerable results;

            switch (ParameterSetName)
            {
                case ParameterSet.Default:
                    results = service.Execute(testResultConfig);
                    break;

                case ParameterSet.ListAvailable:
                    results = service.List(Name, Integration);
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle parameter set '{ParameterSetName}'.");
            }

            foreach (var item in results)
                WriteObject(item);
        }

        public string[] GetIntegrationParameterSets() => new[] { ParameterSet.Default };
    }
}
