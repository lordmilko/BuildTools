using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BuildTools
{
    class GetAppveyorVersionService
    {
        private readonly IAppveyorClient client;
        private readonly EnvironmentService environmentService;
        private readonly GetVersionService getVersionService;
        private readonly Logger logger;

        public GetAppveyorVersionService(
            IAppveyorClient client,
            EnvironmentService environmentService,
            GetVersionService getVersionService,
            Logger logger)
        {
            this.client = client;
            this.environmentService = environmentService;
            this.getVersionService = getVersionService;
            this.logger = logger;
        }

        public string GetVersion(bool isLegacy)
        {
            var build = environmentService.AppveyorBuildNumber;

            if (build == -1)
                throw new InvalidOperationException($"Environment variable '{WellKnownEnvironmentVariable.AppveyorBuildNumber}' is not set");

            var assemblyVersion = new Version(getVersionService.GetVersion(isLegacy).File.ToString(3));
            var lastBuild = GetLastAppveyorBuild();
            var lastRelease = GetLastAppveyorNuGetVersion();

            logger.LogVerbose($"    Assembly version: {assemblyVersion}");
            logger.LogVerbose($"    Last build: {lastBuild}");
            logger.LogVerbose($"    Last release: {lastRelease}");

            string result;

            if (IsPreview(assemblyVersion, lastRelease))
            {
                if (IsFirstPreview(lastBuild))
                    build = 1;
                else
                    build = IncrementBuild(lastBuild);

                var v = assemblyVersion;

                result = $"{v.Major}.{v.Minor}.{v.Build + 1}-preview.{build}";
            }
            else if (IsPreRelease(assemblyVersion, lastBuild, lastRelease))
            {
                if (IsFirstPreRelease(lastBuild))
                    build = 1;
                else
                    build = IncrementBuild(lastBuild);

                result = $"{assemblyVersion}-build.{build}";
            }
            else if (IsFullRelease(assemblyVersion, lastRelease))
                result = assemblyVersion.ToString();
            else
                throw new InvalidOperationException("Failed to determine the type of build");

            return result;
        }

        private int IncrementBuild(string version)
        {
            var buildStr = Regex.Replace(version, ".+-.+\\.(.+)", "$1");

            if (!int.TryParse(buildStr, out var i))
                throw new InvalidOperationException($"Could not convert build '{buildStr}' from version '{version}' to a number.");

            return i + 1;
        }

        private bool IsPreview(Version assemblyVersion, string lastRelease)
        {
            if (string.IsNullOrEmpty(lastRelease))
                return false;

            //If this DLL has the same version as the last RELEASE, this should be a preview release
            return assemblyVersion == CleanLastRelease(lastRelease);
        }

        private bool IsFirstPreview(string lastBuild) => !lastBuild?.Contains("preview") == true;

        private bool IsFullRelease(Version assemblyVersion, string lastRelease)
        {
            if (string.IsNullOrEmpty(lastRelease))
                return true;

            return assemblyVersion > CleanLastRelease(lastRelease);
        }

        private bool IsPreRelease(Version assemblyVersion, string lastBuild, string lastRelease)
        {
            if (string.IsNullOrEmpty(lastBuild))
                return false;

            if (lastBuild.Contains("preview"))
                return false;

            if (string.IsNullOrEmpty(lastRelease) || assemblyVersion > CleanLastRelease(lastRelease))
            {
                var lastBuildClean = CleanLastRelease(lastBuild);

                if (assemblyVersion == lastBuildClean)
                {
                    //We're the same assembly version as the last build which hasn't
                    //been released yet. Therefore we are a pre-release
                    return true;
                }
            }

            return false;
        }

        private Version CleanLastRelease(string version)
        {
            var str = Regex.Replace(version, "-build.+", string.Empty);

            return Version.Parse(str);
        }

        private bool IsFirstPreRelease(string lastBuild) => !lastBuild?.Contains("build") == true;

        protected virtual string GetLastAppveyorBuild()
        {
            if (environmentService.IsAppveyor)
            {
                var history = client.GetBuildHistory();

                var version = history.FirstOrDefault(v => !v.Version?.StartsWith("Build") == true)?.Version;

                return version;
            }

            return null;
        }

        protected virtual string GetLastAppveyorNuGetVersion()
        {
            if (environmentService.IsAppveyor)
            {
                var deployments = client.GetAppveyorDeployments();

                var lastNuGet = deployments
                    .OrderByDescending(d => d.Started)
                    .FirstOrDefault(d => d.Environment.Provider == "NuGet");

                return lastNuGet?.Build.Version;
            }

            return null;
        }
    }
}
