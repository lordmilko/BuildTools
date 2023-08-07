using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Coverage")]
    [BuildCommand(CommandKind.Coverage, CommandCategory.CI)]
    public abstract class GetCoverage<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string Name
        {
            get => coverageConfig.Name;
            set => coverageConfig.Name = value;
        }

        [ArgumentCompleter(typeof(SupportedTestTypeCompleter<>))]
        [ValidateSetEx(typeof(SupportedTestTypeValidator<>))]
        [Parameter(Mandatory = false)]
        public string[] Type
        {
            get => coverageConfig.Type?.Select(v => v.ToString()).ToArray();
            set => coverageConfig.Type = value?.Select(v => v.DescriptionToEnum<TestType>()).ToArray();
        }

        [Parameter(Mandatory = false)]
        public BuildConfiguration Configuration
        {
            get => coverageConfig.Configuration;
            set => coverageConfig.Configuration = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter TestOnly
        {
            get => coverageConfig.TestOnly;
            set => coverageConfig.TestOnly = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter SkipReport { get; set; }

        private CoverageConfig coverageConfig;

        protected GetCoverage()
        {
            var configProvider = BuildToolsSessionState.ServiceProvider<TEnvironment>().GetService<IProjectConfigProvider>();

            coverageConfig = new CoverageConfig(configProvider.Config.TestTypes);
        }

        public static void CreateHelp(HelpConfig help, ProjectConfig project, ICommandService commandService)
        {
            help.Synopsis = $"Generates a code coverage report for {project.Name}";
            help.Description = $@"
The {help.Command} cmdlet generates a code coverage report for {project.Name}. By default, all tests will be executed using the Debug configuration. Coverage analysis can be limited to a subset of tests by specifying a wildcard to the -Name parameter.

When the coverage analysis has completed, a HTML report detailing the results of the analysis will automatically be opened in your default web browser.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Name), "Wildcard used to limit coverage to those whose test names match a specified pattern."),
                new HelpParameter(nameof(Type), "Types of tests to generate coverage for. If no type is specified, coverage for all test types in your project will be generated."),
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
            var service = GetService<GetCoverageService>();
            var powerShell = GetService<IPowerShellService>();
            var configProvider = GetService<IProjectConfigProvider>();

            service.GetCoverage(coverageConfig, IsLegacyMode);

            if (TestOnly || SkipReport)
                return;

            var date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var coverageDir = Path.Combine(Path.GetTempPath(), $"{configProvider.Config.Name}Coverage_{date}");

            powerShell.WriteColor("Generating coverage report in $dir", ConsoleColor.Cyan);

            service.CreateReport(targetDir: coverageDir);

            Process.Start(Path.Combine(coverageDir, "index.htm"));
        }

        public string[] GetLegacyParameterSets() => null;
    }
}
