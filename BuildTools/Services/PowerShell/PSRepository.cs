using System.Management.Automation;

namespace BuildTools.PowerShell
{
    interface IPSRepository
    {
        string Name { get; }
    }

    class PSRepository : IPSRepository
    {
        public string Name { get; }

        public PSRepository(PSObject pso)
        {
            Name = (string) pso.Properties["Name"].Value;
        }
    }
}