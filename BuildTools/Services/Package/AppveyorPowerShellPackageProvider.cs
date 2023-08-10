using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using BuildTools.PowerShell;

namespace BuildTools
{
    class AppveyorPowerShellPackageProvider : AppveyorPackageProvider
    {
        private readonly PowerShellPackageProvider powerShellPackageProvider;
        private readonly PowerShellPackageSourceService powerShellPackageSourceService;

        public AppveyorPowerShellPackageProvider(
            PowerShellPackageProvider powerShellPackageProvider,
            PowerShellPackageSourceService powerShellPackageSourceService,
            AppveyorPackageProviderServices services) : base(services)
        {
            this.powerShellPackageProvider = powerShellPackageProvider;
            this.powerShellPackageSourceService = powerShellPackageSourceService;
        }

        public void Execute(PackageConfig config)
        {
            logger.LogSubHeader("\tProcessing PowerShell package");

            powerShellPackageSourceService.Install();

            if (environmentService.IsAppveyor)
            {
                var outputDir = configProvider.GetPowerShellOutputDirectory(config.Configuration, config.IsLegacy);

                var psd1 = Path.Combine(outputDir, $"{configProvider.Config.PowerShellModuleName}.psd1");
                powerShell.UpdateModuleManifest(psd1);
            }

            powerShellPackageProvider.Execute(config);

            if (config.Target.PowerShell)
                TestPackage(config);

            if (config.Target.Redist)
                TestRedistributablePackage(config);

            MoveAppveyorPackages("_PowerShell");

            powerShellPackageSourceService.Uninstall();
        }

        #region PowerShell

        protected override void TestPackageDefinition(PackageConfig config, string extractFolder)
        {
            logger.LogInformation("\t\t\tValidating package definition");

            var psd1Path = Path.Combine(extractFolder, $"{configProvider.Config.PowerShellModuleName}.psd1");

            if (!fileSystem.FileExists(psd1Path))
                throw new FileNotFoundException($"Could not find file '{psd1Path}' in extracted package folder", psd1Path);

            if (config.IsMultiTargeting)
            {
                TestPsd1RootModule(config, psd1Path);

                //Dynamic expression on RootModule for checking the PSEdition cannot be parsed by
                //Import-PowerShellDataFile; as such, we need to remove this property

                var projectName = configProvider.GetPowerShellProjectName();

                var fullModule = $"fullclr\\{projectName}.dll";
                var coreModule = $"coreclr\\{projectName}.dll";

                var rootModule = coreModule;

                if (powerShell.Edition == PSEdition.Desktop && fileSystem.FileExists(Path.Combine(extractFolder, fullModule)))
                    rootModule = fullModule;

                powerShell.UpdateModuleManifest(psd1Path, rootModule);
            }

            var psd1Contents = fileSystem.ReadFileText(psd1Path);
            var psd1Hashtable = (Hashtable) powerShell.InvokeAndUnwrap(psd1Contents);

            var version = getVersionService.GetVersion(config.IsLegacy).File.ToString(3);

            var expectedUrl = $"https://github.com/lordmilko/{configProvider.Config.Name}/releases/tag/v{version}";

            var privateData = (Hashtable) psd1Hashtable["PrivateData"];

            if (privateData == null)
                throw new InvalidOperationException("Could not find PrivateData section in psd1 file.");

            var psData = (Hashtable) privateData["PSData"];

            if (psData == null)
                throw new InvalidOperationException("Could not find PSData section in psd1 file.");

            var releaseNotes = (string) psData["ReleaseNotes"];

            if (string.IsNullOrWhiteSpace(releaseNotes))
                throw new InvalidOperationException("Could not find ReleaseNotes section in psd1 file.");

            if (!releaseNotes.Contains(expectedUrl))
                throw new InvalidOperationException($"Release notes did not contain correct release version. Expected notes to contain URL '{expectedUrl}'. Release notes were '{releaseNotes}'");

            if (environmentService.IsAppveyor)
            {
                var cmdletsToExport = psd1Hashtable["CmdletsToExport"];
                var aliasesToExport = psd1Hashtable["AliasesToExport"];

                var exportTests = configProvider.Config.PackageTests.PowerShell.OfType<PSExportPackageTest>().ToArray();

                if (exportTests.Length == 0)
                    throw new InvalidOperationException($"No tests of type {nameof(PSExportPackageTest)} were specified for PowerShell tests. Add a {nameof(ProjectConfig.PackageTests)} item in the form '{{ command = \"Get-Foo\"; kind = \"cmdletexport\"/\"aliasexport\" }}'");

                foreach (var test in exportTests)
                {
                    switch (test.Type)
                    {
                        case CommandTypes.Cmdlet:
                            test.Test(cmdletsToExport);
                            break;

                        case CommandTypes.Alias:
                            test.Test(aliasesToExport);
                            break;

                        default:
                            throw new NotImplementedException($"Don't know how to handle '{nameof(CommandTypes)}' type '{test.Type}'.");
                    }
                }
            }
        }

