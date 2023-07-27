﻿using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BuildTools
{
    class VersionChange
    {
        public string OldVersion { get; set; }

        public string NewVersion { get; set; }

        public VersionChange(Version oldVersion, Version newVersion)
        {
            OldVersion = oldVersion.ToString();
            NewVersion = newVersion.ToString();
        }

        public VersionChange(string oldVersion, string newVersion)
        {
            OldVersion = oldVersion;
            NewVersion = newVersion;
        }

        public override string ToString()
        {
            if (OldVersion == NewVersion)
                return OldVersion;

            return $"{OldVersion} -> {NewVersion}";
        }
    }

    class SetVersionResult
    {
        public VersionChange Package { get; set; }
        public VersionChange Assembly { get; set; }
        public VersionChange File { get; set; }
        public VersionChange Info { get; set; }
        public VersionChange Module { get; set; }
        public VersionChange ModuleTag { get; set; }
        public string PreviousTag { get; set; }
    }

    class SetVersionService
    {
        private IProjectConfigProvider configProvider;
        private IFileSystemProvider fileSystem;
        private GetVersionService getVersionService;

        public SetVersionService(IProjectConfigProvider configProvider, IFileSystemProvider fileSystem, GetVersionService getVersionService)
        {
            this.configProvider = configProvider;
            this.fileSystem = fileSystem;
            this.getVersionService = getVersionService;
        }

        public SetVersionResult SetVersion(Version version, bool isLegacy, string continuousIntegrationBuild)
        {
            var oldVersion = getVersionService.GetVersion(isLegacy);

            SetVersionInternal(version, isLegacy, continuousIntegrationBuild);

            var newVersion = getVersionService.GetVersion(isLegacy);

            var result = new SetVersionResult
            {
                Package = new VersionChange(oldVersion.Package, newVersion.Package),
                Assembly = new VersionChange(oldVersion.Assembly, newVersion.Assembly),
                File = new VersionChange(oldVersion.File, newVersion.File),
                Info = new VersionChange(oldVersion.Info, newVersion.Info),
                Module = new VersionChange(oldVersion.Module, newVersion.Module),
                ModuleTag = new VersionChange(oldVersion.ModuleTag, newVersion.ModuleTag),
                PreviousTag = oldVersion.PreviousTag
            };

            return result;
        }

        public SetVersionResult UpdateVersion(bool isLegacy)
        {
            var oldVersion = getVersionService.GetVersion(isLegacy);

            var newVersion = new Version(
                oldVersion.File.Major,
                oldVersion.File.Minor,
                oldVersion.File.Build + 1,
                oldVersion.File.Revision
            );

            return SetVersion(newVersion, isLegacy, null);
        }

        private void SetVersionInternal(Version version, bool isLegacy, string continuousIntegrationBuild)
        {
            if (continuousIntegrationBuild != null)
            {
                if (continuousIntegrationBuild.Contains("-build."))
                    version = Version.Parse(continuousIntegrationBuild.Replace("-build", string.Empty));
                else
                    version = getVersionService.GetVersion(isLegacy).File;
            }

            SetVersionProps(version, isLegacy, continuousIntegrationBuild);
            SetPsd1Props(version);
        }

        private void SetVersionProps(Version version, bool isLegacy, string continuousIntegrationBuild)
        {
            var major = version.Major;
            var minor = version.Minor;
            var build = version.Build;
            var revision = version.Revision;

            if (build == -1)
                build = 0;

            if (revision == -1)
                revision = 0;

            var versionStr = $"{major}.{minor}.{build}";
            var assemblyVersion = $"{major}.{minor}.0.0";
            var fileVersion = $"{major}.{minor}.{build}.{revision}";
            var infoVersion = $"{major}.{minor}.{build}";

            if (continuousIntegrationBuild != null)
                infoVersion = continuousIntegrationBuild;
            else
            {
                if (revision != 0)
                    infoVersion = $"{infoVersion}.{revision}";
            }

            SetVersionPropsCore(versionStr, assemblyVersion, fileVersion, infoVersion);
            SetVersionPropsDesktop(versionStr, assemblyVersion, fileVersion, infoVersion);
        }

        private void SetVersionPropsCore(string version, string assemblyVersion, string fileVersion, string infoVersion)
        {
            var newContent = $@"
<!-- This code was generated by a tool. Any changes made manually will be lost -->
<!-- the next time this code is regenerated. -->

<Project>
  <PropertyGroup>
    <Version>{version}</Version>
    <AssemblyVersion>{assemblyVersion}</AssemblyVersion>
    <FileVersion>{fileVersion}</FileVersion>
    <InformationalVersion>{infoVersion}</InformationalVersion>
  </PropertyGroup>
</Project>
";

            var versionPath = configProvider.GetVersionPropsPath();

            fileSystem.WriteFileText(versionPath, newContent.Replace("\r", string.Empty).Replace("\n", Environment.NewLine));
        }

        private void SetVersionPropsDesktop(string version, string assemblyVersion, string fileVersion, string infoVersion)
        {
            var newContent = $@"
// This code was generated by a tool. Any changes made manually will be lost
// the next time this code is regenerated.

using System.Reflection;

[assembly: AssemblyVersion(""{assemblyVersion}"")]
[assembly: AssemblyFileVersion(""{fileVersion}"")]
[assembly: AssemblyInformationalVersion(""{infoVersion}"")]

";

            var versionPath = configProvider.GetVersionAttibPath();

            fileSystem.WriteFileText(versionPath, newContent.Replace("\r", string.Empty).Replace("\n", Environment.NewLine));
        }

        private void SetPsd1Props(Version version)
        {
            var major = version.Major;
            var minor = version.Minor;
            var build = version.Build;

            if (build == -1)
                build = 0;

            var versionStr = $"{major}.{minor}.{build}";

            var psd1Path = configProvider.GetSourcePowerShellModuleManifest();

            var psd1Contents = fileSystem.GetFileLines(psd1Path);

            var newContents = psd1Contents.Select(v =>
            {
                if (v.StartsWith("ModuleVersion = '"))
                {
                    v = Regex.Replace(v, "ModuleVersion = '(.+?)'", $"ModuleVersion = '{versionStr}'");

                    return v;
                }
                else if (Regex.IsMatch(v, ".+ReleaseNotes = '.+/tag.+"))
                {
                    v = Regex.Replace(v, "(.+ReleaseNotes = '.+/tag/)(.+)", $"$1v{versionStr}");

                    return v;
                }

                return v;
            }).ToArray();

            fileSystem.WriteFileLines(psd1Path, newContents);
        }
    }
}