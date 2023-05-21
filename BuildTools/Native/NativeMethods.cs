using System;
using System.Runtime.InteropServices;

namespace BuildTools
{
    static class NativeMethods
    {
        private const string kernel32 = "kernel32.dll";

        [DllImport(kernel32, SetLastError = true)]
        internal static extern bool DeleteFileW(
            [MarshalAs(UnmanagedType.LPWStr), In] string lpFileName);

        [DllImport(kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr FindFirstStreamW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [In] STREAM_INFO_LEVELS InfoLevel,
            [MarshalAs(UnmanagedType.LPStruct), In, Out] WIN32_FIND_STREAM_DATA lpFindStreamData, //You're not supposed to use LPStruct with anything but Guid, but ref and out didn't work!
            [In] int dwFlags);

        [DllImport(kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool FindNextStreamW(
            [In] IntPtr hFindStream,
            [MarshalAs(UnmanagedType.LPStruct), In, Out] WIN32_FIND_STREAM_DATA lpFindStreamData);

        [DllImport(kernel32, SetLastError = true)]
        internal static extern bool FindClose(
            [In] IntPtr hFindFile);
    }
}
