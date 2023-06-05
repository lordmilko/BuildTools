namespace BuildTools
{
    class ChocolateyDependency : ChocolateyPackageDependency
    {
        public ChocolateyDependency(string version = null, string minimumVersion = null) : base(WellKnownDependency.Chocolatey, version, minimumVersion)
        {
        }
    }
}