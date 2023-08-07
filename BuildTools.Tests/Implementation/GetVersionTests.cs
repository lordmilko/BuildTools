using System.Collections;
using System.IO;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    [TestClass]
    public class GetVersionTests
    {
        [TestMethod]
        public void GetVersion_Normal()
        {
            var result = Test(false);

            Assert.AreEqual("0.9.0.0", result.Assembly.ToString());
            Assert.AreEqual("0.9.16.0", result.File.ToString());
            Assert.AreEqual("0.9.16", result.Info.ToString());
            Assert.AreEqual("0.9.16", result.Module.ToString());
            Assert.AreEqual("v0.9.16", result.ModuleTag);
            Assert.AreEqual("0.9.16", result.Package.ToString());
            Assert.IsNull(result.PreviousTag);
        }

        [TestMethod]
        public void GetVersion_Legacy()
        {
            var result = Test(true);

            Assert.AreEqual("0.9.0.0", result.Assembly.ToString());
            Assert.AreEqual("0.9.16.0", result.File.ToString());
            Assert.AreEqual("0.9.16", result.Info.ToString());
            Assert.AreEqual("0.9.16", result.Module.ToString());
            Assert.AreEqual("v0.9.16", result.ModuleTag);
            Assert.AreEqual("0.9.16", result.Package.ToString());
            Assert.IsNull(result.PreviousTag);
        }

        private VersionTable Test(bool isLegacy)
        {
            var serviceProvider = new ServiceCollection
            {
                typeof(GetVersionService),

                { typeof(IFileSystemProvider), typeof(MockFileSystemProvider) },
                { typeof(IPowerShellService), typeof(MockPowerShellService) },
                { typeof(IProcessService), typeof(MockProcessService) },

                p => (IProjectConfigProvider) new ProjectConfigProvider(WellKnownConfig.PrtgAPI, "C:\\Root", p.GetService<IFileSystemProvider>(), p.GetService<IPowerShellService>())
            }.Build();

            var fileSystem = (MockFileSystemProvider) serviceProvider.GetService<IFileSystemProvider>();

            //We won't actually use the *.sln file, we're just checking we've got the right root folder
            fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
            fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;
            fileSystem.EnumerateFilesMap[("C:\\Root\\src\\PrtgAPI.PowerShell", "*.psd1", SearchOption.TopDirectoryOnly)] = new[] { "C:\\Root\\src\\PrtgAPI.PowerShell\\PrtgAPI.psd1" };
            fileSystem.ReadFileTextMap["C:\\Root\\src\\PrtgAPI.PowerShell\\PrtgAPI.psd1"] = "@{}";
            fileSystem.DirectoryExistsMap["C:\\Root\\.git"] = true;

            fileSystem.EnumerateFilesMap[("C:\\Root", "*.csproj", SearchOption.AllDirectories)] = new[]
            {
                "C:\\Root\\first\\first.csproj",
                "C:\\Root\\PrtgAPI.PowerShell\\PrtgAPI.PowerShell.csproj",
            };

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

            var powerShell = (MockPowerShellService) serviceProvider.GetService<IPowerShellService>();

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

            var versionService = serviceProvider.GetService<GetVersionService>();

            var result = versionService.GetVersion(isLegacy);

            return result;
        }
    }
}
