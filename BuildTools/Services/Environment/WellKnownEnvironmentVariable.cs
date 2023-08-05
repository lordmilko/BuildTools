namespace BuildTools
{
    public static class WellKnownEnvironmentVariable
    {
        public const string Appveyor = "APPVEYOR";
        public const string AppveyorAccountName = "APPVEYOR_ACCOUNT_NAME";
        public const string AppveyorAPIToken = "APPVEYOR_API_TOKEN";
        public const string AppveyorBuildFolder = "APPVEYOR_BUILD_FOLDER";
        public const string AppveyorBuildNumber = "APPVEYOR_BUILD_NUMBER";
        public const string AppveyorBuildVersion = "APPVEYOR_BUILD_VERSION";
        public const string AppveyorProjectSlug = "APPVEYOR_PROJECT_SLUG";

        public const string TravisBuildDir = "TRAVIS_BUILD_DIR";

        public const string Configuration = "CONFIGURATION";
        public const string CI = "CI";

        public const string Path = "PATH";
        public const string ProgramFilesx86 = "ProgramFiles(x86)";

        public const string ChocolateyInstall = "ChocolateyInstall";

        public const string DotnetSkipFirstTimeExperience = "DOTNET_SKIP_FIRST_TIME_EXPERIENCE";
    }
}
