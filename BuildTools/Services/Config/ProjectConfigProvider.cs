using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuildTools
{
    class ProjectConfigProvider : IProjectConfigProvider
    {
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

        internal ProjectConfigProvider(ProjectConfig config, string buildRoot, IFileSystemProvider fileSystem)
        {
            Config = config;
            SolutionRoot = CalculateSolutionRoot(buildRoot, fileSystem);

            if (config.SourceFolder != null)
                SourceRoot = Path.Combine(SolutionRoot, config.SourceFolder);
            else
                SourceRoot = CalculateSourceRoot(fileSystem);
        }

        private string CalculateSolutionRoot(string buildRoot, IFileSystemProvider fileSystem)
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

        private string CalculateSourceRoot(IFileSystemProvider fileSystem)
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

        public string GetSolutionName(bool isLegacy)
        {
            if (Config.SolutionName.IsLeft)
                return Config.SolutionName.Left(new ProjectConfigResolutionContext { IsLegacy = isLegacy });

            return Config.SolutionName.Right;
        }

        public string GetSolutionPath(bool isLegacy) => Path.Combine(SolutionRoot, GetSolutionName(isLegacy));
    }
}
