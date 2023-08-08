using BuildTools.PowerShell;

namespace BuildTools.Tests
{
    class MockProjectConfigProviderFactory : ProjectConfigProviderFactory
    {
        public MockProjectConfigProviderFactory(IFileSystemProvider fileSystem, IPowerShellService powerShell) : base(fileSystem, powerShell)
        {
        }

        protected override void RegisterConfig(ProjectConfig config)
        {
            //Don't register the config
        }
    }
}