        private void TestPsd1RootModule(PackageConfig config, string psd1Path)
        {
            if (config.IsMultiTargeting)
            {
                var contents = fileSystem.ReadFileText(psd1Path);

                var projectName = configProvider.GetPowerShellProjectName();

                var expected = string.Join(Environment.NewLine, new[]
                {
                    "RootModule = if($PSEdition -eq 'Core')",
                    "{",
                    $"    'coreclr\\{projectName}.dll'",
                    "}",
                    "else # Desktop",
                    "{",
                    $"    'fullclr\\{projectName}.dll'",
                    "}"
                });

                if (!contents.Contains(expected))
                    throw new InvalidOperationException($"'{psd1Path}' did not contain correct RootModule for Release build");
            }
        }

        protected override void TestPackageContents(PackageConfig config, string extractFolder)
        {
            var powerShellFiles = configProvider.Config.PackageFiles.PowerShell;

            var context = new PackageFileContext(config.Configuration, config.IsLegacy, configProvider.Config.PowerShellMultiTargeted, configProvider.Config.DebugTargetFramework);
            var required = powerShellFiles.Where(v => v.Condition == null || v.Condition(context)).Select(v => v.Name).ToArray();

            TestPackageContents(extractFolder, required);
        }

        protected override void TestPackageInstalls(PackageConfig config)
        {
            logger.LogInformation("\t\t\tInstalling Package");

            if (config.IsMultiTargeting)
            {
                TestPowerShellPackageInstallsHidden(PSEdition.Desktop);
                TestPowerShellPackageInstallsHidden(PSEdition.Core);
            }
            else
            {
                TestPowerShellPackageInstallsHidden(powerShell.Edition);
            }
        }

        private void TestPowerShellPackageInstallsHidden(PSEdition edition)
        {
            logger.LogInformation($"\t\t\t\tTesting package installs on {edition}");

            HideModule(edition, () =>
            {
                var moduleName = configProvider.Config.PowerShellModuleName;

                if (!InstallEditionPackage(edition, moduleName, source: PackageSourceService.RepoName, allowClobber: true))
                    throw new InvalidOperationException($"{moduleName} did not install properly");

                logger.LogInformation("\t\t\t\t\tTesting Package cmdlets");

                try
                {
                    TestPowerShellPackageInstallsInternal(edition, moduleName);
                }
                finally
                {
                    logger.LogInformation("\t\t\t\t\tUninstalling Package");

                    if (!UninstallEditionPackage(edition, moduleName))
                        throw new InvalidOperationException($"{moduleName} did not uninstall properly");
                }
            });
        }

        private void TestPowerShellPackageInstallsInternal(PSEdition edition, string module)
        {
            var exe = PSPackageTest.GetPowerShellExecutable(edition);

            logger.LogInformation($"\t\t\t\t\t\tValidating '{exe}' cmdlet output");

            var tests = configProvider.Config.PackageTests.PowerShell.OfType<PSCommandPackageTest>().ToArray();

            if (tests.Length == 0)
                throw new InvalidOperationException($"No tests of type {nameof(PSCommandPackageTest)} were specified for PowerShell tests");

            foreach (var test in tests)
                test.Test(processService, edition, module);
        }

        private IPowerShellModule[] GetEditionModules(PSEdition edition)
        {
            var moduleName = configProvider.Config.PowerShellModuleName;

            if (powerShell.Edition == edition)
            {
                return powerShell.GetInstalledModules(moduleName);
            }
            else
            {
                var response = InvokeEdition(
                    edition,
                    $"Get-Module {moduleName} -ListAvailable | foreach {{ $_.Name + '|' + $_.Path + '|' + $_.Version }}"
                );

                return response.Select(v =>
                {
                    var split = v.Split('|');

                    return (IPowerShellModule) new RemotePowerShellModule(split[0], split[1], Version.Parse(split[2]));
                }).ToArray();
            }
        }

        private bool InstallEditionPackage(PSEdition edition, string name, string source, bool allowClobber)
        {
            if (powerShell.Edition == edition)
            {
                powerShell.InstallPackage(
                    name,
                    source: source,
                    allowClobber: allowClobber
                );

                return true;
            }
            else
            {
                InvokeEdition(
                    edition,
                    $"Install-Package {name} -Source '{source}' -AllowClobber:{(allowClobber ? "$true" : "$false")}"
                );

                return true;
            }
        }

        private bool UninstallEditionPackage(PSEdition edition, string name)
        {
            if (powerShell.Edition == edition)
            {
                powerShell.UninstallPackage(name);

                return true;
            }
            else
            {
                InvokeEdition(
                    edition,
                    $"Uninstall-Package {name}"
                );

                return true;
            }
        }

        private string[] InvokeEdition(PSEdition edition, string command)
        {
            if (powerShell.Edition == edition)
                throw new InvalidOperationException($"Cannot invoke command '{command}' in edition '{edition}': edition is the same as the currently running process");

            var exe = PSPackageTest.GetPowerShellExecutable(edition);

            var result = processService.Execute(exe, $"-command \"{command}\"");

            return result;
        }

