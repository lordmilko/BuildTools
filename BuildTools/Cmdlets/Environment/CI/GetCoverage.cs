using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Coverage")]
    [BuildCommand(CommandKind.Coverage, CommandCategory.CI)]
    public abstract class GetCoverage<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string Name { get; set; }

        [ArgumentCompleter(typeof(SupportedTestTypeCompleter<>))]
        [ValidateSetEx(typeof(SupportedTestTypeValidator<>))]
        [Parameter(Mandatory = false)]
        public string[] Type { get; set; }

        [Parameter(Mandatory = false)]
        public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Debug;

        [Parameter(Mandatory = false)]
        public SwitchParameter TestOnly { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter SkipReport { get; set; }

        public static void CreateHelp(HelpConfig help, ProjectConfig project, CommandService commandService)
        {
            help.Synopsis = $"Generates a code coverage report for {project.Name}";
            help.Description = $@"
The {help.Command} cmdlet generates a code coverage report for {project.Name}. By default, all tests will be executed using the Debug configuration. Coverage analysis can be limited to a subset of tests by specifying a wildcard to the -Name parameter.

When the coverage analysis has completed, a HTML report detailing the results of the analysis will automatically be opened in your default web browser.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Name), "Wildcard used to limit coverage to those whose test names match a specified pattern."),
                new HelpParameter(nameof(Type), "Types of tests to generate coverage for. If no type is specified, both C# and PowerShell test coverage will be generated."),
                new HelpParameter(nameof(Configuration), "Build configuration to use when calculating coverage. If no configuration is specified, Debug will be used."),
                new ConditionalHelpParameter(NeedLegacyParameter, LegacyParameterName, "Specifies whether code coverage should be generated using .NET Core CLI tooling (where applicable) or legacy .NET Framework tooling."),
                new HelpParameter(nameof(TestOnly), "Run the test commands used by OpenCover without collecting coverage."),
                new HelpParameter(nameof(SkipReport), "Skip generating a HTML report upon generating code coverage.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, "Generate a code coverage report"),
                new HelpExample($"{help.Command} *dynamic*", "Generate a code coverage report of all tests whose name contains the word \"dynamic\"")
            };

            help.RelatedLinks = new[]
            {
                commandService.GetCommand(CommandKind.InvokeBuild)
            };
        }

        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }

        public string[] GetLegacyParameterSets()
        {
            throw new NotImplementedException();
        }
    }
}