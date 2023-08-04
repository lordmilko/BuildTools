using BuildTools.PowerShell;

namespace BuildTools.Tests
{
    class MockPSRepository : IPSRepository
    {
        public string Name => PackageSourceService.RepoName;
    }
}
