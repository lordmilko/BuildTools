using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Install, "Dependency")]
    [BuildCommand(CommandKind.InstallDependency, CommandCategory.Utility, Feature.Dependency)]
    public abstract class InstallDependency<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        [Parameter(Mandatory = false, Position = 0)]
        [ArgumentCompleter(typeof(DependencyCompleter<>))]
        [ValidateSetEx(typeof(DependencyValidator<>))]
        public string[] Name { get; set; }

        public static void CreateHelp(HelpConfig help, ProjectConfig project, ICommandService commandService)
        {
            help.Synopsis = $"Installs dependencies required to use the {project.Name} Build Environment";
            help.Description = $"The {help.Command} cmdlet installs dependencies required to utilize the {project.Name} Build Environment. By default, {help.Command} will install all dependencies that are required. A specific dependency can be installed by specifying a value to the -Name parameter. If dependencies are not installed, the {project.Name} Build Environment will automatically install a given dependency for you when attempting to execute a command that requires it.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Name), "The dependencies to install. If no value is specified, all dependencies will be installed.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, $"Install all dependencies required to use the {project.Name} Build Environment"),
                new HelpExample($"{help.Command} codecov", $"Install the version of CodeCov required by the {project.Name} Build Environment")
            };
        }

        protected override void ProcessRecordEx()
        {
            var provider = GetService<DependencyProvider>();

            var dependencies = provider.GetDependencies(Name);

            foreach (var dependency in dependencies)
                WriteObject(provider.Install(dependency, false));
        }
    }
}
