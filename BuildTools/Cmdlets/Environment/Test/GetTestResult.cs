using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "TestResult", DefaultParameterSetName = ParameterSet.Default)]
    [BuildCommand(CommandKind.TestResult, CommandCategory.Version)]
    public abstract class GetTestResult<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string Name { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Default, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path { get; set; }

        [ArgumentCompleter(typeof(SupportedTestTypeCompleter<>))]
        [ValidateSetEx(typeof(SupportedTestTypeValidator<>))]
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Default)]
        public string[] Type { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Default)]
        public TestOutcome Outcome { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.ListAvailable)]
        public SwitchParameter ListAvailable { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Integration { get; set; }

        public static void CreateHelp(HelpConfig help, ProjectConfig project, CommandService commandService)
        {
            var invokeTest = commandService.GetCommand(CommandKind.InvokeTest);

            help.Synopsis = $"Retrieve test results from the last invocation of {invokeTest.Name}";
            help.Description = $@"
The {help.Command} cmdlet retrieves test results from the last invocation of {invokeTest.Name}. By default, test all test results for both C# and PowerShell can be displayed. Results can be limited to either of the two by specifying a value to the -Type parameter. The -Name parameter allows results to be further limited based on a wildcard expression that matches part of the results name. Results can also be filtered to those that had a particular status (such as Failed) using the -Outcome parameter.

To view results from one or more previous test invocations, the -ListAvailable parameter can be used to enumerate all available test files. These files can then be piped into {help.Command} to view their contents. To view available integration tests, specify the -Integration parameter in conjunction with the -ListAvailable parameter.

Note that whenever the PrtgAPI.Tests.UnitTests and PrtgAPI.Tests.IntegrationTests projects are built, all previous test results will automatically be cleared.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Name), "Wildcard specifying the tests to view the results of. If no value is specified, all test results will be displayed."),
                new HelpParameter(nameof(Path), "One or more test files to view the results of. Accepts values by pipeline."),
                new HelpParameter(nameof(Type), "Type of test results to view. By default both C# and PowerShell test results will be displayed."),
                new HelpParameter(nameof(Outcome), "Limits test results to only those with a specified outcome."),
                new HelpParameter(nameof(ListAvailable), "Lists all test files that are available within the test results directory."),
                new HelpParameter(nameof(Integration), "Indicates to retrieve test results from the last integration test run rather than unit test run.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, "View all test results"),
                new HelpExample($"{help.Command} *dynamic*", "View all test results whose name contains the word \"dynamic\""),
                new HelpExample($"{help.Command} -Outcome Failed", $"View all tests that failed in the last invocation of {invokeTest.Name}"),
                new HelpExample($"{help.Command} -ListAvailable", "List all unit test results that are available"),
                new HelpExample($"{help.Command} *2019* -ListAvailable | {help.Command} *dynamic* -Type C#", "Get all C# test results from 2019 whose test name contains the word \"dynamic\""),
                new HelpExample($"{help.Command} -Integration", $"View all test results from the last invocation of {invokeTest.Name} -Integration")
            };

            help.RelatedLinks = new[]
            {
                commandService.GetCommand(CommandKind.InvokeTest)
            };
        }

        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}