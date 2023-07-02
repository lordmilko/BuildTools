using System;
using System.Management.Automation;
using BuildTools.Dynamic;
using BuildTools.PowerShell;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "BuildEnvironment")]
    public class StartBuildEnvironment : BuildCmdlet<object>
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string BuildRoot { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        public string File { get; set; }

        protected override void ProcessRecordEx()
        {
            var factory = GetService<IProjectConfigProviderFactory>();

            var configProvider = factory.CreateProvider(BuildRoot, File);

            //Build dynamic cmdlet types based on the configuration file
            var dynamicAssemblyBuilder = new DynamicAssemblyBuilder(configProvider.Config);
            dynamicAssemblyBuilder.BuildCmdlets();

            //Create and import a dynamic module containing the dynamic cmdlets
            var powerShell = GetService<IPowerShellService>();
            var module = powerShell.RegisterModule(configProvider.Config.Name, dynamicAssemblyBuilder.CmdletTypes);

            //Now finalize the build environment
            FinalizeEnvironment(
                dynamicAssemblyBuilder.EnvironmentId,
                dynamicAssemblyBuilder.CmdletTypes.ToArray(),
                configProvider,
                module
            );
        }

        private void FinalizeEnvironment(
            Type environmentId,
            Type[] cmdletTypes,
            IProjectConfigProvider configProvider,
            IPowerShellModule module)
        {
            var envProvider = (ServiceProvider)BuildToolsSessionState.ServiceProvider(environmentId);

            //Register the project's configuration provider so that it is accessible when its custom cmdlets execute
            envProvider.AddSingleton(configProvider);

            //Register all the cmdlets in the build environment
            envProvider.AddSingleton<ICommandService>(new CommandService(cmdletTypes));

            //Note that we can't use WithActiveCmdlet here, because we don't want to use the GlobalServiceProvider!
            var powerShell = (PowerShellService) envProvider.GetService<IPowerShellService>();

            powerShell.Push(this);

            try
            {
                powerShell.InitializePrompt(configProvider.Config);

                var helpService = envProvider.GetService<IHelpService>();
                helpService.RegisterHelp(module);

                //And then display the welcome banner listing the key cmdlets that are available
                var bannerService = envProvider.GetService<BannerService>();
                bannerService.DisplayBanner();
            }
            finally
            {
                powerShell.Pop();
            }
        }

        protected override T GetService<T>() => BuildToolsSessionState.GlobalServiceProvider.GetService<T>();
    }
}
