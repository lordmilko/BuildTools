using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    class MockWebClient : IWebClient, IMock<IWebClient>
    {
        public List<(string url, string outputFile)> DownloadedFile { get; } = new List<(string url, string outputFile)>();

        public Dictionary<string, string> DownloadedString { get; } = new Dictionary<string, string>();

        public void DownloadFile(string url, string outputFile)
        {
            DownloadedFile.Add((url, outputFile));
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            string GetResponse()
            {
                var uri = request.RequestUri.AbsolutePath;

                uri = uri.Substring(uri.LastIndexOf('/') + 1);

                switch (uri)
                {
                    case "deployments":
                        return GetDeploymentsResponse();

                    case "history":
                        return GetHistoryResponse();

                    default:
                        throw new NotImplementedException($"Don't know how to handle endpoint '{uri}'");
                }
            }

            return Task.FromResult(new HttpResponseMessage
            {
                Content = new StringContent(GetResponse())
            });
        }

        public string GetString(string url)
        {
            if (DownloadedString.TryGetValue(url, out var value))
                return value;

            throw new InvalidOperationException($"String to return for URL '{url}' has not been set.");
        }

        public void AssertDownloaded(string url, string outputFile)
        {
            foreach (var item in DownloadedFile)
            {
                if (item.url == url)
                {
                    Assert.AreEqual(outputFile, item.outputFile);
                    return;
                }
            }

            Assert.Fail($"Did not download URL '{url}'");
        }

        private string GetDeploymentsResponse()
        {
            //Note that the real structure of the deployments response is different from what
            //Appveyor's docs say

            return @"
{
   ""project"":{
      ""projectId"":22321,
      ""accountId"":2,
      ""accountName"":""appvyr"",
      ""builds"":[

      ],
      ""name"":""simple-web"",
      ""slug"":""simple-web"",
      ""repositoryType"":""gitHub"",
      ""repositoryScm"":""git"",
      ""repositoryName"":""AppVeyor/simple-web"",
      ""isPrivate"":false,
      ""skipBranchesWithoutAppveyorYml"":false,
      ""securityDescriptor"":{
      },
      ""created"":""2014-05-08T18:38:57.9163293+00:00"",
      ""updated"":""2014-07-14T10:16:26.9351867+00:00""
   },
   ""deployments"":[
      {
         ""deploymentId"":19475,
         ""build"":{
             ""buildId"":132746,
             ""buildNumber"":38,
             ""version"":""1.0.38"",
             ""message"":""Removed Start-Website"",
             ""branch"":""master""
         },
         ""environment"":{
             ""deploymentEnvironmentId"":27,
             ""name"":""agent test"",
             ""provider"":""Agent"",
             ""created"":""2014-04-01T17:56:41.30982+00:00"",
             ""updated"":""2014-08-12T22:35:51.9723883+00:00""
         },
         ""status"":""success"",
         ""started"":""2014-08-12T23:06:10.8776088+00:00"",
         ""finished"":""2014-08-12T23:06:25.0502019+00:00"",
         ""created"":""2014-08-12T23:06:07.9009315+00:00"",
         ""updated"":""2014-08-12T23:06:25.0502019+00:00""
      }
   ]
}";
        }

        private string GetHistoryResponse()
        {
            return @"
{
   ""project"":{
      ""projectId"":42438,
      ""accountId"":2,
      ""accountName"":""appvyr"",
      ""builds"":[

      ],
      ""name"":""wix-test"",
      ""slug"":""wix-test"",
      ""repositoryType"":""gitHub"",
      ""repositoryScm"":""git"",
      ""repositoryName"":""FeodorFitsner/wix-test"",
      ""isPrivate"":false,
      ""skipBranchesWithoutAppveyorYml"":false,
      ""created"":""2014-08-09T00:30:43.3327131+00:00""
   },
   ""builds"":[
      {
         ""buildId"":134174,
         ""jobs"":[

         ],
         ""buildNumber"":5,
         ""version"":""1.0.5"",
         ""message"":""Enabled diag mode"",
         ""branch"":""master"",
         ""commitId"":""d19740243e3ec5497345de0f7d828e66a7cd1a6b"",
         ""authorName"":""Feodor Fitsner"",
         ""authorUsername"":""FeodorFitsner"",
         ""committerName"":""Feodor Fitsner"",
         ""committerUsername"":""FeodorFitsner"",
         ""committed"":""2014-08-10T14:08:16+00:00"",
         ""messages"":[

         ],
         ""status"":""success"",
         ""started"":""2014-08-14T05:42:17.2696755+00:00"",
         ""finished"":""2014-08-14T05:43:47.4732355+00:00"",
         ""created"":""2014-08-14T05:39:30.8845902+00:00"",
         ""updated"":""2014-08-14T05:43:47.4732355+00:00""
      },
      {
         ""buildId"":129289,
         ""jobs"":[

         ],
         ""buildNumber"":3,
         ""version"":""1.0.3"",
         ""message"":""Added appveyor.yml"",
         ""branch"":""master"",
         ""commitId"":""28c6eec932c0e21eca5bb5571a722f850aa8bf6f"",
         ""authorName"":""Feodor Fitsner"",
         ""authorUsername"":""FeodorFitsner"",
         ""committerName"":""Feodor Fitsner"",
         ""committerUsername"":""FeodorFitsner"",
         ""committed"":""2014-08-09T00:33:34+00:00"",
         ""messages"":[

         ],
         ""status"":""success"",
         ""started"":""2014-08-09T15:42:45.7878479+00:00"",
         ""finished"":""2014-08-09T15:44:15.5828009+00:00"",
         ""created"":""2014-08-09T15:42:38.8315273+00:00"",
         ""updated"":""2014-08-09T15:44:15.5828009+00:00""
      }
   ]
}
";
        }
    }
}
