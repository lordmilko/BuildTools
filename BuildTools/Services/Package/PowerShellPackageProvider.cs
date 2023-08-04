using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class PowerShellPackageProvider
    {
        private IProjectConfigProvider configProvider;
        private IFileSystemProvider fileSystem;
        private IPowerShellService powerShell;
        private IZipService zip;
        private Logger logger;

        public PowerShellPackageProvider(
            IProjectConfigProvider configProvider,
            IFileSystemProvider fileSystem,
            IPowerShellService powerShell,
            IZipService zip,
            Logger logger)
        {
            this.configProvider = configProvider;
            this.fileSystem = fileSystem;
            this.powerShell = powerShell;
            this.zip = zip;
            this.logger = logger;
        }

        public void Execute(PackageConfig config)
        {
            var moduleDir = configProvider.GetPowerShellOutputDirectory(config.Configuration, config.IsLegacy);
            var configDir = configProvider.GetPowerShellConfigurationDirectory(config.Configuration);

            var outputDir = moduleDir;

            var dll = Path.Combine(outputDir, configProvider.GetPowerShellProjectName() + ".dll");

            if (!fileSystem.FileExists(dll))
                throw new FileNotFoundException($"Cannot build PowerShell package as {configProvider.Config.Name} has not been compiled. Could not find file '{dll}'.", dll);

            //We are either in Release\net461 or Release\net461\FooModule. We want to package up both Release\net461 and Release\netstandard2.0.
            //As such, we must find our way back to the Release folder.
            if (config.IsMultiTargeting)
                outputDir = configDir;

            PackageSourceService.WithTempCopy(
                outputDir,
                fileSystem,
                tempPath =>
                {
                    string packageDir = tempPath;
                    string primaryModuleDir = tempPath;

                    if (config.IsMultiTargeting)
                    {
                        //While we may have multiple target frameworks we want to include, for the purposes of publishing we just move everything into whatever the "main" folder is
                        var relativePath = moduleDir.Substring(configDir.Length).TrimStart(Path.DirectorySeparatorChar);
                        primaryModuleDir = Path.Combine(tempPath, relativePath);

                        UpdateRootModule_MultiTargetedRelease(primaryModuleDir);

                        packageDir = MovePowerShellAssemblies_MultiTargetedRelease(tempPath);
                    }

                    CreateRedistributablePackage(packageDir, config.Target);
                    CreatePowerShellPackage(primaryModuleDir, config.Target);
                }
            );
        }

        private void UpdateRootModule_MultiTargetedRelease(string primaryModuleDir)
        {
            //Cmdlets such as Import-PowerShellDataFile and code inside PSModule.psm1 cannot handle a RootModule being set to a script expression.
            //As such, we must rewrite these strings directly

            var psd1Path = GetPsd1Path(primaryModuleDir);

            var lines = fileSystem.ReadFileLines(psd1Path);

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("RootModule ="))
                {
                    var dllName = configProvider.GetPowerShellProjectName();

                    lines[i] = string.Join(Environment.NewLine, $@"
RootModule = if($PSEdition -eq 'Core')
{{
    'coreclr\{dllName}.dll'
}}
else # Desktop
{{
    'fullclr\{dllName}.dll'
}}".TrimStart().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

                    break;
                }
            }

            fileSystem.WriteFileLines(psd1Path, lines);
        }

        private string GetPsd1Path(string primaryModuleDir)
        {
            if (!fileSystem.DirectoryExists(primaryModuleDir))
                throw new DirectoryNotFoundException($"Could not find module directory '{primaryModuleDir}'");

            var psd1 = Path.Combine(primaryModuleDir, $"{configProvider.Config.PowerShellModuleName}.psd1");

            if (!fileSystem.FileExists(psd1))
                throw new FileNotFoundException($"Could not find psd1 file '{psd1}'", psd1);

            return psd1;
        }

        private void CreateRedistributablePackage(string packageDir, PackageTarget target)
        {
            if (target.Redist)
            {
                var zipFile = Path.Combine(PackageSourceService.RepoLocation, $"{configProvider.Config.PowerShellModuleName}.zip");

                if (fileSystem.FileExists(zipFile))
                    fileSystem.DeleteFile(zipFile);

                zip.CreateFromDirectory(packageDir, zipFile);
            }
        }

        private string MovePowerShellAssemblies_MultiTargetedRelease(string modulePath)
        {
            var netStandardOutput = Path.Combine(modulePath, "netstandard2.0", configProvider.Config.PowerShellModuleName);
            var netFrameworkOutput = Path.Combine(modulePath, "net452", configProvider.Config.PowerShellModuleName); //todo

            var coreclr = Path.Combine(netFrameworkOutput, "coreclr");
            var fullclr = Path.Combine(netFrameworkOutput, "fullclr");

            var include = new[]
            {
                "*.dll",
                "*.json",
                "*.xml",
                "*.pdb",
            };

            string[] getFiles(string path) => include
                .SelectMany(i => fileSystem.EnumerateFiles(path, i, SearchOption.AllDirectories))
                .Where(f => !f.EndsWith("-Help.xml", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var standardFiles = getFiles(netStandardOutput);
            var frameworkFiles = getFiles(netFrameworkOutput);

            fileSystem.CreateDirectory(coreclr);
            fileSystem.CreateDirectory(fullclr);

            foreach (var file in standardFiles)
                fileSystem.MoveFile(file, coreclr);

            foreach (var file in frameworkFiles)
                fileSystem.MoveFile(file, fullclr);

            var primaryProject = configProvider.GetPrimaryProject(false);
            var configDir = configProvider.GetProjectConfigurationDirectory(primaryProject, BuildConfiguration.Release);

            var depsName = $"{configProvider.GetPrimaryProject(false).NormalizedName}.deps.json";

            var depsSourcePath = Path.Combine(configDir, "netstandard2.0", depsName);

            if (!fileSystem.FileExists(depsSourcePath))
                throw new FileNotFoundException($"Could not find '{depsName}' at '{depsSourcePath}'", depsSourcePath);

            var depsDestPath = Path.Combine(coreclr, depsName);

            fileSystem.CopyFile(depsSourcePath, depsDestPath);

            //The coreclr and fullclr files were created under this folder. Everything that didn't need moving into the fullclr
            //folder (e.g. *.cmd files, etc) is still in the root
            return netFrameworkOutput;
        }

        private void CreatePowerShellPackage(string modulePath, PackageTarget target)
        {
            if (target.PowerShell)
            {
                DeleteUnnecessaryFiles(modulePath);

                logger.LogInformation($"\t\tPublishing module to {PackageSourceService.RepoName}");

                powerShell.PublishModule(modulePath);
            }
        }

        private void DeleteUnnecessaryFiles(string modulePath)
        {
            //Remove any files not required in the nupkg

            var list = new List<string>
            {
                "*.cmd",
                "*.pdb",
                "*.sh",
                "*.json"
            };

            foreach (var item in list)
            {
                var matches = fileSystem.EnumerateFiles(modulePath, item, SearchOption.AllDirectories);

                foreach (var match in matches)
                    fileSystem.DeleteFile(match);
            }

            //Delete all XmlDoc files associated with any DLLs published
            var dlls = fileSystem.EnumerateFiles(modulePath, "*.dll", SearchOption.AllDirectories);

            foreach (var dll in dlls)
            {
                var xml = Path.ChangeExtension(dll, ".xml");

                if (fileSystem.FileExists(xml))
                    fileSystem.DeleteFile(xml);
            }
        }
    }
}
