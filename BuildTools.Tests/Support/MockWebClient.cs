using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    class MockWebClient : IWebClient, IMock<IWebClient>
    {
        public List<(string url, string outputFile)> Downloaded { get; } = new List<(string url, string outputFile)>();

        public void DownloadFile(string url, string outputFile)
        {
            Downloaded.Add((url, outputFile));
        }

        public void AssertDownloaded(string url, string outputFile)
        {
            foreach (var item in Downloaded)
            {
                if (item.url == url)
                {
                    Assert.AreEqual(outputFile, item.outputFile);
                    return;
                }
            }

            Assert.Fail($"Did not download URL '{url}'");
        }
    }
}