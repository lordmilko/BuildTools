using System;
using System.IO;

namespace BuildTools
{
    interface IFileLogger
    {
        string GetLogFile(LogKind kind);

        void LogBuild(string message);
    }

    class FileLogger : IFileLogger
    {
        private readonly string buildLog;
        private readonly string integrationLog;

        public FileLogger(IProjectConfigProvider configProvider)
        {
            var temp = Path.GetTempPath();

            buildLog = Path.Combine(temp, $"{configProvider.Config.Name}.Build.log");
            integrationLog = Path.Combine(temp, $"{configProvider.Config.Name}.IntegrationTests.log");
        }

        public string GetLogFile(LogKind kind)
        {
            switch (kind)
            {
                case LogKind.Build:
                    return buildLog;

                case LogKind.Integration:
                    return integrationLog;

                default:
                    throw new NotImplementedException($"Don't know what log file to return for '{nameof(LogKind)}' '{kind}'.");
            }
        }

        public void LogBuild(string message)
        {
            File.AppendAllText(buildLog, $"{DateTime.Now} {message}{Environment.NewLine}");
        }
    }
}
