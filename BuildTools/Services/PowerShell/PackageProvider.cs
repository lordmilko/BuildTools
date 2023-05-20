using System;
using System.Management.Automation;

namespace BuildTools.PowerShell
{
    public class PackageProvider
    {
        public string Name { get; }

        public Version Version { get; }

        internal PackageProvider(PSObject pso)
        {
            Name = (string)pso.Properties["Name"].Value;
            Version = (Version) pso.Properties["Version"].Value;
        }
    }
}