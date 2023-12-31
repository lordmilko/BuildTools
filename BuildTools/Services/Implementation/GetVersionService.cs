﻿using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class VersionTable
    {
        public Version Package { get; }

        public Version Assembly { get; }

        public Version File { get; }

        public string Info { get; }

        public Version Module { get; }

        public string ModuleTag { get; }

        public string PreviousTag { get; }

        public VersionTable(
            Version package,
            Version assembly,
            Version file,
            string info,
            Version module,
            string moduleTag,
            string previousTag)
        {
            Package = package;
            Assembly = assembly;
            File = file;
            Info = info;
            Module = module;
            ModuleTag = moduleTag;
            PreviousTag = previousTag;
        }
    }

    class GetVersionService
    {
        private IProjectConfigProvider configProvider;
        private IFileSystemProvider fileSystem;
        private IPowerShellService powerShell;
        private IProcessService processService;

        public GetVersionService(
            IProjectConfigProvider configProvider,
            IFileSystemProvider fileSystem,
            IPowerShellService powerShell,
            IProcessService processService)
        {
            this.configProvider = configProvider;
            this.fileSystem = fileSystem;
            this.powerShell = powerShell;
            this.processService = processService;
        }

        public virtual VersionTable GetVersion(bool isLegacy)
        {
            string assembly;
            string file;
            string info;
            string package;
            string moduleVersion = null;
            string releaseTag = null;
            string previousTag = null;

            if (isLegacy)
            {
                var versionFile = configProvider.GetVersionAttibPath();

                var versionContents = fileSystem.ReadFileLines(versionFile);

                assembly = GetAssemblyAttribute(versionContents, "AssemblyVersion");
                file = GetAssemblyAttribute(versionContents, "AssemblyFileVersion");
                info = GetAssemblyAttribute(versionContents, "AssemblyInformationalVersion");
                package = new Version(file).ToString(3);
            }
            else
            {
                var versionPath = configProvider.GetVersionPropsPath();

                var versionContents = fileSystem.ReadFileText(versionPath);

                var versionProps = XDocument.Parse(versionContents).Element("Project")?.Element("PropertyGroup");

                if (versionProps == null)
                    throw new InvalidOperationException($"Could not find XML element Project -> PropertyGroup in file '{versionPath}'");

                package = GetElementValue(versionProps, "Version");
                assembly = GetElementValue(versionProps, "AssemblyVersion");
                file = GetElementValue(versionProps, "FileVersion");
                info = GetElementValue(versionProps, "InformationalVersion");
            }

            var psd1Path = configProvider.GetSourcePowerShellModuleManifest();

            if (psd1Path != null)
            {
                var psd1Contents = fileSystem.ReadFileText(psd1Path);
                var psd1Hashtable = (Hashtable) powerShell.InvokeAndUnwrap(psd1Contents);

                moduleVersion = (string) psd1Hashtable["ModuleVersion"];
                releaseTag = GetReleaseTag(psd1Hashtable);
            }

            if (HaveGit())
            {
                previousTag = GetGitTag();
            }

            return new VersionTable(
                new Version(package),
                new Version(assembly),
                new Version(file),
                info,
                moduleVersion == null ? null : new Version(moduleVersion),
                releaseTag,
                previousTag
            );
        }

        private string GetElementValue(XElement elm, string name)
        {
            var valueElm = elm.Element(name);

            if (valueElm == null)
                throw new InvalidOperationException($"Could not find version XML element '{name}'.");

            var value = valueElm.Value;

            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Version XML element '{name}' did not have a value.");

            return value;
        }

        private string GetAssemblyAttribute(string[] fileContents, string attributeName)
        {
            var matches = fileContents.Where(l => l.Contains(attributeName)).ToArray();

            if (matches.Length == 0)
                throw new InvalidOperationException($"Could not find version attribute '{attributeName}'.");

            if (matches.Length > 1)
                throw new InvalidOperationException($"Found more than one version attribute '{attributeName}'");

            var result = Regex.Replace(matches[0], $".+{attributeName}\\(\"(.+?)\"\\).+", "$1");

            return result;
        }

        private string GetReleaseTag(Hashtable psd1Hashtable)
        {
            var privateData = (Hashtable) psd1Hashtable["PrivateData"];

            if (privateData == null)
                throw new InvalidOperationException("Could not find PrivateData section in psd1 file.");

            var psData = (Hashtable) privateData["PSData"];

            if (psData == null)
                throw new InvalidOperationException("Could not find PSData section in psd1 file.");

            var releaseNotes = (string) psData["ReleaseNotes"];

            var licenseUri = (string) psData["LicenseUri"];

            if (string.IsNullOrWhiteSpace(licenseUri))
                throw new InvalidOperationException("LicenseUri has not been set in psd1 file.");

            var projectUri = (string) psData["ProjectUri"];

            if (string.IsNullOrWhiteSpace(projectUri))
                throw new InvalidOperationException("ProjectUri has not been set in psd1 file.");

            if (string.IsNullOrWhiteSpace(releaseNotes))
                throw new InvalidOperationException("Could not find ReleaseNotes section in psd1 file.");

            var match = Regex.Match(releaseNotes, ".+/releases/tag/(.+)");

            if (!match.Success)
                throw new InvalidOperationException("Could not find release tag URL in ReleaseNotes section of psd1 file.");

            return match.Groups[1].Value.TrimEnd();
        }

        private bool HaveGit()
        {
            var gitFolder = Path.Combine(configProvider.SolutionRoot, ".git");

            if (!fileSystem.DirectoryExists(gitFolder))
                return false;

            var command = powerShell.GetCommand("git");

            if (command == null)
                return false;

            return true;
        }

        private string GetGitTag()
        {
            string[] result;

            try
            {
                result = processService.Execute("git", new ArgList
                {
                    "-C",
                    configProvider.SolutionRoot,
                    "describe",
                    "--tags"
                });
            }
            catch (Exception ex) when (ex.Message.Contains("No names found"))
            {
                return null;
            }

            if (result.Length == 0)
                return null;

            if (result.Length > 1)
                throw new InvalidOperationException($"Don't know how to handle having multiple Git tag results: {string.Join(", ", result)}");

            var match = Regex.Match(result[0], "(.+?)(-.+)");

            if (!match.Success)
                throw new InvalidOperationException($"Could not extract version from Git tag '{result[0]}'");

            return match.Groups[1].Value;
        }
    }
}
