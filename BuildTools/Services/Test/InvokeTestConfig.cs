namespace BuildTools
{
    class InvokeTestConfig
    {
        public string[] Name { get; set; }

        public TestType[] Type { get; set; }

        public TestType[] ProjectType { get; set; }

        public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Debug;

        public bool Integration { get; set; }

        public string[] Tags { get; set; }

        public TestTarget Target => new TestTarget(Type, ProjectType);

        public InvokeTestConfig(TestType[] projectType)
        {
            ProjectType = projectType;
        }
    }
}
