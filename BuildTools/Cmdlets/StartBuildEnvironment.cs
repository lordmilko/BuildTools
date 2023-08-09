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
    [Cmdlet(VerbsLifecycle.Start, "BuildEnvironment", DefaultParameterSetName = ParameterAttribute.AllParameterSets)]
    public class StartBuildEnvironment : GlobalBuildCmdlet
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

        [Parameter(Mandatory = false)]
        public SwitchParameter Quiet { get; set; }

        /// <summary>
        /// Indicates that the alternate CI environment that would not normally be loaded should be used.
        /// </summary>
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.CI)]
        public SwitchParameter Alternate { get; set; }

        protected override void ProcessRecordEx()
        {
            //Process the config before checking for possible singleton environments so that on PowerShell Core
            //we throw a more specific error message that we're trying to reimport the same environment twice
            var factory = GetService<IProjectConfigProviderFactory>();
            var configProvider = factory.CreateProvider(BuildRoot, File);

            var powerShell = GetService<IPowerShellService>();

            bool isSingleton = powerShell.Edition == PSEdition.Core;
            ValidateSingleton(powerShell, isSingleton);

            //Build dynamic cmdlet types based on the configuration file
            var dynamicAssemblyBuilder = new DynamicAssemblyBuilder(configProvider.Config);

            powerShell.WriteVerbose("Generating dynamic cmdlets");
            dynamicAssemblyBuilder.BuildCmdlets(isSingleton);

            var name = configProvider.Config.Name;

            IPowerShellModule module = null;

            //Create and import a dynamic module containing the dynamic cmdlets

            if (CI)
            {
                powerShell.WriteVerbose($"Generating CI module instead of regular module as -{nameof(CI)} was specified");
                RegisterCI(configProvider, powerShell, name);
            }
            else
                module = powerShell.RegisterModule($"{name}.Build", dynamicAssemblyBuilder.CmdletTypes);

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
                //If we're in CI, module is null
                if (!CI)
                {
                    var helpService = envProvider.GetService<IHelpService>();
                    helpService.RegisterHelp(module);
                }

                if (!Quiet && !CI)
                {
                    powerShell.InitializePrompt(configProvider.Config);

                    //And then display the welcome banner listing the key cmdlets that are available
                    var bannerService = envProvider.GetService<BannerService>();
                    bannerService.DisplayBanner();
                }
            }
            finally
            {
                powerShell.Pop();
            }
        }

        private void RegisterCI(IProjectConfigProvider configProvider, IPowerShellService powerShell, string name)
        {
            var environmentService = GetService<EnvironmentService>();

            var appveyor = environmentService.IsCI ? environmentService.IsAppveyor : powerShell.IsWindows;

            if (Alternate)
                appveyor = !appveyor;

            if (appveyor)
            {
                powerShell.WriteVerbose("Generating Appveyor CI module");

                RegisterCIModule<AppveyorCmdlet>(configProvider, powerShell, name, "Appveyor");
            }
            else
            {
                powerShell.WriteVerbose("Generating generic CI module");

                RegisterCIModule<GenericCICmdlet>(configProvider, powerShell, name, "CI");
            }
        }

        private void RegisterCIModule<T>(IProjectConfigProvider configProvider, IPowerShellService powerShell, string name, string suffix)
        {
            var moduleName = $"{name}.{suffix}";

            if (BuildToolsSessionState.ContinuousIntegrationOwner != null)
                throw new InvalidOperationException($"Cannot register {name} cmdlets: {name} cmdlets have already been registered under the {BuildToolsSessionState.ContinuousIntegrationOwner} build environment");

            var cmdlets = typeof(StartBuildEnvironment).Assembly.GetTypes()
                .Where(v =>
                {
                    if (!typeof(T).IsAssignableFrom(v))
                        return false;

                    if (v.GetCustomAttribute<CmdletAttribute>() == null)
                        return false;

                    var featureAttrib = v.GetCustomAttribute<FeatureAttribute>();

                    if (featureAttrib != null)
                    {
                        if (!configProvider.HasFeature(featureAttrib.Feature))
                            return false;
                    }

                    return true;
                })
                .ToArray();

            powerShell.RegisterModule(moduleName, cmdlets);

            BuildToolsSessionState.ContinuousIntegrationOwner = moduleName;
        }

        private void ValidateSingleton(IPowerShellService powerShell, bool isSingleton)
        {
            if (isSingleton)
            {
                /* Cmdlets that need to access the currrent environment (such as argument completers/validators) do so by being defined as taking a generic type argument
                 * that is filled in to be the dynamically generated environment type by the dynamic assembly builder. In .NET Framework this all works fine, however in .NET Core,
                 * when attempting to load the custom attributes of a cmdlet parameter, the runtime either won't see that the BuildTools.GeneratedCode assembly is already loaded,
                 * or will see it but ignore it due to it being a dynamic assembly. In attempting to load it, the Location property of the Assembly will be touched, which is invalid
                 * for dynamically generated assemblies. To prevent the CLR from touching the Location property we can try and intercept the load event of the AssemblyLoadContext,
                 * however attempting to return our already known dynamic assembly here generates an exception that dynamic assemblies are not valid in this context.
                 *
                 * As such, we work around this in PowerShell Core by utilizing the well known SingletonEnvironment, and not creating a dynamically generated environment type at all.
                 * The flipside of this is that we can't allow any other environments to be loaded, as we'll have no way of referring to one environment over another within our custom
                 * attributes */

                if (BuildToolsSessionState.Environments.Length > 0)
                    throw new InvalidOperationException("Cannot load environment: another environment is already loaded, and PowerShell Core only supports singleton environments.");

                powerShell.WriteVerbose("Build Environment is running under PowerShell Core. Only a single build environment will be allowed");
            }
        }
    }
}
