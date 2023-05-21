using System.Collections.Generic;
using System.IO;

namespace BuildTools
{
    interface IFileSystemProvider
    {
        bool DirectoryExists(string path);

        bool FileExists(string path);

        IEnumerable<string> EnumerateFiles(string path);
    }

    class FileSystemProvider : IFileSystemProvider
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);

        public bool FileExists(string path) => File.Exists(path);

        public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);
    }
}
