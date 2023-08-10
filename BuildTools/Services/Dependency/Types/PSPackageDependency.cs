namespace BuildTools
{
    class PSPackageDependency : Dependency
    {
        public bool SkipPublisherCheck { get; }

        public PSPackageDependency(string name, string version = null, string minimumVersion = null, bool skipPublisherCheck = false, bool condition = true) : base(name, DependencyType.PSPackage, version, minimumVersion, condition: condition)
        {
            SkipPublisherCheck = skipPublisherCheck;
        }
    }
}
