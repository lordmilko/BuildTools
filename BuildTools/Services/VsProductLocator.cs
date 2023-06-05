using System.IO;
using System.Linq;

namespace BuildTools
{
    /// <summary>
    /// Locates products commonly found in or associated with Visual Studio
    /// </summary>
    interface IVsProductLocator
    {
        string GetMSBuild();

        string GetVSTest();
    }

    class VsProductLocator : IVsProductLocator
    {
        private DependencyProvider dependencyProvider;
        private ChocolateyDependencyInstaller chocolateyDependencyInstaller;
        private IFileSystemProvider fileSystem;
        private IProcessService processService;

        private string vswhere;
        private const string vsTestFileName = "vstest.console.exe";

        public VsProductLocator(
            DependencyProvider dependencyProvider,
            ChocolateyDependencyInstaller chocolateyDependencyInstaller,
            IFileSystemProvider fileSystem,
            IProcessService processService)
        {
            this.dependencyProvider = dependencyProvider;
            this.chocolateyDependencyInstaller = chocolateyDependencyInstaller;
            this.fileSystem = fileSystem;
            this.processService = processService;

            vswhere = GetVSWhere();
        }

        public string GetMSBuild()
        {
            var vswhereArgs = new ArgList
            {
                "-latest",
                "-requires",
                "Microsoft.Component.MSBuild",
                "-find",
                "MSBuild\\**\\Bin\\MSBuild.exe"
            };

            var msbuild = processService.Execute(vswhere, vswhereArgs).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(msbuild) || !fileSystem.FileExists(msbuild))
            {
                msbuild = "C:\\Program Files (x86)\\MSBuild\\14.0\\bin\\amd64\\msbuild.exe";

                if (!fileSystem.FileExists(msbuild))
                    throw new FileNotFoundException("Could not find a standalone version of MSBuild or a version included with Visual Studio");
            }

            return msbuild;
        }

        public string GetVSTest()
        {
            var vswhereArgs = new ArgList
            {
                "-latest",
                "-products",
                "*",
                "-requires",
                "Microsoft.VisualStudio.Workload.ManagedDesktop",
                "Microsoft.VisualStudio.Workload.Web",
                "-requiresAny",
                "-property",
                "installationPath"
            };

            var path = processService.Execute(vswhere, vswhereArgs).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(path) || !fileSystem.DirectoryExists(path))
            {
                path = "C:\\Program Files (x86)\\Microsoft Visual Studio 14.0";

                if (!fileSystem.DirectoryExists(path))
                    throw new FileNotFoundException($"Could not find {vsTestFileName}");
            }

            var vstest = Path.Combine(path, "Common7\\IDE\\CommonExtensions\\Microsoft\\TestWindow", vsTestFileName);

            if (!fileSystem.FileExists(vstest))
                throw new FileNotFoundException($"Expected to find {vsTestFileName} at path '{vstest}' however this was not the case.");

            return vstest;
        }

        private string GetVSWhere()
        {
            dependencyProvider.Install(WellKnownDependency.vswhere);
            var path = chocolateyDependencyInstaller.GetChocolateyCommand(WellKnownDependency.vswhere);

            return path;
        }
    }
}
