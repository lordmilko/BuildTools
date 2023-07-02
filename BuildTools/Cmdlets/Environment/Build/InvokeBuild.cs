using System;
using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Invoke, "Build")]
    [BuildCommand(CommandKind.InvokeBuild, CommandCategory.Build)]
    public abstract class InvokeBuild<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string Name
        {
            get => buildConfig.Name;
            set => buildConfig.Name = value;
        }

        [Alias("Args")]
        [Parameter(Mandatory = false, Position = 1)]
        public string[] ArgumentList
        {
            get => buildConfig.ArgumentList;
            set => buildConfig.ArgumentList = value;
        }

        [Parameter(Mandatory = false)]
        public BuildConfiguration Configuration
        {
            get => buildConfig.Configuration;
            set => buildConfig.Configuration = value;
        }

        [Alias("Dbg", "DebugMode")]
        [Parameter(Mandatory = false)]
        public SwitchParameter DebugBuild
        {
            get => buildConfig.DebugBuild;
            set => buildConfig.DebugBuild = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter SourceLink
        {
            get => buildConfig.SourceLink ?? false;
            set => buildConfig.SourceLink = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter ViewLog { get; set; } = true;

        private BuildConfig buildConfig = new BuildConfig();

        public static void CreateHelp(HelpConfig help, ProjectConfig project, ICommandService commandService)
        {
            help.Synopsis = $"Compiles {project.Name} from source";
            help.Description = $@"
The {help.Command} cmdlet compiles {help.Command} from source. By default, all projects in the {project.Name} solution will be built using the Debug configuration. A specific project can be built by specifying a wildcard expression to the -Name parameter.

In the event you wish to debug your build, the -Dbg parameter can be specified. This will generate a *.binlog file in the root of the project solution that will be automatically opened in the MSBuild Structured Log Viewer when the build has completed (assuming it is installed).";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Name), $"Wildcard specifying the name of a single {project.Name} project to build. If no value is specified, the entire {project.Name} solution will be built."),
                new HelpParameter(nameof(ArgumentList), "Additional arguments to pass to the build tool."),
                new HelpParameter(nameof(Configuration), $"Configuration to build. If no value is specified, {project.Name} will be built for Debug."),
                new HelpParameter(nameof(DebugBuild), "Specifies whether to generate an msbuild *.binlog file. File will automatically be opened upon completion of the build."),
                new ConditionalHelpParameter(NeedLegacyParameter, LegacyParameterName, $"Specifies whether to build the .NET Core version of {project.Name} or the legacy .NET Framework solution."),
                new HelpParameter(nameof(SourceLink), $"Specifies whether to build the .NET Core version of {project.Name} with SourceLink debug info. If this value is not specified, on Windows it will be true by default."),
                new HelpParameter(nameof(ViewLog), "Specifies whether to open the debug log upon finishing the build when -DebugBuild is specified.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, $"Build a Debug version of {help.Command}"),
                new HelpExample($"{help.Command} -c Release", $"Build a Release version of {help.Command}"),
                new HelpExample($"{help.Command} *test*", "Build just the projects whose name contains 'test'"),
                new HelpExample($"{help.Command} -Dbg", $"Build {project.Name} and log to a *.binlog file to be opened by the MSBuild Structured Log Viewer upon completion")
            };

            help.RelatedLinks = new[]
            {
                commandService.GetCommand(CommandKind.ClearBuild),
                commandService.GetCommand(CommandKind.InvokeTest)
            };
        }

        protected override void ProcessRecordEx()
        {
            var buildService = GetService<InvokeBuildService>();

            buildService.Build(buildConfig, IsLegacyMode);
        }

        public string[] GetLegacyParameterSets()
        {
            throw new NotImplementedException();
        }
    }
}