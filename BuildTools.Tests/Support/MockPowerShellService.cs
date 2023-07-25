﻿using System;
using System.Collections.Generic;
using System.Management.Automation;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    class MockPowerShellService : IPowerShellService, IMock<IPowerShellService>
    {
        public IPowerShellModule[] InstalledModules { get; set; }
        public IPackageProvider InstalledPackageProvider { get; set; }

        public Dictionary<string, IPowerShellCommand> KnownCommands { get; } = new Dictionary<string, IPowerShellCommand>();

        public Dictionary<string, object> InvokeScriptMap { get; } = new Dictionary<string, object>();

        public List<string> InvokedCommands { get; } = new List<string>();

        public bool IsISE { get; }
        public PSEdition Edition { get; }
        public bool IsProgressEnabled { get; }
        public bool IsWindows { get; set; }

        public IPowerShellCommand GetCommand(string name)
        {
            if (KnownCommands.TryGetValue(name, out var command))
                return command;

            throw new InvalidOperationException($"Existence of command '{name}' has not been specified.");
        }

        public void WriteColor(string message, ConsoleColor? color = null, bool newLine = true)
        {
            throw new NotImplementedException();
        }

        public void WriteError(ErrorRecord errorRecord)
        {
            throw new NotImplementedException();
        }

        public void WriteVerbose(string message)
        {
        }

        public void WriteProgress(
            string activity = null,
            string status = null,
            string currentOperation = null,
            int? percentComplete = null)
        {
        }

        public void WriteWarning(string message)
        {
            throw new NotImplementedException();
        }

        public void CompleteProgress()
        {
        }

        public IPowerShellModule[] GetInstalledModules(string name)
        {
            return InstalledModules ?? new IPowerShellModule[0];
        }

        public IPowerShellModule GetModule(string name)
        {
            throw new NotImplementedException();
        }

        public IPowerShellModule ImportModule(string name, bool global)
        {
            throw new NotImplementedException();
        }

        public IPowerShellModule RegisterModule(string name, IList<Type> cmdletTypes)
        {
            throw new NotImplementedException();
        }

        public void PublishModule(string path)
        {
        }

        public IPowerShellPackage InstallPackage(string name, Version requiredVersion = null, Version minimumVersion = null,
            bool skipPublisherCheck = false)
        {
            return new MockPowerShellPackage(
                name,
                requiredVersion ?? minimumVersion ?? new Version("1.0")
            );
        }

        #region PackageProvider

        public IPackageProvider GetPackageProvider(string name)
        {
            return InstalledPackageProvider;
        }

        public IPackageProvider InstallPackageProvider(string name, Version minimumVersion = null)
        {
            return new MockPackageProvider(
                name,
                minimumVersion ?? new Version("1.0")
            );
        }

        #endregion
        #region PackageSource

        public IPackageSource[] GetPackageSource()
        {
            throw new NotImplementedException();
        }

        public void RegisterPackageSource()
        {
            throw new NotImplementedException();
        }

        public void UnregisterPackageSource()
        {
            throw new NotImplementedException();
        }

        #endregion
        #region PSRepository

        public IPSRepository[] GetPSRepository() => new[] {new MockPSRepository()};

        public void RegisterPSRepository()
        {
        }

        public void UnregisterPSRepository()
        {
        }

        #endregion

        public object InvokeAndUnwrap(string script, params object[] input)
        {
            InvokedCommands.Add(script);

            InvokeScriptMap.TryGetValue(script, out var result);

            return result;
        }

        public void InitializePrompt(ProjectConfig config)
        {
            throw new NotImplementedException();
        }

        public object[] InvokeWithArgs(string cmdlet, params string[] args)
        {
            throw new NotImplementedException();
        }

        public void AssertInvoked(string script)
        {
            Assert.IsTrue(InvokedCommands.Contains(script), $"Command '{script}' was not invoked");
        }
    }
}
