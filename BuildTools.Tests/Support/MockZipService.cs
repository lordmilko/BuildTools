namespace BuildTools.Tests
{
    class MockZipService : IZipService
    {
        public void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
        }

        public void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
        {
        }
    }
}
