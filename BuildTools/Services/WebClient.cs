namespace BuildTools
{
    interface IWebClient
    {
        void DownloadFile(string url, string outputFile);

        string GetString(string url);
    }

    class WebClient : IWebClient
    {
        private readonly System.Net.WebClient webClient = new System.Net.WebClient();

        public void DownloadFile(string url, string outputFile)
        {
            webClient.DownloadFile(url, outputFile);
        }

        public string GetString(string url)
        {
            return webClient.DownloadString(url);
        }
    }
}