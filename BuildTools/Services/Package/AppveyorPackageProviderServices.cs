using BuildTools.PowerShell;

namespace BuildTools
{
    class AppveyorPackageProviderServices
    {
        public IFileSystemProvider FileSystem { get; }
        public EnvironmentService Environment { get; }
        public IProjectConfigProvider ConfigProvider { get; }
        public IPowerShellService PowerShell { get; }
        public IProcessService Process { get; }
        public GetVersionService GetVersion { get; }
        public NewPackageService NewPackage { get; }
        public Logger Logger { get; }
        public IZipService Zip { get; }

        public AppveyorPackageProviderServices(
            IFileSystemProvider fileSystem,
            EnvironmentService environmentService,
            IProjectConfigProvider configProvider,
            IPowerShellService powerShell,
            IProcessService processService,
            GetVersionService getVersionService,
            NewPackageService newPackage,
            Logger logger,
            IZipService zip)
        {
            FileSystem = fileSystem;
            Environment = environmentService;
            ConfigProvider = configProvider;
            PowerShell = powerShell;
            Process = processService;
            GetVersion = getVersionService;
            NewPackage = newPackage;
            Logger = logger;
            Zip = zip;
        }
    }
}
