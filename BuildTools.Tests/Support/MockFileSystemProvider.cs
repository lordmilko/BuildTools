using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    class MockFileSystemProvider : IFileSystemProvider, IMock<IFileSystemProvider>
    {
        public Dictionary<string, bool> DirectoryExistsMap { get; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> FileExistsMap { get; } = new Dictionary<string, bool>();
        public Dictionary<string, string> ReadFileTextMap { get; } = new Dictionary<string, string>();
        public Dictionary<string, string[]> ReadFileLinesMap { get; } = new Dictionary<string, string[]>();
        public Dictionary<(string path, string searchPattern, SearchOption searchOption), string[]> EnumerateFilesMap { get; } = new Dictionary<(string path, string searchPattern, SearchOption searchOption), string[]>();
        public Dictionary<(string path, string searchPattern, SearchOption searchOption), string[]> EnumerateDirectoryFileSystemEntriesMap { get; } = new Dictionary<(string path, string searchPattern, SearchOption searchOption), string[]>();
        public Dictionary<(string path, string searchPattern, SearchOption searchOption), string[]> EnumerateDirectoriesMap { get; } = new Dictionary<(string path, string searchPattern, SearchOption searchOption), string[]>();
        public List<string> DeletedFiles { get; } = new List<string>();
        public List<string> DeletedDirectories { get; } = new List<string>();
        public List<string> CreatedDirectories { get; } = new List<string>();
        public List<(string source, string destination)> MovedFiles { get; } = new List<(string source, string destination)>();
        public Dictionary<string, Version> VersionInfoMap { get; } = new Dictionary<string, Version>();
        public Dictionary<string, Stream> WriteFileMap { get; } = new Dictionary<string, Stream>();
        public Dictionary<string, Action<string, string>> OnWriteFileText { get; } = new Dictionary<string, Action<string, string>>();
        public Dictionary<string, Action<string, string[]>> OnWriteFileLines { get; } = new Dictionary<string, Action<string, string[]>>();

        public bool DirectoryExists(string path)
        {
            if (DirectoryExistsMap.TryGetValue(path, out var exists))
                return exists;

            throw new InvalidOperationException($"Existence of directory '{path}' has not been set");
        }

        public bool FileExists(string path)
        {
            if (FileExistsMap.TryGetValue(path, out var exists))
                return exists;

            throw new InvalidOperationException($"Existence of file '{path}' has not been set");
        }

        public void CreateDirectory(string path)
        {
            CreatedDirectories.Add(path);
        }

        public void DeleteDirectory(string path)
        {
            DeletedDirectories.Add(path);
        }

        public void DeleteFile(string path)
        {
            DeletedFiles.Add(path);
        }

        public Stream ReadFile(string path)
        {
            var text = ReadFileText(path);

            return new MemoryStream(Encoding.ASCII.GetBytes(text));
        }

        public Stream WriteFile(string path, FileMode mode)
        {
            var stream = new MemoryStream();

            WriteFileMap[path] = stream;

            return stream;
        }

        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (EnumerateDirectoriesMap.TryGetValue((path, searchPattern, searchOption), out var directories))
                return directories;

            throw new InvalidOperationException($"Directories of directory '{path}, {searchPattern} , {searchOption}' have not been set");
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (EnumerateFilesMap.TryGetValue((path, searchPattern, searchOption), out var files))
                return files;

            throw new InvalidOperationException($"Files of directory '{path}, {searchPattern} , {searchOption}' have not been set");
        }

        public IEnumerable<string> EnumerateDirectoryFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        {
            if (EnumerateDirectoryFileSystemEntriesMap.TryGetValue((path, searchPattern, searchOption), out var files))
                return files;

            throw new InvalidOperationException($"Filesystem entries of directory '{path}, {searchPattern} , {searchOption}' have not been set");
        }

        public void MoveFile(string sourceFileName, string destFileName)
        {
            MovedFiles.Add((sourceFileName, destFileName));
        }

        public void MoveDirectory(string sourceDirName, string destDirName)
        {
            throw new NotImplementedException();
        }

        public Version GetVersionInfo(string fileName)
        {
            if (VersionInfoMap.TryGetValue(fileName, out var version))
                return version;

            return new Version(1, 0);
        }

        public string ReadFileText(string path)
        {
            if (ReadFileTextMap.TryGetValue(path, out var text))
                return text;

            throw new InvalidOperationException($"Content of file '{path}' have not been set");
        }

        public string[] ReadFileLines(string path)
        {
            if (ReadFileLinesMap.TryGetValue(path, out var lines))
                return lines;

            throw new InvalidOperationException($"Lines of file '{path}' have not been set");
        }

        public void WriteFileText(string path, string contents)
        {
            if (OnWriteFileText.TryGetValue(path, out var action))
                action(path, contents);
            else
                throw new InvalidOperationException($"{nameof(OnWriteFileText)} for '{path}' is not set");
        }

        public void WriteFileLines(string path, string[] contents)
        {
            if (OnWriteFileLines.TryGetValue(path, out var action))
                action(path, contents);
            else
                throw new InvalidOperationException($"{nameof(OnWriteFileLines)} for '{path}' is not set");
        }

        public void CopyFile(string sourceFileName, string destFileName)
        {
        }

        public void CopyDirectory(string sourcePath, string destinationPath, bool recursive = false)
        {
        }

        public void WithCurrentDirectory(string path, Action action) => action();

        public void SetFileLastWriteTime(string path, DateTime lastWriteTime)
        {
            throw new NotImplementedException();
        }

        public void AssertDeletedFiles(params string[] expected)
        {
            Assert.AreEqual(expected.Length, DeletedFiles.Count, "Number of deleted files was not correct");

            for (var i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], DeletedFiles[i], $"Deleted file {i + 1} was not correct");
        }

        public void AssertDeletedDirectories(params string[] expected)
        {
            Assert.AreEqual(expected.Length, DeletedDirectories.Count, "Number of deleted directories was not correct");

            for (var i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], DeletedDirectories[i], $"Deleted directory {i + 1} was not correct");
        }

        public void AssertMovedFile(string source, string destination)
        {
            Assert.IsTrue(MovedFiles.Contains((source, destination)), $"Did not have {source} -> {destination}");
        }
    }
}
