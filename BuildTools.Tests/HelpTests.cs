using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    [TestClass]
    public class HelpTests
    {
        [TestMethod]
        public void Help_HasHelp()
        {
            Test(help =>
            {
                var synopsis = help.synopsis;

                Assert.AreEqual("Clears the output of one or more previous Help_HasHelp builds.", synopsis);
            });
        }

        [TestMethod]
        public void Help_Legacy_Syntax()
        {
            Test(help =>
            {
                var item = help.syntax.syntaxItem[0];
                var parameters = item.parameter;

                Assert.AreEqual(2, parameters.Length);

                Assert.AreEqual("Configuration", parameters[0].name);
                var description = parameters[0].description[0];
                Assert.AreEqual("Configuration to clean. If no value is specified Help_Legacy_Syntax will clean the last Debug build.", description.Text);

                Assert.AreEqual("Legacy", parameters[1].name);
                description = parameters[1].description[0];
                Assert.AreEqual("Specifies whether to use legacy .NET tooling to clear the build.", description.Text);
            });
        }

        [TestMethod]
        public void Help_Legacy_Parameter()
        {
            Test(help =>
            {
                var parameters = help.parameters.parameter;

                Assert.AreEqual(3, parameters.Length);

                var legacy = parameters[2];
                Assert.AreEqual("Legacy", legacy.name);
                var description = legacy.description[0];
                Assert.AreEqual("Specifies whether to use legacy .NET tooling to clear the build.", description.Text);
            });
        }

        private void Test(Action<dynamic> action, [CallerMemberName] string methodName = null)
        {
            var invoker = new PowerShellInvoker();

            BuildToolsSessionState.HeadlessUI = true;

            var tempFile = Path.GetTempFileName();
            var newTempFile = Path.ChangeExtension(tempFile, "sln");
            File.Move(tempFile, newTempFile);
            tempFile = newTempFile;

            try
            {
                File.WriteAllText(tempFile, $@"
@{{
    Name = '{methodName}'
    CmdletPrefix = '{methodName}'
    SolutionName = '{methodName}.sln'
    CopyrightAuthor = 'foo'
    CopyrightYear = '2023'
}}
");

                var dir = Path.GetDirectoryName(tempFile);
                var file = Path.GetFileName(tempFile);

                invoker.Invoke<object>("Start-BuildEnvironment", new
                {
                    BuildRoot = dir,
                    File = file
                });

                dynamic help = invoker.Invoke<PSObject>("Get-Help", $"Clear-{methodName}Build").SingleOrDefault();

                Assert.IsNotNull(help);

                action(help);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}