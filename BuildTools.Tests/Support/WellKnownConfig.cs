namespace BuildTools.Tests
{
    static class WellKnownConfig
    {
        public static ProjectConfigBuilder PrtgAPI => ProjectConfigBuilder.Empty
            .WithName("PrtgAPI")

            .WithSolutionName("PrtgAPI.sln")

            .WithBuildFilter("PrtgAPI.*")

            .WithCmdletPrefix("Prtg")
            .WithPowerShellProjectName("PrtgAPI.PowerShell")

            .WithPrompt("PrtgAPI")

            .WithTestTypes(TestType.CSharp)

            .WithCopyrightAuthor("lordmilko")
            .WithCopyrightYear("2015");
    }
}