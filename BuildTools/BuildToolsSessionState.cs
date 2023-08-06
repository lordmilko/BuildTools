using System;
using System.Collections.Generic;
using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    static class BuildToolsSessionState
    {
        private static Dictionary<Type, IServiceProvider> serviceProviderCache = new Dictionary<Type, IServiceProvider>();

        internal static IServiceProvider ServiceProvider<TEnvironment>() =>
            ServiceProvider(typeof(TEnvironment));

        internal static bool HeadlessUI { get; set; }

        internal static bool ScriptAnalyzerRepaired { get; set; }

        internal static Type[] Environments => serviceProviderCache.Keys.ToArray();

        internal static string ContinuousIntegrationOwner { get; set; }

        internal static bool? AppveyorBuildCore { get; set; }

        internal static IServiceProvider ServiceProvider(Type environment)
        {
            if (environment == null)
                throw new ArgumentNullException(nameof(environment));

            if (!typeof(IEnvironmentIdentifier).IsAssignableFrom(environment))
                throw new ArgumentException($"Environment Identifier '{environment.Name}' is not assignable from type '{typeof(IEnvironmentIdentifier).Name}'.");

            if (!serviceProviderCache.TryGetValue(environment, out var serviceProvider))
            {
                serviceProvider = CreateServiceProvider();
                serviceProviderCache[environment] = serviceProvider;
            }

            return serviceProvider;
        }

        private static IServiceProvider globalServiceProvider;

        internal static IServiceProvider GlobalServiceProvider
        {
            get
            {
                if (globalServiceProvider == null)
                    globalServiceProvider = CreateGlobalServiceProvider();

                return globalServiceProvider;
            }
        }

        private static IServiceProvider CreateGlobalServiceProvider()
        {
            var serviceCollection = new ServiceCollection
            {
                typeof(NewBuildEnvironmentService),

                typeof(EnvironmentService),
                { typeof(IFileSystemProvider), typeof(FileSystemProvider) },
                { typeof(IProjectConfigProviderFactory), typeof(ProjectConfigProviderFactory) },
                { typeof(IPowerShellService), typeof(PowerShellService) },
                { typeof(IProcessService), typeof(ProcessService) },
                { typeof(IEnvironmentVariableProvider), typeof(EnvironmentVariableProvider) }
            };

            var serviceProvider = serviceCollection.Build();

            return serviceProvider;
        }

        private static IServiceProvider CreateServiceProvider()
        {
            var serviceCollection = new ServiceCollection
            {
                typeof(BannerService),
                typeof(DependencyProvider),
                typeof(EnvironmentService),
                typeof(HelpBuilder),
                typeof(Logger),

                //Cmdlet Services
                typeof(ClearBuildService),
                typeof(GetLogService),
                typeof(GetCoverageService),
                typeof(GetTestResultService),
                typeof(GetVersionService),
                typeof(SetVersionService),
                typeof(InvokeBuildService),
                typeof(InvokeTestService),
                typeof(NewPackageService),
                typeof(StartModuleService),

                #region Appveyor

                typeof(ClearAppveyorBuildService),
                typeof(InvokeAppveyorInstallService),
                typeof(GetAppveyorVersionService),
                typeof(SetAppveyorVersionService),

                typeof(InvokeAppveyorBeforeBuildService),
                typeof(InvokeAppveyorBuildService),
                typeof(InvokeAppveyorAfterBuildService),

                typeof(InvokeAppveyorBeforeTestService),
                typeof(InvokeAppveyorTestService),
                typeof(InvokeAppveyorAfterTestService),

                typeof(NewAppveyorPackageService),
                typeof(MeasureAppveyorCoverageService),
                typeof(SimulateAppveyorService),

                typeof(AppveyorPackageProviderServices),
                typeof(AppveyorCSharpPackageProvider),
                typeof(AppveyorPowerShellPackageProvider),
                { typeof(IAppveyorClient), typeof(AppveyorClient) },

                #endregion
                #region CI

                typeof(ClearCIBuildService),
                typeof(InvokeCIInstallService),
                typeof(InvokeCIScriptService),
                typeof(InvokeCIBuildService),
                typeof(InvokeCITestService),
                typeof(SimulateCIService),

                #endregion

                typeof(DotnetDependencyInstaller),
                typeof(ChocolateyDependencyInstaller),
                typeof(PSPackageDependencyInstaller),
                typeof(PSPackageProviderDependencyInstaller),
                typeof(TargetingPackDependencyInstaller),

                typeof(CSharpPackageProvider),
                typeof(PowerShellPackageProvider),
                typeof(CSharpPackageSourceService),
                typeof(PowerShellPackageSourceService),

                { typeof(IConsoleLogger), typeof(ConsoleLogger) },
                { typeof(IFileLogger), typeof(FileLogger) },

                { typeof(IAlternateDataStreamService), typeof(AlternateDataStreamService) },
                { typeof(ICommandService), typeof(CommandService) },
                { typeof(IEnvironmentVariableProvider), typeof(EnvironmentVariableProvider) },
                { typeof(IFileSystemProvider), typeof(FileSystemProvider) },
                { typeof(IHasher), typeof(Hasher) },
                { typeof(IHelpService), typeof(HelpService) },
                { typeof(IPowerShellService), typeof(PowerShellService) },
                { typeof(IProcessService), typeof(ProcessService) },
                { typeof(IVsProductLocator), typeof(VsProductLocator) },
                { typeof(IWebClient), typeof(WebClient) },
                { typeof(IZipService), typeof(ZipService) }
            };

            var serviceProvider = serviceCollection.Build();

            return serviceProvider;
        }
    }
}
