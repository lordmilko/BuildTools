namespace BuildTools
{
    public class PackageTests
    {
        [PackageType(PackageType.CSharp)]
        public IPackageTest[] CSharp { get; set; }
        
        [PackageType(PackageType.PowerShell)]
        public IPackageTest[] PowerShell { get; set; }
    }
}
