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

        void DeleteDirectory(string path);

        void DeleteFile(string path);

        IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        Version GetVersionInfo(string fileName);

        string GetFileText(string path);
    }

    class FileSystemProvider : IFileSystemProvider
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);

        public bool FileExists(string path) => File.Exists(path);

        public void DeleteDirectory(string path) => Directory.Delete(path, true);

        public void DeleteFile(string path) => File.Delete(path);

        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            Directory.EnumerateDirectories(path, searchPattern, searchOption);

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            Directory.EnumerateFiles(path, searchPattern, searchOption);

        public Version GetVersionInfo(string fileName) =>
            new Version(FileVersionInfo.GetVersionInfo(fileName).FileVersion);

        public string GetFileText(string path) => File.ReadAllText(path);
    }
}
