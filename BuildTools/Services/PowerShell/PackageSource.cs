using System.Management.Automation;

namespace BuildTools.PowerShell
{
    interface IPackageSource
    {
        string Name { get; }
    }

    class PackageSource : IPackageSource
    {
        public string Name { get; }

        public PackageSource(PSObject pso)
        {
            Name = (string) pso.Properties["Name"].Value;
        }
    }
}