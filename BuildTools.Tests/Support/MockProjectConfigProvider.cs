namespace BuildTools.Tests
{
    class MockProjectConfigProvider : IProjectConfigProvider
    {
        public ProjectConfig Config { get; set; }
        public string SolutionRoot { get; } = "C:\\";

        public MockProjectConfigProvider()
        {
            Config = new ProjectConfig
            {
                Name = "Foo",
                SolutionName = "Bar",
                CmdletPrefix = "Foo"
            };
        }

        public string GetSolutionPath(bool isLegacy)
        {
            throw new System.NotImplementedException();
        }
    }
}