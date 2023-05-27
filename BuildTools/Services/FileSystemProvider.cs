using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BuildTools
{
    interface IFileSystemProvider
    {
        bool DirectoryExists(string path);

        bool FileExists(string path);

        IEnumerable<string> EnumerateFiles(string path);

        Version GetVersionInfo(string fileName);
    }

    class FileSystemProvider : IFileSystemProvider
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);

        public bool FileExists(string path) => File.Exists(path);

        public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

        public Version GetVersionInfo(string fileName)
        {
            return new Version(FileVersionInfo.GetVersionInfo(fileName).FileVersion);
        }
    }
}
