namespace BuildTools
{
    class PSPackageDependency : Dependency
    {
        public bool SkipPublisherCheck { get; }

        public PSPackageDependency(string name, string version = null, string minimumVersion = null, bool skipPublisherCheck = false) : base(name, DependencyType.PSPackage, version, minimumVersion)
        {
            SkipPublisherCheck = skipPublisherCheck;
        }
    }
}