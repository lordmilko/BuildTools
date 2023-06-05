﻿using System;
using System.Collections.Generic;
using BuildTools.PowerShell;

namespace BuildTools
{
    static class BuildToolsSessionState
    {
        private static Dictionary<Type, IServiceProvider> serviceProviderCache = new Dictionary<Type, IServiceProvider>();

        internal static IServiceProvider ServiceProvider<TEnvironment>() =>
            ServiceProvider(typeof(TEnvironment));

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
                { typeof(IFileSystemProvider), typeof(FileSystemProvider) },
                { typeof(IProjectConfigProviderFactory), typeof(ProjectConfigProviderFactory) },
                { typeof(IPowerShellService), typeof(PowerShellService) },
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

                typeof(DotnetDependencyInstaller),
                typeof(ChocolateyDependencyInstaller),
                typeof(PSPackageDependencyInstaller),
                typeof(PSPackageProviderDependencyInstaller),
                typeof(TargetingPackDependencyInstaller),

                { typeof(IConsoleLogger), typeof(ConsoleLogger) },
                { typeof(IFileLogger), typeof(FileLogger) },

                { typeof(IAlternateDataStreamService), typeof(AlternateDataStreamService) },
                { typeof(IEnvironmentVariableProvider), typeof(EnvironmentVariableProvider) },
                { typeof(IFileSystemProvider), typeof(FileSystemProvider) },
                { typeof(IHasher), typeof(Hasher) },
                { typeof(IHelpService), typeof(HelpService) },
                { typeof(IPowerShellService), typeof(PowerShellService) },
                { typeof(IProcessService), typeof(ProcessService) },
                { typeof(IVsProductLocator), typeof(VsProductLocator) },
                { typeof(IWebClient), typeof(WebClient) }
            };

            var serviceProvider = serviceCollection.Build();

            return serviceProvider;
        }
    }
}