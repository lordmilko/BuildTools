namespace BuildTools
{
    class DotnetDependency : Dependency
    {
        public DotnetDependency() : base(WellKnownDependency.Dotnet, DependencyType.Dotnet)
        {
        }
    }
}