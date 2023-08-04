using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using BuildTools.PowerShell;

namespace BuildTools
{
    abstract class AppveyorPackageProvider
    {
        private AppveyorPackageProviderServices services;

        protected IFileSystemProvider fileSystem => services.FileSystem;
        protected EnvironmentService environmentService => services.Environment;
        protected IProjectConfigProvider configProvider => services.ConfigProvider;
        protected IPowerShellService powerShell => services.PowerShell;
        protected IProcessService processService => services.Process;
        protected GetVersionService getVersionService => services.GetVersion;
        protected NewPackageService newPackageService => services.NewPackage;
        protected Logger logger => services.Logger;
        protected IZipService zip => services.Zip;

        protected AppveyorPackageProvider(AppveyorPackageProviderServices services)
        {
            this.services = services;
        }

        protected void TestPackage(PackageConfig config)
        {
            logger.LogInformation("\t\tTesting package");

            var nupkg = GetCSharpNupkg();

            ExtractPackage(nupkg, extractFolder =>
            {
                TestPackageDefinition(config, extractFolder);
                TestPackageContents(config, extractFolder);
            });

            TestPackageInstalls(config);
        }

        protected abstract void TestPackageDefinition(PackageConfig config, string extractFolder);

        protected abstract void TestPackageContents(PackageConfig config, string extractFolder);

        protected abstract void TestPackageInstalls(PackageConfig config);

        protected void MoveAppveyorPackages(string suffix)
        {
            if (environmentService.IsAppveyor)
            {
                logger.LogInformation("\t\t\tMoving Appveyor artifacts");

                newPackageService.MovePackages(suffix ?? string.Empty, configProvider.SolutionRoot);
            }
            else
            {
                logger.LogInformation("\t\t\t\tClearing repo (not running under Appveyor)");
                ClearRepo();
            }
        }

        private void ClearRepo() => fileSystem.DeleteDirectory(PackageSourceService.RepoLocation);

        protected string GetDebugTargetFramework()
        {
            return "netstandard2.0"; //temp
        }

        protected void ExtractPackage(string path, Action<string> action)
        {
            //var newName = Path.ChangeExtension(path, ".zip");

            var parentDir = Path.GetDirectoryName(path);
            var subDir = Path.GetFileNameWithoutExtension(path);

            var extractFolder = Path.Combine(parentDir, subDir);

            try
            {
                zip.ExtractToDirectory(path, extractFolder);

                action(extractFolder);
            }
            finally
            {
                if (fileSystem.DirectoryExists(extractFolder))
                    fileSystem.DeleteDirectory(extractFolder);
            }
        }

        protected void TestPackageContents(string extractFolder, IList<string> required)
        {
            logger.LogInformation("\t\t\tValidating package contents");

            var existing = fileSystem.EnumerateDirectoryFileSystemEntries(extractFolder, "*", SearchOption.AllDirectories)
                .Select(v => new
                {
                    Name = v.Substring(extractFolder.Length).TrimStart(Path.DirectorySeparatorChar),
                    IsFolder = fileSystem.DirectoryExists(v)
                }).ToArray();

            var requiredWildcards = required.Select(v => new
            {
                Name = v,
                Wildcard = new WildcardPattern(v, WildcardOptions.IgnoreCase)
            }).ToArray();

            var found = new List<string>();
            var illegal = new List<string>();

            foreach (var item in existing)
            {
                if (item.IsFolder)
                {
                    //Do we have a folder that contains a wildcard that matches this folder? (e.g. packages\* covers packages\foo)
                    var match = requiredWildcards.Where(v => v.Wildcard.IsMatch(item.Name)).ToArray();

                    if (match.Length == 0)
                    {
                        //There isn't a wildcard that covers this folder, but if there are actually any items contained under this folder
                        //then transitively this folder is allowed
                        match = requiredWildcards.Where(v => new WildcardPattern($"{item.Name}\\*").IsMatch(v.Name)).ToArray();

                        //If there is a match, we don't care - we don't whitelist empty folders, so we'll leave it up to the file processing block
                        //to decide whether the required files have been found or not
                        if (match.Length == 0)
                            illegal.Add(item.Name);
                    }
                    else
                    {
                        //Add our wildcard folder (e.g. packages\*)
                        found.AddRange(match.Select(v => v.Name));
                    }
                }
                else
                {
                    //If there isnt a required item that case insensitively matches a file that appears
                    //to exist, then that file must be "extra" and is therefore considered illegal
                    var match = requiredWildcards.Where(v => v.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)).ToArray();

                    if (match.Length == 0)
                    {
                        //We don't have a direct match, however maybe we have a folder that contains a wildcard
                        //that matches this file (e.g. packages\* covers packages\foo.txt)
                        match = requiredWildcards.Where(v => v.Wildcard.IsMatch(item.Name)).ToArray();
                    }

                    if (match.Length == 0)
                        illegal.Add(item.Name);
                    else
                        found.AddRange(match.Select(v => v.Name));
                }
            }

            if (illegal.Count > 0)
            {
                var str = string.Join(Environment.NewLine, illegal.OrderBy(v => v).Select(v => $"'{v}'"));
                throw new InvalidOperationException($"Package contained illegal items:{Environment.NewLine}{str}");
            }

            var missing = required.Except(found).ToArray();

            if (missing.Length > 0)
            {
                var str = string.Join(Environment.NewLine, missing.OrderBy(v => v).Select(v => $"'{v}'"));
                throw new InvalidOperationException($"Package is missing required items:{Environment.NewLine}{str}");
            }
        }

        protected string GetCSharpNupkg()
        {
            var primaryProject = configProvider.GetPrimaryProject(false);

            var nupkg = fileSystem.EnumerateFiles(PackageSourceService.RepoLocation, "*.nupkg").Where(v => !v.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase) && !v.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (nupkg.Length == 0)
                throw new FileNotFoundException($"Could not find nupkg for project '{primaryProject.NormalizedName}'");

            if (nupkg.Length > 1)
            {
                var builder = new StringBuilder();
                builder.AppendFormat("Found more than one nupkg for project '{0}': ", primaryProject.NormalizedName);

                var names = string.Join(", ", nupkg.Select(Path.GetFileName));

                builder.Append(names);

                throw new InvalidOperationException(builder.ToString());
            }

            return nupkg[0];
        }
    }
}
