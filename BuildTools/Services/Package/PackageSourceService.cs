using System;
using System.IO;
using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    abstract class PackageSourceService
    {
        public static readonly string RepoName;
        public static readonly string RepoLocation;
        public static readonly string PackageLocation;

        static PackageSourceService()
        {
            var temp = Path.GetTempPath();

            RepoName = "TempRepository";
            RepoLocation = Path.Combine(temp, RepoName);
            PackageLocation = Path.Combine(temp, "TempPackages");
        }

        protected IPowerShellService PowerShell { get; }
        private IFileSystemProvider fileSystem;
        private Logger logger;

        private LangType langType;

        protected PackageSourceService(
            LangType langType,
            IPowerShellService powerShell,
            IFileSystemProvider fileSystem,
            Logger logger)
        {
            this.langType = langType;
            PowerShell = powerShell;
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        public void Install()
        {
            logger.LogInformation($"\t\tInstalling temp {langType} repository");

            if (fileSystem.DirectoryExists(RepoLocation))
            {
                logger.LogAttention("\t\t\tRemoving repository folder left over from previous run...");

                fileSystem.DeleteDirectory(RepoLocation);
            }

            logger.LogInformation("\t\t\tCreating repository folder");
            fileSystem.CreateDirectory(RepoLocation);

            var sources = GetPackageSource();

            if (sources.Any(s => s == RepoName))
            {
                logger.LogAttention("\t\t\tRemoving repository left over from previous run...");
                UnregisterPackageSource();
            }

            logger.LogInformation("\t\t\tRegistering temp repository");
            RegisterPackageSource();
        }

        public void Uninstall()
        {
            logger.LogInformation($"\t\tUninstall temp {langType} repository");

            logger.LogInformation("\t\t\tUnregistering temp repository");
            UnregisterPackageSource();

            logger.LogInformation("\t\t\tRemoving temp repository folder");
            fileSystem.DeleteDirectory(RepoLocation);
        }

        protected abstract string[] GetPackageSource();

        protected abstract void RegisterPackageSource();

        protected abstract void UnregisterPackageSource();

        public static void WithTempCopy(
            string sourcePath,
            IFileSystemProvider fileSystem,
            Action<string> action,
            string folderName = null)
        {
            folderName ??= Path.GetDirectoryName(sourcePath);

            var tempPath = Path.Combine(RepoLocation, "TempOutput", folderName);

            fileSystem.CopyDirectory(sourcePath, tempPath, true);

            try
            {
                action(tempPath);
            }
            finally
            {
                fileSystem.DeleteDirectory(tempPath);
            }
        }
    }
}
