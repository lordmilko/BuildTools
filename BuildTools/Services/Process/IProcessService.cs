namespace BuildTools
{
    interface IProcessService
    {
        string[] Execute(string fileName, ArgList arguments = default, string errorFormat = null, bool writeHost = false);

        bool IsRunning(string processName);
    }
}