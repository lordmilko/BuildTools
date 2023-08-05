using System;
using System.IO;
using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class StartModuleService
    {
        private IProjectConfigProvider configProvider;
        private IFileSystemProvider fileSystem;
        private IPowerShellService powerShell;
        private IProcessService processService;

        public StartModuleService(
            IProjectConfigProvider configProvider,
            IFileSystemProvider fileSystem,
            IPowerShellService powerShell,
            IProcessService processService)
        {
            this.configProvider = configProvider;
            this.fileSystem = fileSystem;
            this.powerShell = powerShell;
            this.processService = processService;
        }


        public IPowerShellModule Execute(BuildConfiguration configuration, bool isLegacy, string targetFramework)
        {
            var path = GetModulePath(configuration, isLegacy, targetFramework);

            if (fileSystem.DirectoryExists(path))
            {
                var exe = GetPowerShellExe(path, isLegacy);

                var psd1 = Path.Combine(path, $"{configProvider.Config.PowerShellModuleName}.psd1");

                if (!fileSystem.FileExists(psd1))
                    throw new FileNotFoundException($"Cannot start {configProvider.Config.Name} as module manifest '{psd1}' does not exist.", psd1);

                if (powerShell.IsWindows)
                {
                    powerShell.WriteColor($"\nLaunching {configProvider.Config.Name} from '{psd1}'\n", ConsoleColor.Green);

                    processService.Execute(exe, new ArgList
                    {
                        "-executionpolicy",
                        "bypass",
                        "-noexit",
                        "-command",
                        $"ipmo '{psd1}'; cd ~"
                    }, shellExecute: true);

                    return null;
                }
                else
                {
                    //Process.Start can't open new windows on Unix platforms and malfunctions when trying to run a nested PowerShell instance,
                    //so just import the module into the current session

                    if (powerShell.GetModule(configProvider.Config.PowerShellModuleName) != null)
                    {
                        var name = configProvider.Config.Name;

                        throw new InvalidOperationException($"Cannot start {name} as {name} is already loaded in the current session. Please reopen the {name} Build Environment and try running {name} again.");
                    }

                    powerShell.WriteColor($"\nImporting {configProvider.Config.Name} from {psd1}", ConsoleColor.Green);

                    var module = powerShell.ImportModule(psd1, true);

                    return module;
                }
            }
            else
            {
                throw new InvalidOperationException($"Cannot start {configProvider.Config.Name}: solution has not been compiled for '{configuration}' build. Path '{path}' does not exist.");
            }
        }

        private string GetPowerShellExe(string path, bool isLegacy)
        {
            if (isLegacy)
                return "powershell";

            if (path.IndexOf("net4", StringComparison.OrdinalIgnoreCase) == -1)
            {
                if (powerShell.GetCommand("pwsh") == null)
                    throw new InvalidOperationException($"Cannot find command 'pwsh' for launching module '{path}'; is PowerShell Core installed?");
            }

            return "pwsh";
        }

        private string GetModulePath(BuildConfiguration configuration, bool isLegacy, string targetFramework)
        {
            string targetFolder;

            if (!isLegacy && !string.IsNullOrWhiteSpace(targetFramework))
            {
                var configDir = configProvider.GetPowerShellConfigurationDirectory(configuration);

                targetFolder = Path.Combine(configDir, targetFramework);

                if (!Directory.Exists(targetFolder))
                {
                    var candidates = fileSystem.EnumerateDirectories(configDir, "net*").ToArray();

                    if (candidates.Length > 0)
                    {
                        var str = string.Join(", ", candidates.Select(Path.GetFileNameWithoutExtension));

                        throw new InvalidOperationException($"Cannot start {configProvider.Config.Name}: target framework '{targetFramework}' does not exist. Please ensure {configProvider.Config.Name} has been compiled for the specified TargetFramework and Configuration. Known target frameworks: {str}.");
                    }

                    throw new InvalidOperationException($"Cannot start {configProvider.Config.Name}: target folder '{targetFolder}' does not exist. Please ensure {configProvider.Config.Name} has been compiled for the specified TargetFramework and Configuration.");
                }

                //The PowerShell project should be inside a folder with the name we want to use for the module
                var moduleDir = Path.Combine(targetFolder, configProvider.Config.PowerShellModuleName);

                if (fileSystem.DirectoryExists(moduleDir))
                    targetFolder = moduleDir;
            }
            else
                targetFolder = configProvider.GetPowerShellOutputDirectory(configuration, isLegacy);

            return targetFolder;
        }
    }
}
