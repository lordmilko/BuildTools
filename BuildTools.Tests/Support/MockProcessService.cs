using System;
using System.Collections.Generic;
using System.Linq;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    class MockProcessService : IProcessService, IMock<IProcessService>
    {
        private Lazy<DependencyProvider> provider;
        private MockPowerShellService powerShell;

        public MockProcessService(Lazy<DependencyProvider> provider, IPowerShellService powerShell)
        {
            this.provider = provider;
            this.powerShell = (MockPowerShellService) powerShell;
        }

        public List<string> Executed { get; } = new List<string>();

        public void Execute(string fileName, string arguments = null, string errorFormat = null)
        {
            Executed.Add($"{fileName} {arguments}");
        }

        public void Execute(string fileName, IEnumerable<string> arguments = null, string errorFormat = null)
        {
            var argList = arguments?.ToArray();

            Execute(fileName, arguments == null ? null : string.Join(" ", argList), errorFormat);

            if (fileName == "choco" && argList != null && argList.Length >= 2 && argList[0] == "install")
            {
                var dependencyName = argList[1];
                var dependency = (ChocolateyPackageDependency) provider.Value.GetDependency(dependencyName);
                var command = powerShell.GetCommand(dependency.CommandName);
                Assert.IsNull(command);
                powerShell.KnownCommands[dependency.CommandName] = new MockPowerShellCommand(dependency.Name);
            }
        }

        public void AssertExecuted(string fileNameAndArgs)
        {
            Assert.IsTrue(Executed.Contains(fileNameAndArgs), $"'{fileNameAndArgs}' was not executed");
        }
    }
}