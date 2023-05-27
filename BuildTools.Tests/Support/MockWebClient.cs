using System;
using System.Collections.Generic;
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
    }
}