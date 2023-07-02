using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BuildTools
{
    interface IZipService
    {
        void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName);
    }

    class ZipService : IZipService
    {
        private IFileSystemProvider fileSystem;

        public ZipService(IFileSystemProvider fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
            destinationArchiveFileName = Path.GetFullPath(destinationArchiveFileName);

            using (var fileStream = fileSystem.WriteFile(destinationArchiveFileName, FileMode.CreateNew))
            using (var destination = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                foreach (var enumerateFileSystemInfo in fileSystem.EnumerateDirectoryFileSystemEntries(sourceDirectoryName, "*", SearchOption.AllDirectories))
                {
                    var length = enumerateFileSystemInfo.Length - sourceDirectoryName.Length;
                    var entryName = EntryFromPath(enumerateFileSystemInfo, sourceDirectoryName.Length, length);

                    if (fileSystem.FileExists(enumerateFileSystemInfo))
                    {
                        CreateEntryFromFile(destination, enumerateFileSystemInfo, entryName);
                    }
                    else
                    {
                        if (fileSystem.DirectoryExists(enumerateFileSystemInfo))
                        {
                            if (IsDirEmpty(enumerateFileSystemInfo))
                                destination.CreateEntry(entryName + '/');
                        }
                    }
                }
            }
        }

        private ZipArchiveEntry CreateEntryFromFile(
            ZipArchive destination,
            string sourceFileName,
            string entryName)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));

            if (entryName == null)
                throw new ArgumentNullException(nameof(entryName));

            using (var stream = fileSystem.ReadFile(sourceFileName))
            {
                var zipArchiveEntry = destination.CreateEntry(entryName);

                var dateTime = File.GetLastWriteTime(sourceFileName);

                if (dateTime.Year < 1980 || dateTime.Year > 2107)
                    dateTime = new DateTime(1980, 1, 1, 0, 0, 0);

                zipArchiveEntry.LastWriteTime = dateTime;

                using (var destination1 = zipArchiveEntry.Open())
                    stream.CopyTo(destination1);

                return zipArchiveEntry;
            }
        }

        private static string EntryFromPath(string entry, int offset, int length)
        {
            for (; length > 0 && (entry[offset] == Path.DirectorySeparatorChar || entry[offset] == Path.AltDirectorySeparatorChar); --length)
                ++offset;

            if (length == 0)
                return string.Empty;

            var charArray = entry.ToCharArray(offset, length);

            for (var index = 0; index < charArray.Length; ++index)
            {
                if (charArray[index] == Path.DirectorySeparatorChar || charArray[index] == Path.AltDirectorySeparatorChar)
                    charArray[index] = '/';
            }

            return new string(charArray);
        }

        private bool IsDirEmpty(string possiblyEmptyDir) =>
            !fileSystem.EnumerateDirectoryFileSystemEntries(possiblyEmptyDir, "*", SearchOption.AllDirectories).Any();
    }
}