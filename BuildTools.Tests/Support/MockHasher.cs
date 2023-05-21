namespace BuildTools.Tests
{
    class MockHasher : IHasher, IMock<IHasher>
    {
        public string Hash { get; set; }

        public string HashFile(string fileName)
        {
            return Hash;
        }
    }
}