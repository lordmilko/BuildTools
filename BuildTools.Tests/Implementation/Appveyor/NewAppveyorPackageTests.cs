using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests.Implementation
{
    [TestClass]
    public class NewAppveyorPackageTests : BaseTest
    {
        private const string SolutionRoot = "C:\\Root";
        private const string SourceRoot = SolutionRoot + "\\src";

        private const string ChocolateyInstall = "C:\\ProgramData\\chocolatey";
        private const string ChocolateyExe = ChocolateyInstall + "\\bin\\chocolatey.exe";
        private const string NuGetExe = ChocolateyInstall + "\\bin\\nuget.exe";

        private static readonly string rootModule = string.Join(Environment.NewLine, new[]
        {
            "RootModule = if($PSEdition -eq 'Core')",
            "{",
            "    'coreclr\\PrtgAPI.PowerShell.dll'",
            "}",
            "else # Desktop",
            "{",
            "    'fullclr\\PrtgAPI.PowerShell.dll'",
            "}"
        });

        private static readonly string TempOutputPrtgAPI = Path.Combine(PackageSourceService.RepoLocation, "TempOutput", "PrtgAPI");

        #region CSharp

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_CSharp_Legacy_Debug()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorCSharpPackageProvider> appveyorCSharpPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockCSharpPackageProvider(fileSystem, envProvider, powerShell);
                MockVersion(fileSystem, powerShell, true);

                MockTestCSharpPackage(
                    fileSystem,
                    powerShell,
                    processService,

                    ("lib\\net452\\PrtgAPI.dll", false),
                    ("lib\\net452\\PrtgAPI.xml", false)
                );

                appveyorCSharpPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Debug, true, true, null, PackageType.CSharp));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_CSharp_Legacy_Release()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorCSharpPackageProvider> appveyorCSharpPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockCSharpPackageProvider(fileSystem, envProvider, powerShell);
                MockVersion(fileSystem, powerShell, true);

                MockTestCSharpPackage(
                    fileSystem,
                    powerShell,
                    processService,

                    ("lib\\net452\\PrtgAPI.dll", false),
                    ("lib\\net452\\PrtgAPI.xml", false)
                );

                appveyorCSharpPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Release, true, true, null, PackageType.CSharp));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_CSharp_Core_Debug()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorCSharpPackageProvider> appveyorCSharpPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockCSharpPackageProvider(fileSystem, envProvider, powerShell);
                MockVersion(fileSystem, powerShell, false);

                MockTestCSharpPackage(
                    fileSystem,
                    powerShell,
                    processService,

                    ("lib\\netstandard2.0\\PrtgAPI.dll", false),
                    ("lib\\netstandard2.0\\PrtgAPI.xml", false),
                    ("LICENSE", false)
                );

                appveyorCSharpPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Debug, false, true, null, PackageType.CSharp));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_CSharp_Core_Release()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorCSharpPackageProvider> appveyorCSharpPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockCSharpPackageProvider(fileSystem, envProvider, powerShell);
                MockVersion(fileSystem, powerShell, false);

                MockTestCSharpPackage(
                    fileSystem,
                    powerShell,
                    processService,

                    ("lib\\net452\\PrtgAPI.dll", false),
                    ("lib\\net452\\PrtgAPI.xml", false),
                    ("lib\\netstandard2.0\\PrtgAPI.dll", false),
                    ("lib\\netstandard2.0\\PrtgAPI.xml", false),
                    ("LICENSE", false)
                );

                appveyorCSharpPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Release, false, true, null, PackageType.CSharp));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_CSharp_Appveyor()
        {

            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorCSharpPackageProvider> appveyorCSharpPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockCSharpPackageProvider(fileSystem, envProvider, powerShell);
                MockVersion(fileSystem, powerShell, false);

                MockTestCSharpPackage(
                    fileSystem,
                    powerShell,
                    processService,

                    ("lib\\net452\\PrtgAPI.dll", false),
                    ("lib\\net452\\PrtgAPI.xml", false),
                    ("lib\\netstandard2.0\\PrtgAPI.dll", false),
                    ("lib\\netstandard2.0\\PrtgAPI.xml", false),
                    ("LICENSE", false)
                );

                envProvider.SetValue(WellKnownEnvironmentVariable.Appveyor, "1");
                envProvider.SetValue(WellKnownEnvironmentVariable.AppveyorBuildVersion, "0.9.16-build.2");
                fileSystem.EnumerateFilesMap[(PackageSourceService.RepoLocation, "*.*nupkg", SearchOption.TopDirectoryOnly)] = new[]
                {
                    "C:\\temp\\PrtgAPI.0.9.16.nupkg",
                    "C:\\temp\\PrtgAPI.0.9.16.snupkg"
                };

                fileSystem.EnumerateFilesMap[(PackageSourceService.RepoLocation, "*.zip", SearchOption.TopDirectoryOnly)] = new string[0];

                appveyorCSharpPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Release, false, true, null, PackageType.CSharp));
            });
        }

        #endregion
        #region PowerShell

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_PowerShell_Legacy_Debug()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorPowerShellPackageProvider> appveyorPowerShellPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockPowerShellPackageProvider(fileSystem);
                MockVersion(fileSystem, powerShell, true);

                MockTestPowerShellPackage(
                    fileSystem,
                    powerShell,
                    processService,
                    false,

                    ("about_ChannelSettings.help.txt", false),
                    ("about_ObjectSettings.help.txt", false),
                    ("about_PrtgAPI.help.txt", false),
                    ("about_SensorParameters.help.txt", false),
                    ("about_SensorSettings.help.txt", false),
                    ("Functions\\New-Credential.ps1", false),
                    ("PrtgAPI.dll", false),
                    ("PrtgAPI.Format.ps1xml", false),
                    ("PrtgAPI.PowerShell.dll", false),
                    ("PrtgAPI.PowerShell.dll-Help.xml", false),
                    ("PrtgAPI.psd1", false),
                    ("PrtgAPI.psm1", false),
                    ("PrtgAPI.Types.ps1xml", false)
                );

                appveyorPowerShellPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Debug, true, true, null, PackageType.PowerShell));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_PowerShell_Legacy_Release()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorPowerShellPackageProvider> appveyorPowerShellPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockPowerShellPackageProvider(fileSystem);
                MockVersion(fileSystem, powerShell, true);

                MockTestPowerShellPackage(
                    fileSystem,
                    powerShell,
                    processService,
                    false,

                    ("about_ChannelSettings.help.txt", false),
                    ("about_ObjectSettings.help.txt", false),
                    ("about_PrtgAPI.help.txt", false),
                    ("about_SensorParameters.help.txt", false),
                    ("about_SensorSettings.help.txt", false),
                    ("Functions\\New-Credential.ps1", false),
                    ("PrtgAPI.dll", false),
                    ("PrtgAPI.Format.ps1xml", false),
                    ("PrtgAPI.PowerShell.dll", false),
                    ("PrtgAPI.PowerShell.dll-Help.xml", false),
                    ("PrtgAPI.psd1", false),
                    ("PrtgAPI.psm1", false),
                    ("PrtgAPI.Types.ps1xml", false)
                );

                appveyorPowerShellPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Release, true, true, null, PackageType.PowerShell));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_PowerShell_Core_Debug()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorPowerShellPackageProvider> appveyorPowerShellPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockPowerShellPackageProvider(fileSystem);
                MockVersion(fileSystem, powerShell, false);

                MockTestPowerShellPackage(
                    fileSystem,
                    powerShell,
                    processService,
                    false,

                    ("about_ChannelSettings.help.txt", false),
                    ("about_ObjectSettings.help.txt", false),
                    ("about_PrtgAPI.help.txt", false),
                    ("about_SensorParameters.help.txt", false),
                    ("about_SensorSettings.help.txt", false),
                    ("Functions\\New-Credential.ps1", false),
                    ("PrtgAPI.dll", false),
                    ("PrtgAPI.Format.ps1xml", false),
                    ("PrtgAPI.PowerShell.dll", false),
                    ("PrtgAPI.psd1", false),
                    ("PrtgAPI.psm1", false),
                    ("PrtgAPI.Types.ps1xml", false)
                );

                appveyorPowerShellPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Debug, false, true, null, PackageType.PowerShell));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_PowerShell_Core_Release()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorPowerShellPackageProvider> appveyorPowerShellPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockPowerShellPackageProvider(fileSystem);
                MockVersion(fileSystem, powerShell, false);

                MockTestPowerShellPackage(
                    fileSystem,
                    powerShell,
                    processService,
                    false,

                    ("about_ChannelSettings.help.txt", false),
                    ("about_ObjectSettings.help.txt", false),
                    ("about_PrtgAPI.help.txt", false),
                    ("about_SensorParameters.help.txt", false),
                    ("about_SensorSettings.help.txt", false),
                    ("Functions\\New-Credential.ps1", false),
                    ("coreclr\\PrtgAPI.dll", false),
                    ("coreclr\\PrtgAPI.PowerShell.dll", false),
                    ("fullclr\\PrtgAPI.dll", false),
                    ("fullclr\\PrtgAPI.PowerShell.dll", false),
                    ("PrtgAPI.Format.ps1xml", false),
                    ("PrtgAPI.PowerShell.dll-Help.xml", false),
                    ("PrtgAPI.psd1", false),
                    ("PrtgAPI.psm1", false),
                    ("PrtgAPI.Types.ps1xml", false)
                );

                appveyorPowerShellPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Release, false, true, null, PackageType.PowerShell));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_PowerShell_Appveyor()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorPowerShellPackageProvider> appveyorPowerShellPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockPowerShellPackageProvider(fileSystem);
                MockVersion(fileSystem, powerShell, false);

                bool contentsUpdated = false;

                MockTestPowerShellPackage(
                    fileSystem,
                    powerShell,
                    processService,
                    false,

                    new[]
                    {
                        ("about_ChannelSettings.help.txt", false),
                        ("about_ObjectSettings.help.txt", false),
                        ("about_PrtgAPI.help.txt", false),
                        ("about_SensorParameters.help.txt", false),
                        ("about_SensorSettings.help.txt", false),
                        ("Functions\\New-Credential.ps1", false),
                        ("coreclr\\PrtgAPI.dll", false),
                        ("coreclr\\PrtgAPI.PowerShell.dll", false),
                        ("fullclr\\PrtgAPI.dll", false),
                        ("fullclr\\PrtgAPI.PowerShell.dll", false),
                        ("PrtgAPI.Format.ps1xml", false),
                        ("PrtgAPI.PowerShell.dll-Help.xml", false),
                        ("PrtgAPI.psd1", false),
                        ("PrtgAPI.psm1", false),
                        ("PrtgAPI.Types.ps1xml", false)
                    },
                    
                    psd1Contents => (a, b) =>
                    {
                        var psd1Hashtable = (Hashtable) powerShell.InvokeScriptMap[psd1Contents];

                        var cmdletsToExport = (object[])psd1Hashtable["CmdletsToExport"];
                        cmdletsToExport[0] = "Get-Sensor";

                        var aliasesToExport = (object[])psd1Hashtable["AliasesToExport"];
                        aliasesToExport[0] = "Add-Trigger";

                        contentsUpdated = true;
                    }
                );

                fileSystem.EnumerateFilesMap[(PackageSourceService.RepoLocation, "*.*nupkg", SearchOption.TopDirectoryOnly)] = new[]
                {
                    "C:\\temp\\PrtgAPI_PowerShell.0.9.16.nupkg",
                };

                fileSystem.EnumerateFilesMap[(PackageSourceService.RepoLocation, "*.zip", SearchOption.TopDirectoryOnly)] = new string[0];

                envProvider.SetValue(WellKnownEnvironmentVariable.Appveyor, "1");

                appveyorPowerShellPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Release, false, true, null, PackageType.PowerShell));

                Assert.IsTrue(contentsUpdated, "UpdateModuleManifest was not called");
            });
        }

        #endregion
        #region Redist

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_Redist_Legacy_Debug()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorPowerShellPackageProvider> appveyorPowerShellPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockPowerShellPackageProvider(fileSystem);
                MockVersion(fileSystem, powerShell, true);

                MockTestPowerShellPackage(
                    fileSystem,
                    powerShell,
                    processService,
                    true,

                    ("about_ChannelSettings.help.txt", false),
                    ("about_ObjectSettings.help.txt", false),
                    ("about_PrtgAPI.help.txt", false),
                    ("about_SensorParameters.help.txt", false),
                    ("about_SensorSettings.help.txt", false),
                    ("Functions\\New-Credential.ps1", false),
                    ("PrtgAPI.cmd", false),
                    ("PrtgAPI.sh", false),
                    ("PrtgAPI.dll", false),
                    ("PrtgAPI.pdb", false),
                    ("PrtgAPI.xml", false),
                    ("PrtgAPI.Format.ps1xml", false),
                    ("PrtgAPI.PowerShell.dll", false),
                    ("PrtgAPI.PowerShell.pdb", false),
                    ("PrtgAPI.PowerShell.xml", false),
                    ("PrtgAPI.PowerShell.dll-Help.xml", false),
                    ("PrtgAPI.psd1", false),
                    ("PrtgAPI.psm1", false),
                    ("PrtgAPI.Types.ps1xml", false)
                );

                appveyorPowerShellPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Debug, true, true, null, PackageType.Redistributable));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_Redist_Legacy_Release()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorPowerShellPackageProvider> appveyorPowerShellPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockPowerShellPackageProvider(fileSystem);
                MockVersion(fileSystem, powerShell, true);

                MockTestPowerShellPackage(
                    fileSystem,
                    powerShell,
                    processService,
                    true,

                    ("about_ChannelSettings.help.txt", false),
                    ("about_ObjectSettings.help.txt", false),
                    ("about_PrtgAPI.help.txt", false),
                    ("about_SensorParameters.help.txt", false),
                    ("about_SensorSettings.help.txt", false),
                    ("Functions\\New-Credential.ps1", false),
                    ("PrtgAPI.cmd", false),
                    ("PrtgAPI.sh", false),
                    ("PrtgAPI.dll", false),
                    ("PrtgAPI.pdb", false),
                    ("PrtgAPI.xml", false),
                    ("PrtgAPI.Format.ps1xml", false),
                    ("PrtgAPI.PowerShell.dll", false),
                    ("PrtgAPI.PowerShell.pdb", false),
                    ("PrtgAPI.PowerShell.xml", false),
                    ("PrtgAPI.PowerShell.dll-Help.xml", false),
                    ("PrtgAPI.psd1", false),
                    ("PrtgAPI.psm1", false),
                    ("PrtgAPI.Types.ps1xml", false)
                );

                appveyorPowerShellPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Release, true, true, null, PackageType.Redistributable));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_Redist_Core_Debug()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorPowerShellPackageProvider> appveyorPowerShellPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockPowerShellPackageProvider(fileSystem);
                MockVersion(fileSystem, powerShell, false);

                MockTestPowerShellPackage(
                    fileSystem,
                    powerShell,
                    processService,
                    true,

                    ("about_ChannelSettings.help.txt", false),
                    ("about_ObjectSettings.help.txt", false),
                    ("about_PrtgAPI.help.txt", false),
                    ("about_SensorParameters.help.txt", false),
                    ("about_SensorSettings.help.txt", false),
                    ("Functions\\New-Credential.ps1", false),
                    ("PrtgAPI.cmd", false),
                    ("PrtgAPI.sh", false),
                    ("PrtgAPI.dll", false),
                    ("PrtgAPI.pdb", false),
                    ("PrtgAPI.xml", false),
                    ("PrtgAPI.PowerShell.deps.json", false),
                    ("PrtgAPI.Format.ps1xml", false),
                    ("PrtgAPI.PowerShell.dll", false),
                    ("PrtgAPI.PowerShell.pdb", false),
                    ("PrtgAPI.PowerShell.xml", false),
                    //("PrtgAPI.PowerShell.dll-Help.xml", false),
                    ("PrtgAPI.psd1", false),
                    ("PrtgAPI.psm1", false),
                    ("PrtgAPI.Types.ps1xml", false)
                );

                appveyorPowerShellPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Debug, false, true, null, PackageType.Redistributable));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_Redist_Core_Release()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorPowerShellPackageProvider> appveyorPowerShellPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockPowerShellPackageProvider(fileSystem);
                MockVersion(fileSystem, powerShell, false);

                MockTestPowerShellPackage(
                    fileSystem,
                    powerShell,
                    processService,
                    true,

                    ("PrtgAPI.cmd", false),
                    ("PrtgAPI.sh", false),
                    ("about_ChannelSettings.help.txt", false),
                    ("about_ObjectSettings.help.txt", false),
                    ("about_PrtgAPI.help.txt", false),
                    ("about_SensorParameters.help.txt", false),
                    ("about_SensorSettings.help.txt", false),
                    ("Functions\\New-Credential.ps1", false),
                    ("coreclr\\PrtgAPI.dll", false),
                    ("coreclr\\PrtgAPI.pdb", false),
                    ("coreclr\\PrtgAPI.xml", false),
                    ("coreclr\\PrtgAPI.PowerShell.dll", false),
                    ("coreclr\\PrtgAPI.PowerShell.pdb", false),
                    ("coreclr\\PrtgAPI.PowerShell.xml", false),
                    ("fullclr\\PrtgAPI.dll", false),
                    ("fullclr\\PrtgAPI.pdb", false),
                    ("fullclr\\PrtgAPI.xml", false),
                    ("fullclr\\PrtgAPI.PowerShell.dll", false),
                    ("fullclr\\PrtgAPI.PowerShell.pdb", false),
                    ("fullclr\\PrtgAPI.PowerShell.xml", false),
                    ("coreclr\\PrtgAPI.deps.json", false),
                    ("coreclr\\PrtgAPI.PowerShell.deps.json", false),
                    ("PrtgAPI.Format.ps1xml", false),
                    ("PrtgAPI.PowerShell.dll-Help.xml", false),
                    ("PrtgAPI.psd1", false),
                    ("PrtgAPI.psm1", false),
                    ("PrtgAPI.Types.ps1xml", false)
                );

                appveyorPowerShellPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Release, false, true, null, PackageType.Redistributable));
            });
        }

        [TestMethod]
        public void NewAppveyorPackage_PrtgAPI_Redist_Appveyor()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService processService,
                Lazy<AppveyorPowerShellPackageProvider> appveyorPowerShellPackageProvider) =>
            {
                MockSetup(fileSystem, envProvider, powerShell);
                MockPowerShellPackageProvider(fileSystem);
                MockVersion(fileSystem, powerShell, false);

                MockTestPowerShellPackage(
                    fileSystem,
                    powerShell,
                    processService,
                    true,

                    ("PrtgAPI.cmd", false),
                    ("PrtgAPI.sh", false),
                    ("about_ChannelSettings.help.txt", false),
                    ("about_ObjectSettings.help.txt", false),
                    ("about_PrtgAPI.help.txt", false),
                    ("about_SensorParameters.help.txt", false),
                    ("about_SensorSettings.help.txt", false),
                    ("Functions\\New-Credential.ps1", false),
                    ("coreclr\\PrtgAPI.dll", false),
                    ("coreclr\\PrtgAPI.pdb", false),
                    ("coreclr\\PrtgAPI.xml", false),
                    ("coreclr\\PrtgAPI.PowerShell.dll", false),
                    ("coreclr\\PrtgAPI.PowerShell.pdb", false),
                    ("coreclr\\PrtgAPI.PowerShell.xml", false),
                    ("fullclr\\PrtgAPI.dll", false),
                    ("fullclr\\PrtgAPI.pdb", false),
                    ("fullclr\\PrtgAPI.xml", false),
                    ("fullclr\\PrtgAPI.PowerShell.dll", false),
                    ("fullclr\\PrtgAPI.PowerShell.pdb", false),
                    ("fullclr\\PrtgAPI.PowerShell.xml", false),
                    ("coreclr\\PrtgAPI.deps.json", false),
                    ("coreclr\\PrtgAPI.PowerShell.deps.json", false),
                    ("PrtgAPI.Format.ps1xml", false),
                    ("PrtgAPI.PowerShell.dll-Help.xml", false),
                    ("PrtgAPI.psd1", false),
                    ("PrtgAPI.psm1", false),
                    ("PrtgAPI.Types.ps1xml", false)
                );

                envProvider.SetValue(WellKnownEnvironmentVariable.Appveyor, "1");

                fileSystem.EnumerateFilesMap[(PackageSourceService.RepoLocation, "*.*nupkg", SearchOption.TopDirectoryOnly)] = new string[0];

                fileSystem.EnumerateFilesMap[(PackageSourceService.RepoLocation, "*.zip", SearchOption.TopDirectoryOnly)] = new string[0];

                appveyorPowerShellPackageProvider.Value.Execute(new PackageConfig(BuildConfiguration.Release, false, true, null, PackageType.Redistributable));
            });
        }

        #endregion

        private void MockSetup(MockFileSystemProvider fileSystem, MockEnvironmentVariableProvider envProvider, MockPowerShellService powerShell)
        {
            fileSystem.EnumerateFilesMap[(SolutionRoot, "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
            fileSystem.DirectoryExistsMap[SourceRoot] = true;

            fileSystem.EnumerateFilesMap[(SolutionRoot, "*.csproj", SearchOption.AllDirectories)] = new[]
            {
                "C:\\Root\\PrimaryProj\\PrtgAPI.csproj",
                "C:\\Root\\PSProj\\PrtgAPI.PowerShell.csproj",

                "C:\\Root\\PrimaryProj\\PrtgAPIv17.csproj",
                "C:\\Root\\PSProj\\PrtgAPIv17.PowerShell.csproj",
            };

            fileSystem.DirectoryExistsMap["C:\\Root\\PrimaryProj"] = true;
            fileSystem.DirectoryExistsMap["C:\\Root\\PrimaryProj\\bin"] = true;

            fileSystem.DirectoryExistsMap["C:\\Root\\PrimaryProj\\bin\\Debug"] = true;
            fileSystem.DirectoryExistsMap["C:\\Root\\PrimaryProj\\bin\\Release"] = true;

            var powerShellDir = "C:\\Root\\src\\PrtgAPI.PowerShell";
            var binDir = Path.Combine(powerShellDir, "bin");

            fileSystem.DirectoryExistsMap[powerShellDir] = true;
            fileSystem.DirectoryExistsMap[binDir] = true;

            foreach (var config in new[] { "Debug", "Release" })
            {
                var configDir = Path.Combine(binDir, config);
                var moduleDir = Path.Combine(configDir, "PrtgAPI");
                var debugDll = Path.Combine(moduleDir, "PrtgAPI.PowerShell.dll");

                fileSystem.DirectoryExistsMap[configDir] = true;
                fileSystem.DirectoryExistsMap[moduleDir] = true;
                fileSystem.FileExistsMap[debugDll] = true;

                fileSystem.EnumerateDirectoriesMap[(configDir, "net4*", SearchOption.TopDirectoryOnly)] = new[]
                {
                    Path.Combine(configDir, "net452")
                };

                var net452PrtgAPI = Path.Combine(configDir, "net452", "PrtgAPI");
                var net452Dll = Path.Combine(net452PrtgAPI, "PrtgAPI.PowerShell.dll");
                fileSystem.DirectoryExistsMap[net452PrtgAPI] = true;
                fileSystem.FileExistsMap[net452Dll] = true;
            }

            envProvider.SetValue(WellKnownEnvironmentVariable.Appveyor, string.Empty);

            powerShell.IsWindows = true;
        }

        private void MockCSharpPackageProvider(MockFileSystemProvider fileSystem, MockEnvironmentVariableProvider envProvider, MockPowerShellService powerShell)
        {
            fileSystem.DirectoryExistsMap[PackageSourceService.RepoLocation] = true;

            fileSystem.DirectoryExistsMap[ChocolateyInstall] = true;
            fileSystem.FileExistsMap[ChocolateyExe] = true;
            fileSystem.FileExistsMap[NuGetExe] = true;

            envProvider.SetValue(WellKnownEnvironmentVariable.ChocolateyInstall, null);
            envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);

            powerShell.KnownCommands["nuget"] = new MockPowerShellCommand("nuget");
            powerShell.KnownCommands["dotnet"] = new MockPowerShellCommand("dotnet");
        }

        private void MockPowerShellPackageProvider(MockFileSystemProvider fileSystem)
        {
            fileSystem.DirectoryExistsMap[PackageSourceService.RepoLocation] = true;

            fileSystem.FileExistsMap[Path.Combine(PackageSourceService.RepoLocation, "PrtgAPI.zip")] = true;

            var exts = new[]
            {
                "*.cmd",
                "*.pdb",
                "*.sh",
                "*.json"
            };

            foreach (var ext in exts)
                fileSystem.EnumerateFilesMap[(TempOutputPrtgAPI, ext, SearchOption.AllDirectories)] = new string[0];

            //When multi-targeting only
            var tempOutputRelease = Path.Combine(PackageSourceService.RepoLocation, "TempOutput", "Release", "net452", "PrtgAPI");
            fileSystem.DirectoryExistsMap[tempOutputRelease] = true;

            fileSystem.EnumerateFilesMap[(TempOutputPrtgAPI, "*.dll", SearchOption.AllDirectories)] = new string[0];

            var tempOutputPsd1 = Path.Combine(tempOutputRelease, "PrtgAPI.psd1");
            fileSystem.FileExistsMap[tempOutputPsd1] = true;
            fileSystem.ReadFileLinesMap[tempOutputPsd1] = new string[0];
            fileSystem.OnWriteFileLines[tempOutputPsd1] = (_1, _2) => { };

            //Move files
            foreach (var targetFramework in new[] {"netstandard2.0", "net452"})
            {
                var tempOutputTargetFramework = Path.Combine(PackageSourceService.RepoLocation, "TempOutput", "Release", targetFramework);

                var releaseTempOutput = Path.Combine(tempOutputTargetFramework, "PrtgAPI");

                foreach (var ext in new[] { "*.dll", "*.json", "*.xml", "*.pdb" })
                    fileSystem.EnumerateFilesMap[(releaseTempOutput, ext, SearchOption.AllDirectories)] = new string[0];

                if (targetFramework == "netstandard2.0")
                    fileSystem.FileExistsMap["C:\\Root\\PrimaryProj\\bin\\Release\\netstandard2.0\\PrtgAPI.deps.json"] = true;

                if (targetFramework == "net452")
                {
                    foreach (var ext in exts)
                        fileSystem.EnumerateFilesMap[(releaseTempOutput, ext, SearchOption.AllDirectories)] = new string[0];
                }
            }
        }

        private void MockVersion(MockFileSystemProvider fileSystem, MockPowerShellService powerShell, bool isLegacy)
        {
            fileSystem.EnumerateFilesMap[("C:\\Root\\src\\PrtgAPI.PowerShell", "*.psd1", SearchOption.TopDirectoryOnly)] = new[] { "C:\\Root\\src\\PrtgAPI.PowerShell\\PrtgAPI.psd1" };
            fileSystem.ReadFileTextMap["C:\\Root\\src\\PrtgAPI.PowerShell\\PrtgAPI.psd1"] = "@{}";
            fileSystem.DirectoryExistsMap["C:\\Root\\.git"] = true;

            if (isLegacy)
            {
                //Get primary project
                fileSystem.DirectoryExistsMap["C:\\Root\\src\\PrtgAPI"] = true;
                fileSystem.FileExistsMap["C:\\Root\\src\\PrtgAPI\\Properties\\Version.cs"] = true;

                fileSystem.ReadFileLinesMap["C:\\Root\\src\\PrtgAPI\\Properties\\Version.cs"] = new[]
                {
                    "[assembly: AssemblyVersion(\"0.9.0.0\")]",
                    "[assembly: AssemblyFileVersion(\"0.9.16.0\")]",
                    "[assembly: AssemblyInformationalVersion(\"0.9.16\")]"
                };
            }
            else
            {
                fileSystem.FileExistsMap["C:\\Root\\build\\Version.props"] = true;

                fileSystem.ReadFileTextMap["C:\\Root\\build\\Version.props"] = @"
<Project>
  <PropertyGroup>
    <Version>0.9.16</Version>
    <AssemblyVersion>0.9.0.0</AssemblyVersion>
    <FileVersion>0.9.16.0</FileVersion>
    <InformationalVersion>0.9.16</InformationalVersion>
  </PropertyGroup>
</Project>
";
            }

            powerShell.InvokeScriptMap["@{}"] = new Hashtable
            {
                { "ModuleVersion", "0.9.16" },
                { "PrivateData", new Hashtable
                {
                    { "PSData", new Hashtable
                    {
                        { "ReleaseNotes", @"Release Notes: https://github.com/lordmilko/PrtgAPI/releases/tag/v0.9.16

---

PrtgAPI is a C#/PowerShell library that abstracts away the complexity of interfacing with the PRTG Network Monitor HTTP API.
" }
                    } }
                } }
            };

            powerShell.KnownCommands["git"] = new MockPowerShellCommand("git");
        }

        #region CSharp

        private void MockTestCSharpPackage(
            MockFileSystemProvider fileSystem,
            MockPowerShellService powerShell,
            MockProcessService processService,
            params (string relativePath, bool isFolder)[] files)
        {
            fileSystem.EnumerateFilesMap[(PackageSourceService.RepoLocation, "*.nupkg", SearchOption.TopDirectoryOnly)] = new[]
            {
                "C:\\temp\\PrtgAPI.0.9.16.nupkg"
            };

            fileSystem.DirectoryExistsMap["C:\\temp\\PrtgAPI.0.9.16"] = true;

            TestCSharpPackageDefinition(fileSystem);
            MockFilesInPackage(fileSystem, false, files);
            TestCSharpPackageInstalls(fileSystem, powerShell, processService);
        }

        private void TestCSharpPackageDefinition(MockFileSystemProvider fileSystem)
        {
            fileSystem.EnumerateFilesMap[("C:\\temp\\PrtgAPI.0.9.16", "*.nuspec", SearchOption.TopDirectoryOnly)] = new[]
            {
                "C:\\temp\\PrtgAPI.0.9.16\\PrtgAPI.nuspec"
            };

            fileSystem.ReadFileTextMap["C:\\temp\\PrtgAPI.0.9.16\\PrtgAPI.nuspec"] = @"
<package xmlns=""http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd"">
  <metadata>
    <version>0.9.16</version>
    <releaseNotes>Release Notes: https://github.com/lordmilko/PrtgAPI/releases/tag/v0.9.16</releaseNotes>
    <repository type=""git"" url=""https://github.com/lordmilko/PrtgAPI.git"" commit=""2cb75d00390bd10d1b810e2f11097b38dfddcb2f"" />
  </metadata>
</package>";
        }

        private void TestCSharpPackageInstalls(MockFileSystemProvider fileSystem, MockPowerShellService powerShell, MockProcessService processService)
        {
            var packageInstallDir = Path.Combine(PackageSourceService.PackageLocation, "PrtgAPI.0.9.16");

            fileSystem.DirectoryExistsMap[packageInstallDir] = false;
            powerShell.OnInstallPackage["PrtgAPI"] = _ => fileSystem.DirectoryExistsMap[packageInstallDir] = true;
            powerShell.OnUninstallPackage["PrtgAPI"] = _ => fileSystem.DirectoryExistsMap[packageInstallDir] = false;

            var libDir = Path.Combine(packageInstallDir, "lib");

            var targetFrameworks = new[] { "net452" };

            var targetFrameworkDirs = targetFrameworks.Select(v => Path.Combine(libDir, v)).ToArray(); ;

            fileSystem.EnumerateDirectoriesMap[(libDir, "*", SearchOption.TopDirectoryOnly)] = targetFrameworkDirs;

            foreach (var dir in targetFrameworkDirs)
            {
                var dll = Path.Combine(dir, "PrtgAPI.dll");

                fileSystem.FileExistsMap[dll] = true;

                processService.ExecuteMap[$"powershell -command \"Add-Type -Path '{dll}'; [PrtgAPI.AuthMode]::Password\""] = new[] { "Password" };
            }
        }

        #endregion
        #region PowerShell

        private void MockTestPowerShellPackage(
            MockFileSystemProvider fileSystem,
            MockPowerShellService powerShell,
            MockProcessService processService,
            bool isRedist,
            params (string relativePath, bool isFolder)[] files) =>
            MockTestPowerShellPackage(fileSystem, powerShell, processService, isRedist, files, null);

        private void MockTestPowerShellPackage(
            MockFileSystemProvider fileSystem,
            MockPowerShellService powerShell,
            MockProcessService processService,
            bool isRedist,
            (string relativePath, bool isFolder)[] files,
            Func<string, Action<string, string>> onModuleManifest)
        {
            fileSystem.EnumerateFilesMap[(PackageSourceService.RepoLocation, "*.nupkg", SearchOption.TopDirectoryOnly)] = new[]
            {
                "C:\\temp\\PrtgAPI.0.9.16.nupkg"
            };

            fileSystem.DirectoryExistsMap["C:\\temp\\PrtgAPI.0.9.16"] = true;
            fileSystem.DirectoryExistsMap[Path.Combine(PackageSourceService.RepoLocation, "PrtgAPI")] = true;

            TestPowerShellPackageDefinition(fileSystem, powerShell, isRedist, onModuleManifest);
            MockFilesInPackage(fileSystem, isRedist, files);
            TestPowerShellPackageInstalls(fileSystem, processService);
        }

        private void TestPowerShellPackageDefinition(
            MockFileSystemProvider fileSystem,
            MockPowerShellService powerShell,
            bool isRedist,
            Func<string, Action<string, string>> onModuleManifest = null)
        {
            var parentDir = "C:\\temp\\PrtgAPI.0.9.16";

            if (isRedist)
                parentDir = Path.Combine(PackageSourceService.RepoLocation, "PrtgAPI");

            var psd1Path = Path.Combine(parentDir, "PrtgAPI.psd1");

            fileSystem.FileExistsMap[psd1Path] = true;

            var psd1Contents = $@"{{
{rootModule}
}}";
            fileSystem.ReadFileTextMap[psd1Path] = psd1Contents;

            powerShell.InvokeScriptMap[psd1Contents] = new Hashtable
            {
                { "ModuleVersion", "0.9.16" },
                { "PrivateData", new Hashtable
                {
                    { "PSData", new Hashtable
                    {
                        { "ReleaseNotes", @"Release Notes: https://github.com/lordmilko/PrtgAPI/releases/tag/v0.9.16

---

PrtgAPI is a C#/PowerShell library that abstracts away the complexity of interfacing with the PRTG Network Monitor HTTP API.
" }
                    } }
                } },
                { "CmdletsToExport", new[]{"*"} },
                { "AliasesToExport", new[]{"*"} }
            };

            if (onModuleManifest != null)
                powerShell.OnUpdateModuleManifest[psd1Path] = onModuleManifest(psd1Contents);

            //Release
            fileSystem.FileExistsMap["C:\\temp\\PrtgAPI.0.9.16\\fullclr\\PrtgAPI.PowerShell.dll"] = true;
        }

        private void TestPowerShellPackageInstalls(MockFileSystemProvider fileSystem, MockProcessService processService)
        {
            var modules = new[]
            {
                "PrtgAPI",
                Path.Combine(PackageSourceService.RepoLocation, "PrtgAPI")
            };

            foreach (var module in modules)
            {
                foreach (var exe in new[] { "powershell", "pwsh" })
                {
                    processService.ExecuteMap[$"{exe} -command \"&{{ import-module '{module}'; try {{ Get-Sensor }} catch [exception] {{ $_.exception.message }} }}\""] =
                        new[] { "You are not connected to a PRTG Server. Please connect first using Connect-PrtgServer." };

                    processService.ExecuteMap[$"{exe} -command \"&{{ import-module '{module}'; (New-Credential a b).ToString() }}\""] =
                        new[] { "System.Management.Automation.PSCredential" };
                }
            }
        }

        #endregion

        private void MockFilesInPackage(MockFileSystemProvider fileSystem, bool isRedist, (string relativePath, bool isFolder)[] files)
        {
            var itemsInPackage = new List<(string relativePath, bool isFolder)>
            {
                ("PrtgAPI.nuspec", false),
                ("[Content_Types].xml", false),
                ("_rels\\.rels", false),
                ("package\\foo", false)
            };

            if (isRedist)
                itemsInPackage.Clear();

            itemsInPackage.AddRange(files);

            var parentFolder = "C:\\temp\\PrtgAPI.0.9.16";

            if (isRedist)
                parentFolder = Path.Combine(PackageSourceService.RepoLocation, "PrtgAPI");

            fileSystem.EnumerateDirectoryFileSystemEntriesMap[(parentFolder, "*", SearchOption.AllDirectories)] =
                itemsInPackage.Select(v => Path.Combine(parentFolder, v.relativePath)).ToArray();

            foreach (var item in itemsInPackage)
                fileSystem.DirectoryExistsMap[Path.Combine(parentFolder, item.relativePath)] = item.isFolder;
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(GetVersionService),
                typeof(NewAppveyorPackageService),
                typeof(NewPackageService),

                typeof(EnvironmentService),
                typeof(Logger),

                typeof(DependencyProvider),

                typeof(DotnetDependencyInstaller),
                typeof(ChocolateyDependencyInstaller),
                typeof(PSPackageDependencyInstaller),
                typeof(PSPackageProviderDependencyInstaller),
                typeof(TargetingPackDependencyInstaller),

                typeof(AppveyorPackageProviderServices),
                typeof(AppveyorCSharpPackageProvider),
                typeof(AppveyorPowerShellPackageProvider),
                typeof(PowerShellPackageSourceService),

                typeof(CSharpPackageProvider),
                typeof(PowerShellPackageProvider),
                typeof(CSharpPackageSourceService),

                { typeof(IConsoleLogger), typeof(MockConsoleLogger) },
                { typeof(IFileLogger), typeof(MockFileLogger) },

                { typeof(IAlternateDataStreamService), typeof(MockAlternateDataStreamService) },
                { typeof(ICommandService), typeof(MockCommandService) },
                { typeof(IEnvironmentVariableProvider), typeof(MockEnvironmentVariableProvider) },
                { typeof(IFileSystemProvider), typeof(MockFileSystemProvider) },
                { typeof(IHasher), typeof(MockHasher) },
                { typeof(IPowerShellService), typeof(MockPowerShellService) },
                { typeof(IProcessService), typeof(MockProcessService) },
                { typeof(IWebClient), typeof(MockWebClient) },
                { typeof(IZipService), typeof(MockZipService) },

                p => (IProjectConfigProvider) new ProjectConfigProvider(WellKnownConfig.PrtgAPI, "C:\\Root", p.GetService<IFileSystemProvider>())
            };
        }
    }
}
