namespace BuildTools
{
    public class SkipPackageTest : IPackageTest
    {
        public static readonly SkipPackageTest Instance = new SkipPackageTest();

        public string Command { get; }
    }
}
