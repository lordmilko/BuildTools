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
                SolutionName = "Foo",
                ProjectName = "Bar",
                CmdletPrefix = "Foo"
            };
        }
    }
}