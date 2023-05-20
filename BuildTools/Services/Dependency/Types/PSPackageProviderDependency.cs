namespace BuildTools
{
    class PSPackageProviderDependency : Dependency
    {
        public PSPackageProviderDependency(string name, string version = null, string minimumVersion = null) : base(name, DependencyType.PSPackageProvider, version, minimumVersion)
        {
        }
    }
}