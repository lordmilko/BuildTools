using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    class MockProcessService : IProcessService, IMock<IProcessService>
    {
        public List<string> Executed { get; } = new List<string>();

        public void Execute(string fileName, string arguments = null, string errorFormat = null)
        {
            Executed.Add($"{fileName} {arguments}");
        }

        public void AssertExecuted(string fileNameAndArgs)
        {
            Assert.IsTrue(Executed.Contains(fileNameAndArgs), $"'{fileNameAndArgs}' was not executed");
        }
    }
}