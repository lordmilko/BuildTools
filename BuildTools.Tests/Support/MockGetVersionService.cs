using System;

namespace BuildTools.Tests
{
    class MockGetVersionService : GetVersionService, IMock<GetVersionService>
    {
        public Version FileVersion { get; set; }

        public MockGetVersionService() : base(null, null, null, null)
        {
        }

        public override VersionTable GetVersion(bool isLegacy)
        {
            return new VersionTable(
                null,
                null,
                FileVersion,
                null,
                null,
                null,
                null
            );
        }
    }
}
