using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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

        public void Execute(bool isLegacy, BuildConfiguration configuration, PackageTarget target)
        {
            var outputDir = configProvider.GetPowerShellOutputDirectory(configuration, isLegacy);

            var dll = Path.Combine(outputDir, configProvider.GetPowerShellProjectName() + ".dll");

            if (!fileSystem.FileExists(dll))
                throw new FileNotFoundException($"Cannot build PowerShell package as PrtgAPI has not been compiled. Could not find file '{dll}'.", dll);

            PackageSourceService.WithTempCopy(
                outputDir,
                fileSystem,
                tempPath =>
                {
                    UpdateRootModule(tempPath, isLegacy, configuration);
                    CreateRedistributablePackage(tempPath, isLegacy, configuration, target);

                    if (target.PowerShell)
                    {
                        var modulePath = tempPath;
                        if (!isLegacy && configuration == BuildConfiguration.Release)
                            modulePath = Path.Combine(tempPath, "net452", configProvider.Config.PowerShellModuleName);

                        CreatePowerShellPackage(modulePath);
                    }
                },
                configProvider.Config.PowerShellModuleName
            );
        }

        private void UpdateRootModule(string tempPath, bool isLegacy, BuildConfiguration configuration)
        {
            if (!isLegacy && configuration == BuildConfiguration.Release)
            {
                //Cmdlets such as Import-PowerShellDataFile and code inside PSModule.psm1 cannot handle a RootModule being set to a script expression.
                //As such, we must rewrite these strings directly

                var psd1Path = GetPsd1Path(tempPath, isLegacy);

                var lines = fileSystem.GetFileLines(psd1Path);

                for (var i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("RootModule ="))
                    {
                        lines[i] = string.Join(Environment.NewLine, $@"
RootModule = if($PSEdition -eq 'Core')
{{
    'coreclr\PrtgAPI.PowerShell.dll'
}}
else # Desktop
{{
    'fullclr\PrtgAPI.PowerShell.dll'
}}".TrimStart().Split(new[]{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries));
                    }
                }

                fileSystem.WriteFileLines(psd1Path, lines);
            }
        }

        private string GetPsd1Path(string tempPath, bool isLegacy)
        {
            string moduleDir = Path.Combine(tempPath, configProvider.Config.PowerShellModuleName);

            if (!isLegacy)
            {
                var frameworkDir = Path.Combine(tempPath, "net452");

                if (!fileSystem.DirectoryExists(frameworkDir))
                    throw new DirectoryNotFoundException($"Could not find framework directory '{frameworkDir}'");

                moduleDir = Path.Combine(frameworkDir, configProvider.Config.PowerShellModuleName);
            }

            if (!fileSystem.DirectoryExists(moduleDir))
                throw new DirectoryNotFoundException($"Could not find module directory '{moduleDir}'");

            var psd1 = Path.Combine(moduleDir, $"{configProvider.Config.PowerShellModuleName}.psd1");

            if (!fileSystem.FileExists(psd1))
                throw new FileNotFoundException($"Could not find psd1 file '{psd1}'", psd1);

            return psd1;
        }

        private void CreateRedistributablePackage(string modulePath, bool isLegacy, BuildConfiguration buildConfiguration, PackageTarget target)
        {
            var packageDir = MovePowerShellAssemblies(modulePath, isLegacy, buildConfiguration);

            if (target.Redist)
            {
                var zipFile = Path.Combine(PackageSourceService.RepoLocation, $"{configProvider.Config.PowerShellModuleName}.zip");

                if (fileSystem.FileExists(zipFile))
                    fileSystem.DeleteFile(zipFile);

                zip.CreateFromDirectory(packageDir, zipFile);
            }
        }

        private string MovePowerShellAssemblies(string modulePath, bool isLegacy, BuildConfiguration buildConfiguration)
        {
            if (!isLegacy && buildConfiguration == BuildConfiguration.Release)
            {
                var netStandardOutput = Path.Combine(modulePath, "netstandard2.0", configProvider.Config.PowerShellModuleName);
                var netFrameworkOutput = Path.Combine(modulePath, "net452", configProvider.Config.PowerShellModuleName);

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

                //The coreclr and fullclr files were created under this folder. Everything that didn't need moving into the fullclr
                //folder (e.g. *.cmd files, etc) is still in the root
                return netFrameworkOutput;
            }

            return modulePath;
        }

        private void CreatePowerShellPackage(string modulePath)
        {
            DeleteUnnecessaryFiles(modulePath);

            logger.LogInformation($"\t\tPublishing module to {PackageSourceService.RepoName}");

            powerShell.PublishModule(modulePath);
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