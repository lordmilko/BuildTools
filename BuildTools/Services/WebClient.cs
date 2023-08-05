using System.Net.Http;
using System.Threading.Tasks;

namespace BuildTools
{
    interface IWebClient
    {
        void DownloadFile(string url, string outputFile);

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);

        string GetString(string url);
    }

    class WebClient : IWebClient
    {
        private readonly System.Net.WebClient webClient = new System.Net.WebClient();
        private readonly HttpClient httpClient = new HttpClient();

        public void DownloadFile(string url, string outputFile)
        {
            webClient.DownloadFile(url, outputFile);
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) =>
            httpClient.SendAsync(request);

        public string GetString(string url)
        {
            return webClient.DownloadString(url);
        }
    }
}
