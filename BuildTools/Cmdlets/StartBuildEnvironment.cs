using System;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using BuildTools.Cmdlets.Appveyor;
using BuildTools.Cmdlets.CI;
using BuildTools.Dynamic;
using BuildTools.PowerShell;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "BuildEnvironment")]
    public class StartBuildEnvironment : BuildCmdlet<object>
    {
        /// <summary>
        /// The "build" folder containing the config file to read.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string BuildRoot { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        public string File { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = ParameterSet.CI)]
        public SwitchParameter CI { get; set; }

        /// <summary>
        /// Indicates that the alternate CI environment that would not normally be loaded should be used.
        /// </summary>
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.CI)]
        public SwitchParameter Alternate { get; set; }

        protected override void ProcessRecordEx()
        {
            var factory = GetService<IProjectConfigProviderFactory>();

            var configProvider = factory.CreateProvider(BuildRoot, File);

            //Build dynamic cmdlet types based on the configuration file
            var dynamicAssemblyBuilder = new DynamicAssemblyBuilder(configProvider.Config);
            dynamicAssemblyBuilder.BuildCmdlets();

            var name = configProvider.Config.Name;

            //Create and import a dynamic module containing the dynamic cmdlets
            var powerShell = GetService<IPowerShellService>();
            var module = powerShell.RegisterModule($"{name}.Build", dynamicAssemblyBuilder.CmdletTypes);

            if (CI)
                RegisterCI(powerShell, name);

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
            var envProvider = (ServiceProvider) BuildToolsSessionState.ServiceProvider(environmentId);

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

        private void RegisterCI(IPowerShellService powerShell, string name)
        {
            var environmentService = GetService<EnvironmentService>();

            var appveyor = environmentService.IsCI ? environmentService.IsAppveyor : powerShell.IsWindows;

            if (Alternate)
                appveyor = !appveyor;

            if (appveyor)
                RegisterCIModule<AppveyorCmdlet>(powerShell, name, "Appveyor");
            else
                RegisterCIModule<GenericCICmdlet>(powerShell, name, "CI");
        }

        private void RegisterCIModule<T>(IPowerShellService powerShell, string name, string suffix)
        {
            var moduleName = $"{name}.{suffix}";

            if (BuildToolsSessionState.ContinuousIntegrationOwner != null)
                throw new InvalidOperationException($"Cannot register {name} cmdlets: {name} cmdlets have already been registered under the {BuildToolsSessionState.ContinuousIntegrationOwner} build environment");

            var cmdlets = typeof(StartBuildEnvironment).Assembly.GetTypes().Where(v => typeof(T).IsAssignableFrom(v) && v.GetCustomAttribute<CmdletAttribute>() != null).ToArray();

            powerShell.RegisterModule(moduleName, cmdlets);

            BuildToolsSessionState.ContinuousIntegrationOwner = moduleName;
        }

        protected override T GetService<T>() => BuildToolsSessionState.GlobalServiceProvider.GetService<T>();
    }
}
