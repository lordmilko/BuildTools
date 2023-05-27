using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [CommandCategory(CommandCategory.Utility)]
    [Cmdlet(VerbsLifecycle.Install, "Dependency")]
    public abstract class InstallBuildDependency<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        [Parameter(Mandatory = false, Position = 0)]
        [ArgumentCompleter(typeof(DependencyCompleter<>))]
        [ValidateSetEx(typeof(DependencyValidator<>))]
        public string[] Name { get; set; }

        protected override void ProcessRecordEx()
        {
            var provider = GetService<DependencyProvider>();

            var dependencies = provider.GetDependencies(Name);

            foreach (var dependency in dependencies)
                WriteObject(provider.Install(dependency, false));
        }
    }
}