        private void HideModule(PSEdition edition, Action action)
        {
            var moduleName = configProvider.Config.PowerShellModuleName;

            powerShell.GetInstalledModules(moduleName);

            var modules = GetEditionModules(edition);
            bool hidden = false;

            try
            {
                if (modules != null && modules.Length > 0)
                {
                    hidden = true;

                    logger.LogInformation("\t\t\t\t\tRenaming module info files");

                    foreach (var module in modules)
                    {
                        //Rename the module info file so the package manager doesn't find it even inside
                        //the renamed folder

                        var dir = Path.GetDirectoryName(module.Path);
                        var info = Path.Combine(dir, "PSGetModuleInfo.xml");
                        var bakInfo = Path.Combine(dir, "PSGetModuleInfo_bak.xml");

                        if (fileSystem.FileExists(info))
                            fileSystem.MoveFile(info, bakInfo);
                    }

                    logger.LogInformation("\t\t\t\t\tRenaming module directories");

                    foreach (var module in modules)
                    {
                        var path = GetModuleFolder(module);

                        //Check if we haven't already renamed the folder as part of a previous module in the list
                        if (fileSystem.DirectoryExists(path))
                        {
                            var parent = Path.GetDirectoryName(path);
                            var bak = Path.Combine(parent, $"{moduleName}_bak");

                            try
                            {
                                var attempts = 10;

                                for (var i = 0; i < attempts; i++)
                                {
                                    //GetInstalledModules will lock the directory for a small amount of time after it is called

                                    try
                                    {
                                        Thread.Sleep(500);
                                        fileSystem.MoveDirectory(path, bak);
                                        break;
                                    }
                                    catch (IOException)
                                    {
                                        if (i == attempts - 1)
                                            throw;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException($"{path} could not be renamed to $'{moduleName}_bak' properly: {ex.Message}");
                            }

                            if (fileSystem.DirectoryExists(path))
                                throw new InvalidOperationException($"{path} did not rename properly");
                        }
                    }
                }

                logger.LogInformation("\t\t\t\t\tInvoking action");

                action();
            }
            finally
            {
                if (hidden)
                {
                    logger.LogInformation("\t\t\t\t\tRestoring module directories");

                    foreach (var module in modules)
                    {
                        var path = GetModuleFolder(module);

                        var parent = Path.GetDirectoryName(path);
                        var bak = Path.Combine(parent, $"{moduleName}_bak");

                        //Check if we haven't already renamed the folder as part of a previous module in the list
                        if (fileSystem.DirectoryExists(bak))
                            fileSystem.MoveDirectory(bak, path);
                    }

                    logger.LogInformation("\t\t\t\t\tRestoring module info files");

                    foreach (var module in modules)
                    {
                        var dir = Path.GetDirectoryName(module.Path);
                        var info = Path.Combine(dir, "PSGetModuleInfo.xml");
                        var bakInfo = Path.Combine(dir, "PSGetModuleInfo_bak.xml");

                        if (fileSystem.FileExists(bakInfo))
                            fileSystem.MoveFile(bakInfo, info);
                    }
                }
            }
        }

        private string GetModuleFolder(IPowerShellModule module)
        {
            var dir = Path.GetDirectoryName(module.Path);

            var dirName = Path.GetFileName(dir);

            if (dirName == module.Version.ToString())
                return Path.GetDirectoryName(dir);

            return dir;
        }

        #endregion
        #region Redist

        private void TestRedistributablePackage(PackageConfig config)
        {
            logger.LogInformation("\t\tProcessing Redistributable package");

            var zipPath = Path.Combine(PackageSourceService.RepoLocation, $"{configProvider.Config.PowerShellModuleName}.zip");

            ExtractPackage(zipPath, extractFolder =>
            {
                var psd1Path = Path.Combine(extractFolder, $"{configProvider.Config.PowerShellModuleName}.psd1");

                TestPsd1RootModule(config, psd1Path);
                TestRedistributablePackageContents(config, extractFolder);
                TestRedistributableModuleInstalls(config, extractFolder);
            });
        }

        private void TestRedistributablePackageContents(PackageConfig config, string extractFolder)
        {
            var redistFiles = configProvider.Config.PackageFiles.Redist;

            var context = new PackageFileContext(config.Configuration, config.IsLegacy, configProvider.Config.PowerShellMultiTargeted, configProvider.Config.DebugTargetFramework);
            var required = redistFiles.Where(v => v.Condition == null || v.Condition(context)).Select(v => v.Name).ToArray();

            TestPackageContents(extractFolder, required);
        }

        private void TestRedistributableModuleInstalls(PackageConfig config, string extractFolder)
        {
            if (config.IsMultiTargeting)
            {
                TestPowerShellPackageInstallsInternal(PSEdition.Desktop, extractFolder);
                TestPowerShellPackageInstallsInternal(PSEdition.Core, extractFolder);
            }
            else
            {
                TestPowerShellPackageInstallsInternal(powerShell.Edition, extractFolder);
            }
        }

        #endregion
    }
}
