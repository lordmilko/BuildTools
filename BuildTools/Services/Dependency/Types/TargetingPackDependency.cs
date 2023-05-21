namespace BuildTools
{
    class TargetingPackDependency : Dependency
    {
        public TargetingPackDependency(string name, string version) : base(name, DependencyType.TargetingPack, version)
        {
        }
    }
}