using System;
using System.Management.Automation;

namespace BuildTools.PowerShell
{
    public class PowerShellPackage
    {
        //Real type is SoftwareIdentity in Microsoft.PowerShell.PackageManagement.dll

        public string Name { get; }

        public Version Version { get; }

        internal PowerShellPackage(PSObject pso)
        {
            Name = (string) pso.Properties["Name"].Value;
            Version = new Version((string) pso.Properties["Version"].Value);
        }

        public override string ToString()
        {
            return $"{Name} {Version}";
        }
    }
}