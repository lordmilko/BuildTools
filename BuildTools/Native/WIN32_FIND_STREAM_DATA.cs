using System.Runtime.InteropServices;

namespace BuildTools
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class WIN32_FIND_STREAM_DATA
    {
        public long StreamSize;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)] //MAX_PATH + 36
        public string cStreamName;
    }
}