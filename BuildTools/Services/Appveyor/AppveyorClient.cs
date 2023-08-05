using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace BuildTools
{
    interface IAppveyorClient
    {
        AppveyorProjectDeployment[] GetAppveyorDeployments();

        AppveyorProjectHistoryBuild[] GetBuildHistory();

        void ResetBuildVersion();
    }

    class AppveyorClient : IAppveyorClient
    {
        private readonly EnvironmentService environmentService;
        private readonly IWebClient webClient;

        public AppveyorClient(EnvironmentService environmentService, IWebClient webClient)
        {
            this.environmentService = environmentService;
            this.webClient = webClient;
        }

        #region API

        public AppveyorProjectDeployment[] GetAppveyorDeployments() =>
            Get<AppveyorProjectDeployments>("deployments").Deployments;

        public AppveyorProjectHistoryBuild[] GetBuildHistory() =>
            Get<AppveyorProjectHistory>("history?recordsNumber=10").Builds;

        public void ResetBuildVersion() =>
            Put("settings/build-number", "{ \"nextBuildNumber\": 2 }"); //We are 1, so the next one will be 2

        #endregion

        private T Get<T>(string query)
        {
            var response = ExecuteRequest(HttpMethod.Get, query, null);

            var deserializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new DateTimeFormat("o")
            });

            using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(response)))
            {
                return (T) deserializer.ReadObject(stream);
            }
        }

        private void Put(string query, string body) => ExecuteRequest(HttpMethod.Put, query, body);

        private string ExecuteRequest(HttpMethod method, string query, string body)
        {
            if (string.IsNullOrEmpty(environmentService.AppveyorAPIToken))
                throw new InvalidOperationException($"Environment variable '{WellKnownEnvironmentVariable.AppveyorAPIToken}' is not set. When running in AppVeyor, this environment variable should be set under the Settings of the project.");

            if (string.IsNullOrEmpty(environmentService.AppveyorAccountName))
                throw new InvalidOperationException($"Environment variable '{WellKnownEnvironmentVariable.AppveyorAccountName}' is not set.");

            if (string.IsNullOrEmpty(environmentService.AppveyorProjectSlug))
                throw new InvalidOperationException($"Environment variable '{WellKnownEnvironmentVariable.AppveyorProjectSlug}' is not set.");

            var requestUri = $"https://ci.appveyor.com/api/projects/{environmentService.AppveyorAccountName}/{environmentService.AppveyorProjectSlug}";

            if (query != null)
                requestUri = $"{requestUri}/{query}";

            var message = new HttpRequestMessage(method, requestUri);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", environmentService.AppveyorAPIToken);

            if (body != null)
                message.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = webClient.SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();

            response.EnsureSuccessStatusCode();

            var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            return result;
        }
    }
}
