using System;
using System.Management.Automation;

namespace BuildTools.PowerShell
{
    interface IPowerShellModule
    {
        string Name { get; }

        Version Version { get; }
    }

    class PowerShellModule : IPowerShellModule
    {
        private readonly PSModuleInfo module;

        public string Name => module.Name;

        public Version Version => module.Version;

        public PowerShellModule(PSModuleInfo module)
        {
            this.module = module;
        }
    }
}