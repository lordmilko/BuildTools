namespace BuildTools
{
    interface IWebClient
    {
        void DownloadFile(string url, string outputFile);
    }
}