using System.Linq;
using System.Text;

namespace BuildTools
{
    public interface IAppveyorService
    {
        void Execute(BuildConfiguration configuration, bool isLegacy);
    }

    public abstract class AppveyorService : IAppveyorService
    {
        protected IProjectConfigProvider configProvider;
        internal Logger logger;

        internal AppveyorService(IProjectConfigProvider configProvider, Logger logger)
        {
            this.configProvider = configProvider;
            this.logger = logger;
        }

        public abstract void Execute(BuildConfiguration configuration, bool isLegacy);

        protected void LogHeader(string message, bool isLegacy)
        {
            var builder = new StringBuilder();

            var supportsLegacy = configProvider.GetProjects(true).Any();

            builder.Append(message);

            if (supportsLegacy)
                builder.AppendFormat(" (Core: {0})", !isLegacy);

            logger.LogHeader(builder.ToString());
        }
    }
}
