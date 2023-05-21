using System;
using System.Collections.Generic;
using System.IO;

namespace BuildTools.Tests
{
    class MockFileSystemProvider : IFileSystemProvider, IMock<IFileSystemProvider>
    {
        public Dictionary<string, bool> DirectoryMap { get; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> FileMap { get; } = new Dictionary<string, bool>();
        public Dictionary<string, string[]> DirectoryFiles { get; } = new Dictionary<string, string[]>();

        public bool DirectoryExists(string path)
        {
            if (DirectoryMap.TryGetValue(path, out var exists))
                return exists;

            throw new InvalidOperationException($"Existence of directory '{path}' has not been set");
        }

        public bool FileExists(string path)
        {
            if (FileMap.TryGetValue(path, out var exists))
                return exists;

            throw new InvalidOperationException($"Existence of file '{path}' has not been set");
        }

        public IEnumerable<string> EnumerateFiles(string path)
        {
            if (DirectoryFiles.TryGetValue(path, out var files))
                return files;

            throw new InvalidOperationException($"Files of directory '{path}' have not been set");
        }
    }
}