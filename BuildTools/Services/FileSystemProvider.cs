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

        void CreateDirectory(string path);

        void DeleteDirectory(string path);

        void DeleteFile(string path);

        Stream ReadFile(string path);

        Stream WriteFile(string path, FileMode mode);

        IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        IEnumerable<string> EnumerateDirectoryFileSystemEntries(string path, string searchPattern, SearchOption searchOption);

        void MoveFile(string sourceFileName, string destFileName);

        Version GetVersionInfo(string fileName);

        string GetFileText(string path);

        string[] GetFileLines(string path);

        void WriteFileText(string path, string contents);

        void WriteFileLines(string path, string[] contents);

        void CopyDirectory(string sourcePath, string destinationPath, bool recursive = false);
    }

    class FileSystemProvider : IFileSystemProvider
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);

        public bool FileExists(string path) => File.Exists(path);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public void DeleteDirectory(string path) => Directory.Delete(path, true);

        public void DeleteFile(string path) => File.Delete(path);

        public Stream ReadFile(string path) => File.OpenRead(path);

        public Stream WriteFile(string path, FileMode mode) => File.Open(path, mode, FileAccess.Write, FileShare.None);

        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            Directory.EnumerateDirectories(path, searchPattern, searchOption);

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            Directory.EnumerateFiles(path, searchPattern, searchOption);

        public IEnumerable<string> EnumerateDirectoryFileSystemEntries(string path, string searchPattern, SearchOption searchOption) =>
            Directory.EnumerateFileSystemEntries(path, searchPattern, searchOption);

        public void MoveFile(string sourceFileName, string destFileName)
        {
            if (Directory.Exists(destFileName))
            {
                var fileName = Path.GetFileName(sourceFileName);
                destFileName = Path.Combine(destFileName, fileName);
            }

            if (File.Exists(destFileName))
                File.Delete(destFileName);

            File.Move(sourceFileName, destFileName);
        }

        public Version GetVersionInfo(string fileName) =>
            new Version(FileVersionInfo.GetVersionInfo(fileName).FileVersion);

        public string GetFileText(string path) => File.ReadAllText(path);

        public string[] GetFileLines(string path) => File.ReadAllLines(path);

        public void WriteFileText(string path, string contents) => File.WriteAllText(path, contents);

        public void WriteFileLines(string path, string[] contents) => File.WriteAllLines(path, contents);

        public void CopyDirectory(string sourcePath, string destinationPath, bool recursive = false)
        {
            if (!recursive)
                throw new NotImplementedException("Copying a directory non-recursively is not implemented");

            Directory.CreateDirectory(destinationPath);

            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var relativePath = file.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var directory = Path.GetDirectoryName(relativePath);

                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(Path.Combine(destinationPath, directory));

                var destinationFile = Path.Combine(destinationPath, relativePath);

                File.Copy(file, destinationFile, true);
            }
        }
    }
}
