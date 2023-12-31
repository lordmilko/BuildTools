﻿using System;
using System.Collections.Generic;
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

        public Dictionary<string, string[]> ExecuteMap { get; } = new Dictionary<string, string[]>();

        public string[] Execute(string fileName, ArgList arguments = default, string errorFormat = null, bool writeHost = false, bool shellExecute = false)
        {
            var argList = arguments.Arguments;

            Executed.Add($"{fileName} {arguments}");

            if (fileName == "choco" && argList != null && argList.Length >= 2 && argList[0] == "install")
            {
                var dependencyName = argList[1];
                var dependency = (ChocolateyPackageDependency) provider.Value.GetDependency(dependencyName);
                var command = powerShell.GetCommand(dependency.CommandName);
                Assert.IsNull(command);
                powerShell.KnownCommands[dependency.CommandName] = new MockPowerShellCommand(dependency.CommandName);
            }

            if (ExecuteMap.TryGetValue($"{fileName} {arguments}", out var result))
                return result;

            return new string[0];
        }

        public bool IsRunning(string processName) => false;

        public void AssertExecuted(string fileNameAndArgs)
        {
            Assert.IsTrue(Executed.Contains(fileNameAndArgs), $"'{fileNameAndArgs}' was not executed");
        }
    }
}
