using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace BuildTools
{
    class ProjectConfigProvider : IProjectConfigProvider
    {
        private IFileSystemProvider fileSystem;

        private readonly string[] srcFolders =
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

        internal ProjectConfigProvider(ProjectConfig config, string buildRoot, IFileSystemProvider fileSystem)
        {
            this.fileSystem = fileSystem;

            Config = config;
            SolutionRoot = CalculateSolutionRoot(buildRoot);

            if (config.SourceFolder != null)
                SourceRoot = Path.Combine(SolutionRoot, config.SourceFolder);
            else
                SourceRoot = CalculateSourceRoot();
        }

        private string CalculateSolutionRoot(string buildRoot)
        {
            var subDirs = new List<string>();
            subDirs.AddRange(srcFolders);

            if (!string.IsNullOrWhiteSpace(Config.SourceFolder))
                subDirs.Add(Config.SourceFolder);

            while (buildRoot != null)
            {
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
            if (Config.SourceFolder != null)
            {
                var path = Path.Combine(SolutionRoot, Config.SourceFolder);

                if (!fileSystem.DirectoryExists(path))
                    throw new InvalidOperationException($"Specified {nameof(ProjectConfig.SourceFolder)} '{path}' does not exist.");

                return path;
            }
            else
            {
                //Don't know what the source root is, so try guess

                foreach (var dir in srcFolders)
                {
                    var path = Path.Combine(SolutionRoot, dir);

                    if (fileSystem.DirectoryExists(path))
                        return path;
                }

                return SolutionRoot;
            }
        }

        /// <summary>
        /// Gets the filename of the solution.
        /// </summary>
        /// <param name="isLegacy">Whether to get the legacy filename or not.</param>
        /// <returns>The name of the solution file.</returns>
        public string GetSolutionName(bool isLegacy)
        {
            var name = Config.SolutionName;

            var baseName = Path.GetFileNameWithoutExtension(name);

            if (!isLegacy && !string.IsNullOrWhiteSpace(ProjectConfig.CoreSuffix))
                baseName += ProjectConfig.CoreSuffix;

            return baseName + ".sln";
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

        private BuildProject[] GetProjectsInternal()
        {
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
                    bool isLegacy = !item.Name.EndsWith(ProjectConfig.CoreSuffix, StringComparison.OrdinalIgnoreCase);

                    results.Add(new BuildProject(item.Path, isLegacy));
                }
            }
            else
                results.AddRange(items.Select(i => new BuildProject(i.Path, false)));

            return results.ToArray();
        }

        public string GetPowerShellOutputDirectory(BuildConfiguration buildConfiguration, bool isLegacy)
        {
            var powerShellProjectName = GetPowerShellProjectName();

            var powerShellProjectDir = Path.Combine(SourceRoot, powerShellProjectName);

            if (!fileSystem.DirectoryExists(powerShellProjectDir))
                throw new DirectoryNotFoundException($"Could not find PowerShell Project directory '{powerShellProjectDir}'.");

            var binDir = Path.Combine(powerShellProjectDir, "bin");

            if (!fileSystem.DirectoryExists(binDir))
                throw new DirectoryNotFoundException($"Could not find PowerShell Project bin directory '{binDir}'");

            var configDir = Path.Combine(binDir, buildConfiguration.ToString());

            if (!fileSystem.DirectoryExists(configDir))
                throw new DirectoryNotFoundException($"Could not find PowerShell Project {buildConfiguration} directory '{configDir}'");

            string baseDir;

            if (isLegacy)
            {
                baseDir = configDir;
            }
            else
            {
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
                    throw new InvalidOperationException($"Couldn't find any Core {buildConfiguration} build candidates for {powerShellProjectName}");

                baseDir = candidates.First();
            }

            //The PowerShell project should be inside a folder with the name we want to use for the module
            var moduleDir = Path.Combine(baseDir, Config.PowerShellModuleName);

            if (fileSystem.DirectoryExists(moduleDir))
                return moduleDir;

            return baseDir;
        }

        public string GetPowerShellProjectName()
        {
            var name = Config.PowerShellProjectName;

            if (name == null)
                throw new InvalidOperationException($"Cannot process PowerShell projects: setting '{nameof(Config.PowerShellProjectName)}' was not specified");

            return name;
        }

        public string GetPowerShellModuleManifest()
        {
            //It could either be under the PowerShell project root, or a Resources subfolder

            var powerShellProjectDir = Path.Combine(SourceRoot, GetPowerShellProjectName());

            var psd1 = fileSystem.EnumerateFiles(powerShellProjectDir, "*.psd1").ToArray();

            if (psd1.Length == 0)
            {
                var resourcesDir = Path.Combine(powerShellProjectDir, "PowerShell\\Resources");

                if (fileSystem.DirectoryExists(resourcesDir))
                    psd1 = fileSystem.EnumerateFiles(resourcesDir, "*.psd1").ToArray();
            }

            if (psd1.Length == 0)
                throw new FileNotFoundException("Could not find a *.psd1 file");

            if (psd1.Length > 1)
                throw new InvalidOperationException("More than one *.psd1 file was found");

            return psd1[0];
        }
    }
}
