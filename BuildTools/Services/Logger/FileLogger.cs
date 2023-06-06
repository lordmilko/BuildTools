using System;
using System.IO;

namespace BuildTools
{
    interface IFileLogger
    {
        void LogBuild(string message);
    }

    class FileLogger : IFileLogger
    {
        private IProjectConfigProvider configProvider;

        public FileLogger(IProjectConfigProvider configProvider)
        {
            this.configProvider = configProvider;
        }

        public void LogBuild(string message)
        {
            var path = Path.Combine(Path.GetTempPath(), $"{configProvider.Config.Name}.Build.log");

            File.AppendAllText(path, $"{DateTime.Now} {message}{Environment.NewLine}");
        }
    }
}