using System;
using System.Management.Automation;

namespace BuildTools.PowerShell
{
    interface IPackageProvider
    {
        string Name { get; }

        Version Version { get; }
    }

    public class PackageProvider : IPackageProvider
    {
        public string Name { get; }

        public Version Version { get; }

        internal PackageProvider(PSObject pso)
        {
            Name = (string) pso.Properties["Name"].Value;
            Version = (Version) pso.Properties["Version"].Value;
        }
    }
}