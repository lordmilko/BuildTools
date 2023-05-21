using System;
using System.Runtime.InteropServices;

namespace BuildTools
{
    interface IAlternateDataStreamService
    {
        void UnblockFile(string path);
    }

    class AlternateDataStreamService : IAlternateDataStreamService
    {
        private const string ZoneIdentifier = "Zone.Identifier";

        public void UnblockFile(string path)
        {
            if (IsFileBlocked(path))
                NativeMethods.DeleteFileW($"{path}:{ZoneIdentifier}");
        }

        public bool IsFileBlocked(string path)
        {
            WIN32_FIND_STREAM_DATA data = new WIN32_FIND_STREAM_DATA();
            var firstStream = NativeMethods.FindFirstStreamW(path, STREAM_INFO_LEVELS.FindStreamInfoStandard, data, 0);

            if (firstStream == new IntPtr(-1))
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());

            var suffix = ":$DATA";

            try
            {
                do
                {
                    var name = data.cStreamName.Replace(suffix, string.Empty).Trim(':');

                    if (name == ZoneIdentifier)
                        return true;
                } while (NativeMethods.FindNextStreamW(firstStream, data));
            }
            finally
            {
                NativeMethods.FindClose(firstStream);
            }

            return false;
        }
    }
}