using BuildTools.PowerShell;

namespace BuildTools.Tests
{
    class MockPackageSource : IPackageSource
    {
        public string Name => PackageSourceService.RepoName;
    }
}
