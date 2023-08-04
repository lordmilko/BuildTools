namespace BuildTools
{
    class PackageConfig
    {
        public BuildConfiguration Configuration { get; }

        public bool IsRelease => Configuration == BuildConfiguration.Release;

        public bool IsDebug => Configuration == BuildConfiguration.Debug;

        public bool IsLegacy { get; }

        public bool IsMultiTargeting { get; }

        public PackageType[] Types { get; }

        public PackageTarget Target => new PackageTarget(Types);

        public PackageConfig(BuildConfiguration buildConfiguration, bool isLegacy, bool powerShellMultiTargeted, params PackageType[] types)
        {
            Configuration = buildConfiguration;
            IsLegacy = isLegacy;
            IsMultiTargeting = IsRelease && !isLegacy && powerShellMultiTargeted;
            Types = types;
        }
    }
}
