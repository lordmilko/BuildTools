using System;
using System.Management.Automation;

namespace BuildTools.PowerShell
{
    interface IPowerShellPackage
    {
        string Name { get; }

        Version Version { get; }
    }

    public class PowerShellPackage : IPowerShellPackage
    {
        //Real type is SoftwareIdentity in Microsoft.PowerShell.PackageManagement.dll

        public PSObject Raw { get; }

        public string Name { get; }

        public Version Version { get; }

        internal PowerShellPackage(PSObject pso)
        {
            Raw = pso;

            Name = (string) pso.Properties["Name"].Value;
            Version = new Version((string) pso.Properties["Version"].Value);
        }

        public override string ToString()
        {
            return $"{Name} {Version}";
        }
    }
}
