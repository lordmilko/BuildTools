namespace BuildTools
{
    public class PackageFiles
    {
        [PackageType(PackageType.CSharp)]
        public PackageFileItem[] CSharp { get; set; }

        [PackageType(PackageType.PowerShell)]
        public PackageFileItem[] PowerShell { get; set; }

        [PackageType(PackageType.Redistributable)]
        public PackageFileItem[] Redist { get; set; }
    }
}
