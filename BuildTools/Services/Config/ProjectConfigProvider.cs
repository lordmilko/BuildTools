﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools
{
    class ProjectConfigProvider : IProjectConfigProvider
    {
        private readonly IFileSystemProvider fileSystem;
        private readonly IPowerShellService powerShell;

        internal static readonly string[] SrcFolders =
        {
            "src"
        };

        public ProjectConfig Config { get; }

        public string SolutionRoot { get; }

        /// <summary>
        /// Gets the folder the source code is actually contained in. Could be <see cref="SolutionRoot"/>, a "src" subfolder under <see cref="SolutionRoot"/> or a custom <see cref="ProjectConfig.SourceFolder"/>.
        /// </summary>
        public string SourceRoot { get; }

        public bool HasLegacyProjects
        {
            get
            {
                EnsureProjects();

                return projects.Any(p => p.IsLegacy);
            }
        }

        private BuildProject[] projects;
        private string baseSolutionName;

        internal ProjectConfigProvider(
            ProjectConfig config,
            string buildRoot,
            IFileSystemProvider fileSystem,
            IPowerShellService powerShell)
        {
            this.fileSystem = fileSystem;
            this.powerShell = powerShell;

            Config = config;
            SolutionRoot = CalculateSolutionRoot(buildRoot, Config.SourceFolder, fileSystem, powerShell);
            powerShell.WriteVerbose($"Found solution root '{SolutionRoot}'");

            baseSolutionName = GetBaseSolutionName();

            SourceRoot = CalculateSourceRoot();
        }

        internal static string CalculateSolutionRoot(
            string buildRoot,
            string sourceFolder,
            IFileSystemProvider fileSystem,
            IPowerShellService powerShell)
        {
            powerShell.WriteVerbose($"Calculating solution root using build root {buildRoot}{(!string.IsNullOrWhiteSpace(sourceFolder) ? $" and source folder {sourceFolder}" : string.Empty)}");

            var subDirs = new List<string>();
            subDirs.AddRange(SrcFolders);

            if (!string.IsNullOrWhiteSpace(sourceFolder))
                subDirs.Add(sourceFolder);

            while (buildRoot != null)
            {
                powerShell.WriteVerbose($"Searching for *.sln files in {buildRoot}");

                //Is it in the current folder?
                if (fileSystem.EnumerateFiles(buildRoot, "*.sln").Any())
                    return buildRoot;

                //Is it in a subfolder like src

                foreach (var dir in subDirs)
                {
                    var path = Path.Combine(buildRoot, dir);

                    if (fileSystem.DirectoryExists(path) && fileSystem.EnumerateFiles(path, "*.sln").Any())
                        return path;
                }

                buildRoot = Path.GetDirectoryName(buildRoot);
            }

            throw new InvalidOperationException("Could not determine the solution root of the project.");
        }

        private string CalculateSourceRoot()
        {
            powerShell.WriteVerbose("Calculating source root");

            if (Config.SourceFolder != null)
            {
                var path = Path.Combine(SolutionRoot, Config.SourceFolder);

                if (!fileSystem.DirectoryExists(path))
                    throw new InvalidOperationException($"Specified {nameof(ProjectConfig.SourceFolder)} '{path}' does not exist.");

                powerShell.WriteVerbose($"Source folder specified, setting source root to {SourceRoot}");

                return path;
            }
            else
            {
                //Don't know what the source root is, so try guess

                foreach (var dir in SrcFolders)
                {
                    var path = Path.Combine(SolutionRoot, dir);

                    if (fileSystem.DirectoryExists(path))
                    {
                        powerShell.WriteVerbose($"Found directory '{dir}', setting source root to '{path}'");
                        return path;
                    }
                }

                powerShell.WriteVerbose($"Couldn't find a custom source root, using solution root '{SolutionRoot}'");
                return SolutionRoot;
            }
        }

        private string GetBaseSolutionName()
        {
            if (!string.IsNullOrWhiteSpace(Config.SolutionName))
                return Config.SolutionName;

            var files = fileSystem.EnumerateFiles(SolutionRoot, "*.sln").Select(Path.GetFileName).ToArray();

            //Implicitly there's at least one, since we found the solution root
            if (files.Length == 1)
                return files[0];

            var nonCore = files.Where(f =>
            {
                var baseName = Path.GetFileNameWithoutExtension(f);

                if (baseName.EndsWith(ProjectConfig.CoreSuffix))
                    return false;

                return true;
            }).ToArray();

            if (nonCore.Length == 0)
                throw new InvalidOperationException($"Cannot calculate solution name: '{nameof(Config.SolutionName)}' was not specified and all solution names exclusively ended with core suffix '{ProjectConfig.CoreSuffix}'.");

            if (nonCore.Length > 1)
                throw new InvalidOperationException($"Cannot calculate solution name: '{nameof(Config.SolutionName)}' was not specified and multiple solutions were found: {string.Join(", ", nonCore)}.");

            return nonCore[0];
        }

        /// <summary>
        /// Gets the filename of the solution.
        /// </summary>
        /// <param name="isLegacy">Whether to get the legacy filename or not.</param>
        /// <returns>The name of the solution file.</returns>
        public string GetSolutionName(bool isLegacy)
        {
            var name = baseSolutionName;

            var baseName = Path.GetFileNameWithoutExtension(name);

            if (!isLegacy)
            {
                var coreName = Path.Combine($"{baseName}{ProjectConfig.CoreSuffix}.sln");

                if (fileSystem.FileExists(Path.Combine(SolutionRoot, coreName)))
                    return coreName;
            }

            return name;
        }

        public string GetSolutionPath(bool isLegacy) => Path.Combine(SolutionRoot, GetSolutionName(isLegacy));

        public string GetBuildFilter(bool isLegacy)
        {
            if (!string.IsNullOrWhiteSpace(Config.BuildFilter))
            {
                if (isLegacy || string.IsNullOrWhiteSpace(ProjectConfig.CoreSuffix))
                    return Config.BuildFilter;

                var filter = Config.BuildFilter;

                var pattern = string.Empty;

                if (filter.EndsWith("*"))
                {
                    if (filter.EndsWith(".*"))
                        pattern = ".*";
                    else
                        pattern = "*";
                }

                var baseFilter = filter.Substring(0, filter.Length - pattern.Length);

                baseFilter = baseFilter + ProjectConfig.CoreSuffix + pattern;

                return baseFilter;
            }

            return null;
        }

        public BuildProject[] GetProjects(bool isLegacy)
        {
            EnsureProjects();

            var filter = GetBuildFilter(isLegacy);

            if (filter == null)
                return projects;

            var wildcard = new WildcardPattern(filter);

            return projects.Where(p => wildcard.IsMatch(p.FileName)).ToArray();
        }

        private void EnsureProjects()
        {
            if (projects == null)
                projects = GetProjectsInternal();
        }

        public BuildProject GetPrimaryProject(bool isLegacy)
        {
            EnsureProjects();

            var candidates = projects.Where(p => p.Kind == ProjectKind.Normal && p.IsLegacy == isLegacy).ToArray();

            if (candidates.Length == 1)
                return candidates[0];

            var targetName = Config.Name;

            foreach (var candidate in candidates)
            {
                var name = candidate.Name;

                if (candidate.Name.EndsWith(ProjectConfig.CoreSuffix))
                    name = name.Substring(0, name.Length - ProjectConfig.CoreSuffix.Length);

                if (targetName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            throw new InvalidOperationException("Could not identify primary project");
        }

        public BuildProject GetTestProject(bool integration, bool isLegacy) =>
            integration ? GetIntegrationTestProject(isLegacy) : GetUnitTestProject(isLegacy);

        public BuildProject GetUnitTestProject(bool isLegacy) => GetTestProjectInternal(ProjectKind.UnitTest, "unit", isLegacy);
        public BuildProject GetIntegrationTestProject(bool isLegacy) => GetTestProjectInternal(ProjectKind.IntegrationTest, "integration", isLegacy);

        private BuildProject GetTestProjectInternal(ProjectKind kind, string displayKind, bool isLegacy)
        {
            EnsureProjects();

            var candidates = projects.Where(p => (p.Kind & kind) != 0 && p.IsLegacy == isLegacy).ToArray();

            if (candidates.Length == 1)
                return candidates[0];

            if (candidates.Length == 0)
                throw new InvalidOperationException($"Could not find any {displayKind} test projects");

            throw new InvalidOperationException($"Multiple {displayKind} test projects were found: {string.Join(", ", candidates.Select(v => v.Name))}");
        }

        public string GetTestDll(bool integration, BuildConfiguration configuration, bool isLegacy) =>
            integration ? GetIntegrationTestDll(configuration, isLegacy) : GetUnitTestDll(configuration, isLegacy);

        public string GetUnitTestDll(BuildConfiguration configuration, bool isLegacy) => GetTestDllInternal(ProjectKind.UnitTest, "unit", configuration, isLegacy);
        public string GetIntegrationTestDll(BuildConfiguration configuration, bool isLegacy) => GetTestDllInternal(ProjectKind.IntegrationTest, "integration", configuration, isLegacy);

        private string GetTestDllInternal(ProjectKind kind, string displayKind, BuildConfiguration configuration, bool isLegacy)
        {
            EnsureProjects();

            if (!isLegacy)
                throw new NotImplementedException($"Retrieving the {displayKind} test DLL for non-core projects is not implemented");

            var candidates = projects.Where(p => (p.Kind & kind) != 0 && p.IsLegacy == isLegacy).ToArray();

            if (candidates.Length == 1)
            {
                var output = GetProjectConfigurationDirectory(candidates[0], configuration);

                var dll = Path.Combine(output, $"{candidates[0].NormalizedName}.dll");

                if (!fileSystem.FileExists(dll))
                    throw new FileNotFoundException($"Could not find {displayKind} test DLL '{dll}'", dll);

                return dll;
            }

            if (candidates.Length == 0)
                throw new InvalidOperationException($"Could not find any {displayKind} test projects");

            throw new InvalidOperationException($"Multiple {displayKind} test projects were found: {string.Join(", ", candidates.Select(v => v.Name))}");
        }

        public string GetTestPowerShellDirectory(BuildProject project)
        {
            var projectDir = project.DirectoryName;

            var powerShellDir = Path.Combine(projectDir, "PowerShell");

            if (!fileSystem.DirectoryExists(powerShellDir))
                throw new DirectoryNotFoundException($"PowerShell unit test directory '{powerShellDir}' was not found.");

            return powerShellDir;
        }

        private BuildProject[] GetProjectsInternal()
        {
            powerShell.WriteVerbose("Enumerating solution projects");

            //Note that enumerating projects against the Windows Host takes a few seconds in WSL. GetTestResult
            //therefore delays initializing the environment due to it being an IIntegrationProvider, so its dynamic
            //parameters need to check to see whether any integration tests exist in the project. It will access its
            //projects shortly after this when it goes to build its help, but by this time the projects have already
            //been cached
            var files = fileSystem.EnumerateFiles(SolutionRoot, "*.csproj", SearchOption.AllDirectories);

            var results = new List<BuildProject>();

            var items = files.Select(f => new
            {
                Path = f,
                Name = Path.GetFileNameWithoutExtension(f)
            }).ToArray();

            if (items.Any(i => i.Name.EndsWith(ProjectConfig.CoreSuffix, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var item in items)
                {
                    var isLegacy = true;
                    var split = item.Name.Split('.');
                    var normalized = item.Name;

                    if (split[0].EndsWith(ProjectConfig.CoreSuffix, StringComparison.OrdinalIgnoreCase))
                    {
                        isLegacy = false;
                        split[0] = split[0].Substring(0, split[0].Length - ProjectConfig.CoreSuffix.Length);

                        normalized = string.Join(".", split);
                    }

                    powerShell.WriteVerbose($"Found project '{item.Path}' (Legacy: {(isLegacy ? "true" : "false")})");
                    results.Add(new BuildProject(item.Path, normalized, isLegacy));
                }
            }
            else
            {
                foreach (var item in items)
                {
                    powerShell.WriteVerbose($"Found project '{item.Path}'");
                    results.Add(new BuildProject(item.Path, item.Name, false));
                }
            }

            return results.ToArray();
        }

        /// <inheritdoc />
        public string GetPowerShellConfigurationDirectory(BuildConfiguration configuration)
        {
            var powerShellProjectName = GetPowerShellProjectName();

            var powerShellProjectDir = Path.Combine(SourceRoot, powerShellProjectName);

            if (!fileSystem.DirectoryExists(powerShellProjectDir))
                throw new DirectoryNotFoundException($"Could not find PowerShell Project directory '{powerShellProjectDir}'.");

            var binDir = Path.Combine(powerShellProjectDir, "bin");

            if (!fileSystem.DirectoryExists(binDir))
                throw new DirectoryNotFoundException($"Could not find PowerShell Project bin directory '{binDir}'");

            var configDir = Path.Combine(binDir, configuration.ToString());

            if (!fileSystem.DirectoryExists(configDir))
                throw new DirectoryNotFoundException($"Could not find PowerShell Project {configuration} directory '{configDir}'");

            return configDir;
        }

        /// <inheritdoc />
        public string GetPowerShellOutputDirectory(BuildConfiguration configuration, bool isLegacy)
        {
            var configDir = GetPowerShellConfigurationDirectory(configuration);

            string baseDir;

            if (isLegacy)
            {
                baseDir = configDir;
            }
            else
            {
                //If the module directory is outside the target framework folders, thats bad, because Publish-Module is going to look at the
                //target framework folder name when it tries to lookup what the *.psd1 filename is. You can't signify to Publish-Module what the *.psd1
                //name should be. As such, you need to invert your folder hierarchy, as described in PowerShellPackageProvider.CreatePowerShellPackage()

                //.NET SDK style projects may have a variety of subfolders for various target frameworks

                //Get the lowest .NET Framework folder
                string[] candidates = fileSystem.EnumerateDirectories(configDir, "net4*").ToArray();

                if (candidates.Length == 0)
                {
                    //Could not find a build for .NET Framework. Maybe we're building .NET Core instead
                    candidates = fileSystem.EnumerateDirectories(configDir, "netcore*").ToArray();
                }

                if (candidates.Length == 0)
                {
                    //.NET Standard maybe?
                    candidates = fileSystem.EnumerateDirectories(configDir, "netstandard*").ToArray();
                }

                if (candidates.Length == 0)
                {
                    //.NET 5+?
                    candidates = fileSystem.EnumerateDirectories(configDir, "net*").ToArray();
                }

                if (candidates.Length == 0)
                    throw new InvalidOperationException($"Couldn't find any {configuration} build candidates for PowerShell Project '{Config.PowerShellProjectName}'");

                baseDir = candidates.First();
            }

            //The PowerShell project should be inside a folder with the name we want to use for the module
            var moduleDir = Path.Combine(baseDir, Config.PowerShellModuleName);

            if (fileSystem.DirectoryExists(moduleDir))
                return moduleDir;

            return baseDir;
        }

        /// <inheritdoc />
        public string GetPowerShellProjectName(bool mandatory = true)
        {
            return GetProjectName(
                ProjectKind.PowerShell,
                Config.PowerShellProjectName,
                nameof(Config.PowerShellProjectName),
                () =>
                {
                    var expected = new[]
                    {
                        "System.Management.Automation",
                        "Microsoft.PowerShell.5.ReferenceAssemblies",
                        "PowerShellStandard.Library"
                    };

                    foreach (var project in projects)
                    {
                        var contents = fileSystem.ReadFileLines(project.FilePath);

                        //If we have a project that references a PowerShell library, we must have a PowerShell project
                        //that we just couldn't find (and it's best that our BuildTools config file be updated to say what it is,
                        //so we don't match any test projects here)
                        if (contents.Any(l => expected.Any(l.Contains)))
                            return true;
                    }

                    return false;
                }
            );
        }

        public string GetUnitTestProjectName() =>
            GetProjectName(ProjectKind.UnitTest, Config.UnitTestProjectName, nameof(Config.UnitTestProjectName));

        /// <inheritdoc />
        public string GetSourcePowerShellModuleManifest(bool relativePath = false, bool mandatory = true)
        {
            var projectName = GetPowerShellProjectName(mandatory);

            if (projectName == null)
                return null;

            //It could either be under the PowerShell project root, or a Resources subfolder

            var powerShellProjectDir = Path.Combine(SourceRoot, GetPowerShellProjectName());

            var psd1 = fileSystem.EnumerateFiles(powerShellProjectDir, "*.psd1").ToArray();

            if (psd1.Length == 0)
            {
                var resourcesDir = Path.Combine(powerShellProjectDir, "PowerShell", "Resources");

                if (fileSystem.DirectoryExists(resourcesDir))
                    psd1 = fileSystem.EnumerateFiles(resourcesDir, "*.psd1").ToArray();
            }

            if (psd1.Length == 0)
                throw new FileNotFoundException("Could not find a *.psd1 file");

            if (psd1.Length > 1)
                throw new InvalidOperationException("More than one *.psd1 file was found");

            if (relativePath)
                return GetSolutionRelativePath(psd1[0]);

            return psd1[0];
        }

        public bool TryGetVersionAttribPath(out string path)
        {
            var primaryProject = Path.Combine(SourceRoot, Config.Name);

            if (!fileSystem.DirectoryExists(primaryProject))
                throw new DirectoryNotFoundException($"Could not find primary project directory '{primaryProject}'.");

            var versionFile = Path.Combine(primaryProject, "Properties", "Version.cs");

            if (!fileSystem.FileExists(versionFile))
            {
                path = null;
                return false;
            }

            path = versionFile;
            return true;
        }

        public string GetVersionAttibPath()
        {
            if (!TryGetVersionAttribPath(out var path))
                throw new FileNotFoundException($"Could not find legacy version file '{path}'", path);                

            return path;
        }

        public string GetVersionPropsPath(bool relativePath = false)
        {
            var versionPath = Path.Combine(SolutionRoot, "build", "Version.props");

            if (!fileSystem.FileExists(versionPath))
                throw new FileNotFoundException($"Could not find version file '{versionPath}'", versionPath);

            if (relativePath)
                return GetSolutionRelativePath(versionPath);

            return versionPath;
        }

        private string GetSolutionRelativePath(string path)
        {
            string root;

            if (path.StartsWith(SourceRoot))
                root = SourceRoot;
            else
                root = SolutionRoot;

            var length = root.Length;

            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()) && !root.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                length++;

            return path.Substring(length);
        }

        private string GetProjectName(ProjectKind kind, string fallbackSetting, string fallbackSettingName, Func<bool> hasProjectType = null)
        {
            EnsureProjects();

            if (fallbackSetting != null)
            {
                if (!projects.Any(p => p.NormalizedName.Equals(fallbackSetting, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"Could not find any projects with {fallbackSettingName} '{fallbackSetting}'.");

                return fallbackSetting;
            }

            var candidates = projects.Where(p => (p.Kind & kind) != 0 && !p.IsLegacy).ToArray();

            if (candidates.Length == 1)
                return candidates[0].NormalizedName;

            if (hasProjectType != null && !hasProjectType())
                return null;

            throw new InvalidOperationException($"Cannot process PowerShell projects: setting '{fallbackSettingName}' was not specified and PowerShell project could not automatically be identified.");
        }

        public string GetProjectConfigurationDirectory(BuildProject project, BuildConfiguration configuration)
        {
            var projectDir = project.DirectoryName;

            if (!fileSystem.DirectoryExists(projectDir))
                throw new DirectoryNotFoundException($"Could not find project directory '{projectDir}'.");

            var binDir = Path.Combine(projectDir, "bin");

            if (!fileSystem.DirectoryExists(binDir))
                throw new DirectoryNotFoundException($"Could not find project bin directory '{binDir}'");

            var configDir = Path.Combine(binDir, configuration.ToString());

            if (!fileSystem.DirectoryExists(configDir))
                throw new DirectoryNotFoundException($"Could not find project {configuration} directory '{configDir}'");

            return configDir;
        }
    }
}
