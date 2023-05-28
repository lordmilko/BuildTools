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
        public PSModuleInfo Module { get; }

        public string Name => Module.Name;

        public Version Version => Module.Version;

        public PowerShellModule(PSModuleInfo module)
        {
            Module = module;
        }
    }
}