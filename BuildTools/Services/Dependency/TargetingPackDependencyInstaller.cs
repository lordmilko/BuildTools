using System;
using System.IO;
using BuildTools.PowerShell;

namespace BuildTools
{
    class TargetingPackDependencyInstaller : IDependencyInstaller
    {
        private readonly IPowerShellService powerShell;
        private readonly EnvironmentService environmentService;
        private readonly IFileSystemProvider fileSystem;
        private readonly IProcessService process;
        private readonly IWebClient webClient;
        private readonly IHasher hasher;
        private readonly Logger logger;

        public TargetingPackDependencyInstaller(
            IPowerShellService powerShell,
            EnvironmentService environmentService,
            IFileSystemProvider fileSystem,
            IProcessService process,
            IWebClient webClient,
            IHasher hasher,
            Logger logger)
        {
            this.powerShell = powerShell;
            this.environmentService = environmentService;
            this.fileSystem = fileSystem;
            this.process = process;
            this.webClient = webClient;
            this.hasher = hasher;
            this.logger = logger;
        }

        public DependencyResult Install(Dependency dependency, bool log, bool logSkipped = false)
        {
            if (!powerShell.IsWindows)
                throw new InvalidOperationException($"Cannot install targeting framework '{dependency.Name}': .NET Framework is only supported on Windows");

            var referenceRoot = Path.Combine(environmentService.ProgramFilesx86, "Reference Assemblies\\Microsoft\\Framework\\.NETFramework");
            var frameworkPath = Path.Combine(referenceRoot, $"v{dependency.Version}");

            if (!fileSystem.DirectoryExists(referenceRoot) || !fileSystem.DirectoryExists(frameworkPath))
            {
                string url;
                string hash;

                switch (dependency.Name)
                {
                    case "net452":
                        url = "https://download.microsoft.com/download/4/3/B/43B61315-B2CE-4F5B-9E32-34CCA07B2F0E/NDP452-KB2901951-x86-x64-DevPack.exe";
                        hash = "E37AA3BC40DAF9B4625F8CE44C1568A4";
                        break;

                    case "net461":
                        url = "https://download.microsoft.com/download/F/1/D/F1DEB8DB-D277-4EF9-9F48-3A65D4D8F965/NDP461-DevPack-KB3105179-ENU.exe";
                        hash = "C0FD653B0FB4A712DF609E9A52767CAE";
                        break;

                    case "net472":
                        url = "https://download.visualstudio.microsoft.com/download/pr/158dce74-251c-4af3-b8cc-4608621341c8/9c1e178a11f55478e2112714a3897c1a/ndp472-devpack-enu.exe";
                        hash = "B24CC35845EC4BAA9A1C423246073F89";
                        break;

                    default:
                        throw new NotImplementedException($"Don't know what the .NET Framework targeting pack installer is for version '{dependency.Name}'");
                }

                if (log)
                    logger.LogInformation($"`tInstalling '{dependency.Name}' .NET Framework targeting pack");

                var filePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(url));

                if (!fileSystem.FileExists(filePath) || hasher.HashFile(filePath) != hash)
                    webClient.DownloadFile(url, filePath);

                process.Execute(
                    filePath,
                    "/quiet /norestart",
                    errorFormat: "Process $filePath exited with error code {0}. A reboot may be required from a Windows Update. Please run executable manually to confirm error."
                );

                return new DependencyResult(dependency, dependency.Version, DependencyAction.Success);
            }
            else
            {
                if (log && logSkipped)
                    logger.LogInformation($"\tSkipping installing '{dependency.Name}' targeting pack as it is already installed");

                return new DependencyResult(dependency, dependency.Version, DependencyAction.Skipped);
            }
        }
    }
}