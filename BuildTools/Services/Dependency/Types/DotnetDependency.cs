namespace BuildTools
{
    class DotnetDependency : Dependency
    {
        public DotnetDependency() : base("dotnet", DependencyType.Dotnet)
        {
        }
    }
}