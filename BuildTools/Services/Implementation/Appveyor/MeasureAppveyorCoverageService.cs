using System;
using System.IO;
using System.Xml.Linq;

namespace BuildTools
{
    class MeasureAppveyorCoverageService
    {
        private EnvironmentService environmentService;
        private IFileSystemProvider fileSystem;
        private IProjectConfigProvider configProvider;

        //Lazily initialized so we don't try and install opencover if coverage is not supported
        private Lazy<GetCoverageService> getCoverageService;

        private IProcessService processService;
        private Logger logger;

        public MeasureAppveyorCoverageService(
            EnvironmentService environmentService,
            IFileSystemProvider fileSystem,
            IProjectConfigProvider configProvider,
            Lazy<GetCoverageService> getCoverageService,
            IProcessService processService,
            Logger logger)
        {
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
                    processService.Execute("cmd", $"/c \"codecov - f \\\"{GetCoverageService.OpenCoverOutput}\\\" 2 > nul\"", writeHost: true);
                }
            }
        }
    }
}
