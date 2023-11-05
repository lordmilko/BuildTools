using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    [TestClass]
    [DoNotParallelize] //We're messing with the global service provider
    public class NewBuildEnvironmentTests : BaseTest
    {
        [TestMethod]
        public void NewBuildEnvironment_DynamicParameters_String() => Test("Name", "test");

        [TestMethod]
        public void NewBuildEnvironment_DynamicParameters_Array() => Test("Features", new[] { Feature.Build, Feature.Coverage });

        [TestMethod]
        public void NewBuildEnvironment_DynamicParameters_ScriptBlock()
        {
            /* There's a weird issue with how ScriptBlock is parsed, wherein the outer ScriptBlock won't have quotes around it, but the inner one will. BuildTools stringifies
             * the ScriptBlock by using the Ast, but when we try and compare ScriptBlock equality in our unit tests, we get in trouble because our fake block has braces surrounding
             * the block in both the ScriptBlock _and_ ScriptBlockAst. Evidently, this is due to the fact that ScriptBlock.Create() returns a ScriptBlock that actually contains another
             * ScriptBlock. We can't just eschew the braces however, or the ScriptBlockAst won't have braces either! As such, we create the ScriptBlock with braces, and then extract the
             * inner ScriptBlock that has a ScriptBlock without braces and a ScriptBlockAst with braces, as expected */
            var sb = ScriptBlock.Create("{ $_.DirectoryName -notlike \"*Infrastructure\\Build*\" -and $_.BaseName -ne \"Solution.Tests\" }").Ast
                .FindAll(v => v is ScriptBlockAst, true)
                .Cast<ScriptBlockAst>()
                .Last()
                .GetScriptBlock();

            Test("PowerShellUnitTestFilter", sb);
        }

        [TestMethod]
        public void NewBuildEnvironment_DynamicParameters_Hashtable()
        {
            Test("PackageFiles", new Hashtable
            {
                { "PowerShell", new[]{"a", "b"} }
            });
        }

        [TestMethod]
        public void NewBuildEnvironment_DynamicParameters_NegatedEnum() => Test("Features", "~coverage", "~Coverage");

        private void Test(string property, object raw, object expected = null)
        {
            expected = expected ?? raw;

            Test((IServiceProvider serviceProvider, MockFileSystemProvider fileSystem) =>
            {
                Action<string, string> empty = (a, b) => { };

                fileSystem.DirectoryExistsMap["C:\\Root"] = true;
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "C:\\Root\\PrtgAPI.sln" };
                fileSystem.DirectoryExistsMap["C:\\Root\\.git"] = false;
                fileSystem.DirectoryExistsMap["C:\\Root\\build"] = true;
                fileSystem.EnumerateFilesMap[("C:\\Root\\build", "*.psd1", SearchOption.TopDirectoryOnly)] = Array.Empty<string>();

                var ignored = new[]
                {
                    "appveyor.yml",
                    "build.cmd",
                    "build.sh",
                    "build\\Bootstrap.ps1",
                    "build\\Version.props"
                };

                foreach (var item in ignored)
                {
                    fileSystem.FileExistsMap[$"C:\\Root\\{item}"] = false;
                    fileSystem.OnWriteFileText[$"C:\\Root\\{item}"] = empty;
                }

                var invoker = new PowerShellInvoker();

                string storedContents = null;

                fileSystem.FileExistsMap[$"C:\\Root\\build\\Build.psd1"] = false;
                fileSystem.OnWriteFileText[$"C:\\Root\\build\\Build.psd1"] = (path, contents) => storedContents = contents;

                BuildToolsSessionState.HeadlessUI = true;

                try
                {
                    BuildToolsSessionState.ServiceProviderHook = isGlobal => serviceProvider;

                    var parameters = new Dictionary<string, object>
                    {
                        { "Path", "C:\\Root" },
                        { property, raw }
                    };

                    invoker.Invoke<object>("New-BuildEnvironment", parameters);

                    var hashtable = invoker.InvokeScript<Hashtable>(storedContents).Single();

                    Assert.IsTrue(hashtable.ContainsKey(property), $"Hashtable did not contain property '{property}'.");

                    var value = hashtable[property];

                    AssertEx.AreEquivalent(expected, value);
                }
                finally
                {
                    BuildToolsSessionState.ServiceProviderHook = null;
                }
            });
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(NewBuildEnvironmentService),

                { typeof(IFileSystemProvider), typeof(MockFileSystemProvider) },
                { typeof(IPowerShellService), typeof(MockPowerShellService) },
                { typeof(IProcessService), typeof(MockProcessService) }
            };
        }
    }
}
