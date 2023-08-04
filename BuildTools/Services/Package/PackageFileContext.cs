namespace BuildTools
{
    public class PackageFileContext
    {
        public bool IsRelease { get; }

        public bool IsDebug { get; }

        public bool IsLegacy { get; }

        public bool IsMultiTargeting { get; }

        public string DebugTargetFramework { get; }

        public PackageFileContext(BuildConfiguration buildConfiguration, bool isLegacy, bool powerShellMultiTargeted, string debugTargetFramework)
        {
            IsRelease = buildConfiguration == BuildConfiguration.Release;
            IsDebug = buildConfiguration == BuildConfiguration.Debug;
            IsLegacy = isLegacy;
            IsMultiTargeting = IsRelease && !isLegacy && powerShellMultiTargeted;
            DebugTargetFramework = debugTargetFramework;
        }
    }
}
