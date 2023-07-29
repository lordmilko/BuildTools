using System;
using System.Linq;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Invoke, "Test")]
    [BuildCommand(CommandKind.InvokeTest, CommandCategory.Test)]
    public abstract class InvokeTest<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider, IIntegrationProvider
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string[] Name
        {
            get => invokeTestConfig.Name;
            set => invokeTestConfig.Name = value;
        }

        [Parameter(Mandatory = false)]
        [ArgumentCompleter(typeof(SupportedTestTypeCompleter<>))]
        [ValidateSetEx(typeof(SupportedTestTypeValidator<>))]
        public string[] Type
        {
            get => invokeTestConfig.Type?.Select(v => v.ToString()).ToArray();
            set => invokeTestConfig.Type = value?.Select(v => v.DescriptionToEnum<TestType>()).ToArray();
        }

        [Parameter(Mandatory = false)]
        public BuildConfiguration Configuration
        {
            get => invokeTestConfig.Configuration;
            set => invokeTestConfig.Configuration = value;
        }

        [DynamicParameter]
        public SwitchParameter Integration
        {
            get => invokeTestConfig.Integration;
            set => invokeTestConfig.Integration = value;
        }

        [Parameter(Mandatory = false)]
        public string[] Tags
        {
            get => invokeTestConfig.Tags;
            set => invokeTestConfig.Tags = value;
        }

        private InvokeTestConfig invokeTestConfig = new InvokeTestConfig();

        public static void CreateHelp(HelpConfig help, IProjectConfigProvider configProvider, ICommandService commandService)
        {
            var project = configProvider.Config;
            var unitTest = configProvider.GetUnitTestProject(false);
            var testResultCommand = commandService.GetCommand(CommandKind.TestResult);

            help.Synopsis = $"Executes tests on a {project.Name} build.";

            help.Description = $@"
The {help.Command} cmdlet executes tests on previously generated builds of {project.Name}. By default, test types for all languages supported by the project (e.g. both C# and PowerShell) will be executed against the last Debug build. Tests can be limited to a specific platform by specifying a value to the -Type parameter, and can also be limited to those whose name matches a specified wildcard expression via the -Name parameter.

Tests executed by {help.Command} are automatically logged in the TRX format (C#) and NUnitXml format (PowerShell) under the {unitTest.Name}\TestResults folder of the {project.Name} solution. Test results in this directory can be evaluated and filtered after the fact using the {testResultCommand.Name} cmdlet. Note that upon compiling a new build of {unitTest.Name}, all items in this test results folder will automatically be deleted.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Name), "Wildcard used to specify tests to execute. If no value is specified, all tests will be executed."),
                new HelpParameter(nameof(Type), "Type of tests to execute. If no value is specified, both C# and PowerShell tests will be executed."),
                new ConditionalHelpParameter(NeedLegacyParameter, LegacyParameterName, $"Specifies whether to test {project.Name} using the .NET Core CLI or legacy .NET Framework tooling."),
                new HelpParameter(nameof(Configuration), "Build configuration to test. If no value is specified, the last Debug build will be tested."),
                new ConditionalHelpParameter(NeedIntegrationParameter, nameof(Integration), "Specifies to run integration tests instead of unit tests."),
                new HelpParameter(nameof(Tags), "Specifies tags or test categories to execute. If a Name is specified as well, these two categories will be filtered using logical AND.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, $"Executes all unit tests on the last {project.Name} build."),
                new HelpExample($"{help.Command} *dynamic*", "Executes all tests whose name contains the word \"dynamic\"."),
                new HelpExample($"{help.Command} -Type PowerShell", "Executes all PowerShell tests only."),
                new HelpExample($"{help.Command} -Configuration Release", $"Executes tests on the Release build of {project.Name}."),
                new ConditionalHelpExample(NeedIntegrationParameter, $"{help.Command} -Integration", $"Invoke all integration tests on the last {project.Name} build.")
            };

            help.RelatedLinks = new[]
            {
                commandService.GetCommand(CommandKind.InvokeBuild),
                commandService.GetCommand(CommandKind.TestResult)
            };
        }

        protected override void ProcessRecordEx()
        {
            var service = GetService<InvokeTestService>();

            service.Execute(invokeTestConfig, IsLegacyMode);
        }

        public string[] GetLegacyParameterSets() => null;

        public string[] GetIntegrationParameterSets() => null;
    }
}
