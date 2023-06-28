using System.IO;
using System.Linq;
using System.Net;
using BuildTools.PowerShell;

namespace BuildTools
{
    class DotnetDependencyInstaller : IDependencyInstaller
    {
        private readonly EnvironmentService environmentService;
        private readonly IFileSystemProvider fileSystem;
        public IProjectConfigProvider ConfigProvider { get; }
        private readonly IPowerShellService powerShell;
        private readonly Logger logger;
        private readonly IWebClient webClient;
        private readonly IAlternateDataStreamService adsService;

        private readonly string installDir;

        public DotnetDependencyInstaller(
            EnvironmentService environmentService,
            IFileSystemProvider fileSystem,
            IProjectConfigProvider configProvider,
            IPowerShellService powerShell,
            Logger logger,
            IWebClient webClient,
            IAlternateDataStreamService adsService)
        {
            this.environmentService = environmentService;
            this.fileSystem = fileSystem;
            this.ConfigProvider = configProvider;
            this.powerShell = powerShell;
            this.logger = logger;
            this.webClient = webClient;
            this.adsService = adsService;

            var root = configProvider.SolutionRoot;
            installDir = Path.Combine(root, "packages\\dotnet-sdk");
        }

        public DependencyResult Install(Dependency dependency, bool log, bool logSkipped = false)
        {
            if (environmentService.IsCI)
            {
                //dotnet SDK should be managed by CI system, not by us
                return new DependencyResult(dependency, WellKnownDependency.Dotnet, null, DependencyAction.Skipped);
            }

            if (TryGetExecutable(out var path))
                return new DependencyResult(dependency, path, null, DependencyAction.Skipped);

            var baseUrl = "https://dot.net/v1/";

            string fileName;

            if (powerShell.IsWindows)
                fileName = "dotnet-install.ps1";
            else
                fileName = "dotnet-install.sh";

            var url = $"{baseUrl}{fileName}";
            var outFile = Path.Combine(Path.GetTempPath(), fileName);

            if (powerShell.Edition != PSEdition.Core)
            {
                //Microsoft requires TLS 1.2
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }

            webClient.DownloadFile(url, outFile);

            if (powerShell.IsWindows)
            {
                //if we're mocking being windows we need to mock a stream unblocker too
                adsService.UnblockFile(outFile);

                powerShell.InvokeWithArgs($"& '{outFile}' -InstallDir '{installDir}' -NoPath");
            }
            else
            {
                powerShell.InvokeWithArgs($"chmod +x '{outFile}'; & '{outFile}' --install-dir '{installDir}' --no-path");
            }

            logger.LogVerbose($"Using 'dotnet' executable from '{installDir}'");

            environmentService.AppendPath(installDir);

            return new DependencyResult(dependency, WellKnownDependency.Dotnet, null, DependencyAction.Success);
        }

        private bool TryGetExecutable(out string path)
        {
            var command = powerShell.GetCommand("dotnet");

            if (command != null)
            {
                logger.LogVerbose($"Using 'dotnet' executable on PATH from '{command.Source}'");
                path = command.Source;
                return true;
            }
            else
            {
                if (fileSystem.DirectoryExists(installDir) && fileSystem.EnumerateFiles(installDir).Any(f => f.ToLower().StartsWith("dotnet")))
                {
                    logger.LogVerbose($"Using 'dotnet' executable from '{installDir}'");

                    environmentService.AppendPath(installDir);

                    path = WellKnownDependency.Dotnet;
                    return true;
                }
            }

            path = null;
            return false;
        }
    }
}