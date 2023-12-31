﻿namespace BuildTools
{
    class PackageConfig
    {
        public BuildConfiguration Configuration { get; }

        public bool IsRelease => Configuration == BuildConfiguration.Release;

        public bool IsDebug => Configuration == BuildConfiguration.Debug;

        public bool IsLegacy { get; }

        public bool IsMultiTargeting { get; }

        public PackageType[] Types { get; }

        public PackageType[] ProjectType { get; }

        public PackageTarget Target => new PackageTarget(Types, ProjectType);

        public PackageConfig(
            BuildConfiguration configuration,
            bool isLegacy,
            bool powerShellMultiTargeted,
            PackageType[] projectType,
            params PackageType[] types)
        {
            Configuration = configuration;
            IsLegacy = isLegacy;
            IsMultiTargeting = IsRelease && !isLegacy && powerShellMultiTargeted;
            Types = types;
            ProjectType = projectType;
        }
    }
}
