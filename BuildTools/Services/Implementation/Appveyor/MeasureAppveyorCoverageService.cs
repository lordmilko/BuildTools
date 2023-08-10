using System;
using System.IO;
using System.Xml.Linq;

namespace BuildTools
{
    class MeasureAppveyorCoverageService
    {
        private readonly DependencyProvider dependencyProvider;
        private readonly EnvironmentService environmentService;
        private readonly IFileSystemProvider fileSystem;
        private readonly IProjectConfigProvider configProvider;

        //Lazily initialized so we don't try and install opencover if coverage is not supported
        private readonly Lazy<GetCoverageService> getCoverageService;

        private readonly IProcessService processService;
        private readonly Logger logger;

        public MeasureAppveyorCoverageService(
            DependencyProvider dependencyProvider,
            EnvironmentService environmentService,
            IFileSystemProvider fileSystem,
            IProjectConfigProvider configProvider,
            Lazy<GetCoverageService> getCoverageService,
            IProcessService processService,
            Logger logger)
        {
            this.dependencyProvider = dependencyProvider;
            this.environmentService = environmentService;
            this.fileSystem = fileSystem;
            this.configProvider = configProvider;
            this.getCoverageService = getCoverageService;
            this.processService = processService;
            this.logger = logger;
        }

        public void Execute(BuildConfiguration configuration, bool isLegacy)
        {
            if (!configProvider.HasFeature(Feature.Coverage))
                return;

            logger.LogHeader("Calculating code coverage");

            getCoverageService.Value.GetCoverage(new CoverageConfig(configProvider.Config.TestTypes)
            {
                Configuration = configuration
            }, isLegacy);

            var summaryPath = Path.Combine(Path.GetTempPath(), "report", "Summary.xml");

            if (fileSystem.FileExists(summaryPath))
                fileSystem.DeleteFile(summaryPath);

            getCoverageService.Value.CreateReport(CoverageReportType.XmlSummary);

            if (!fileSystem.FileExists(summaryPath))
                throw new FileNotFoundException($"Cannot find coverage report '{summaryPath}'", summaryPath);

            var summaryXmlContents = fileSystem.ReadFileText(summaryPath);

            var xDoc = XDocument.Parse(summaryXmlContents);

            var lineCoverage = Convert.ToDouble(xDoc.Element("CoverageReport").Element("Summary").Element("Linecoverage").Value);

            if (lineCoverage < configProvider.Config.CoverageThreshold)
            {
                var message = $"Code coverage was {lineCoverage}%. Coverage must be higher than {configProvider.Config.CoverageThreshold}%";

                logger.LogAttention(message);

                throw new InvalidOperationException(message);
            }
            else
            {
                logger.LogInformation($"\tCoverage report completed with {lineCoverage}% code coverage");

                if (environmentService.IsAppveyor)
                {
                    logger.LogInformation("\tUploading coverage to codecov");

                    var codecovArgs = new ArgList
                    {
                        "-f",
                        $"\"{GetCoverageService.OpenCoverOutput}\"",
                        "--required" //codecov will return 0 on failure unless --required is specified
                    };

                    var codecov = dependencyProvider.Install(WellKnownDependency.CodeCov);

                    //It seems Appveyor starts with a current directory of system32, but sets the PowerShell current directory
                    //to your project path. All cmdlets that run in C# cmdlets will have the default working directory of system32.
                    //Thus, it is up to us to ensure our working directory is correct when we try and execute codecov

                    fileSystem.WithCurrentDirectory(
                        configProvider.SolutionRoot,
                        () => processService.Execute(codecov.Path, codecovArgs, writeHost: true)
                    );
                }
            }
        }
    }
}
