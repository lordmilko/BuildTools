using System;
using System.Collections;
using System.Linq;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    [TestClass]
    public class UpdateBuildManifest : BaseTest
    {
        [TestMethod]
        public void UpdateBuildManifest_NegatedEnum() =>
            Test(v => v["Features"] = "~Coverage", v => Assert.AreEqual("~Coverage", (string)v["Features"]));

        private void Test(Action<Hashtable> modifyInitial, Action<Hashtable> verifyFinal)
        {
            Test((IServiceProvider serviceProvider, MockFileSystemProvider fileSystem, MockPowerShellService powerShell) =>
            {
                fileSystem.FileExistsMap["C:\\Root\\build\\Build.psd1"] = true;
                fileSystem.ReadFileTextMap["C:\\Root\\build\\Build.psd1"] = "@{}";

                var initialHashtable = new Hashtable
                {
                    { "Name", "PrtgAPI" }
                };

                modifyInitial?.Invoke(initialHashtable);

                powerShell.InvokeScriptMap["@{}"] = initialHashtable;

                string newContents = string.Empty;

                fileSystem.OnWriteFileText["C:\\Root\\build\\Build.psd1"] = (path, contents) => newContents = contents;

                var invoker = new PowerShellInvoker();

                BuildToolsSessionState.HeadlessUI = true;

                try
                {
                    BuildToolsSessionState.ServiceProviderHook = isGlobal => serviceProvider;

                    invoker.Invoke<object>("Update-BuildManifest", new { Path = "C:\\Root\\build\\Build.psd1" });

                    var hashtable = invoker.InvokeScript<Hashtable>(newContents).Single();

                    verifyFinal?.Invoke(hashtable);
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
                typeof(UpdateBuildManifestService),

                { typeof(IFileSystemProvider), typeof(MockFileSystemProvider) },
                { typeof(IPowerShellService), typeof(MockPowerShellService) },
                { typeof(IProcessService), typeof(MockProcessService) }
            };
        }
    }
}
