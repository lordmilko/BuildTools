namespace BuildTools
{
    public class InvokeAppveyorAfterTestService : IAppveyorService
    {
        private readonly MeasureAppveyorCoverageService measureAppveyorCoverage;

        internal InvokeAppveyorAfterTestService(MeasureAppveyorCoverageService measureAppveyorCoverage)
        {
            this.measureAppveyorCoverage = measureAppveyorCoverage;
        }

        public void Execute(BuildConfiguration configuration, bool isLegacy)
        {
            measureAppveyorCoverage.Execute(configuration, isLegacy);
        }
    }
}
