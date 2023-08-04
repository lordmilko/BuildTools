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
        private GetCoverageService getCoverageService;
        private ProcessService processService;
        private Logger logger;

        public MeasureAppveyorCoverageService(
            EnvironmentService environmentService,
            IFileSystemProvider fileSystem,
            IProjectConfigProvider configProvider,
            GetCoverageService getCoverageService,
            ProcessService processService,
            Logger logger)
        {
            this.environmentService = environmentService;
            this.fileSystem = fileSystem;
            this.configProvider = configProvider;
            this.getCoverageService = getCoverageService;
            this.processService = processService;
            this.logger = logger;
        }

        public void Execute(BuildConfiguration buildConfiguration, bool isLegacy)
        {
            logger.LogHeader("Calculating code coverage");

            getCoverageService.GetCoverage(new CoverageConfig
            {
                Configuration = buildConfiguration
            }, isLegacy);

            var summaryPath = Path.Combine(Path.GetTempPath(), "report", "Summary.xml");

            if (fileSystem.FileExists(summaryPath))
                fileSystem.DeleteFile(summaryPath);

            getCoverageService.CreateReport(CoverageReportType.XmlSummary);

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

                    var openCoverXml = Path.Combine(Path.GetTempPath(), "opencover.xml");
                    processService.Execute("cmd", $"/c \"codecov - f \\\"{GetCoverageService.OpenCoverOutput}\\\" 2 > nul\"");
                }
            }
        }
    }
}
