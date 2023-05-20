namespace BuildTools
{
    interface IDependencyInstaller
    {
        DependencyResult Install(Dependency dependency, bool log, bool logSkipped);
    }
}