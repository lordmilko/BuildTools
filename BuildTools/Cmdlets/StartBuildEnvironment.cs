using System.Management.Automation;
using BuildTools.Dynamic;
using BuildTools.PowerShell;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "BuildEnvironment")]
    public class StartBuildEnvironment : BuildCmdlet<object>
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Root { get; set; }

        protected override void ProcessRecordEx()
        {
            var factory = GetService<IProjectConfigProviderFactory>();

            var provider = factory.CreateProvider(Root);

            //Build dynamic cmdlet types based on the configuration file
            var dynamicAssemblyBuilder = new DynamicAssemblyBuilder(provider.Config);
            dynamicAssemblyBuilder.BuildCmdlets();

            //Create and import a dynamic module containing the dynamic cmdlets
            var powerShell = GetService<IPowerShellService>();
            powerShell.RegisterModule(provider.Config.Name, dynamicAssemblyBuilder.CmdletTypes);

            //Register the project's configuration provider so that it is accessible when its custom cmdlets execute
            var envProvider = (ServiceProvider) BuildToolsSessionState.ServiceProvider(dynamicAssemblyBuilder.EnvironmentId);
            envProvider.AddSingleton(provider);
        }

        protected override T GetService<T>() => BuildToolsSessionState.GlobalServiceProvider.GetService<T>();
    }
}
