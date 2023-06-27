namespace BuildTools.Tests
{
    class MockVsProductLocator : IVsProductLocator
    {
        public string GetMSBuild() => "C:\\msbuild.exe";

        public string GetVSTest() => "C:\\vstest.console.exe";
    }
}
