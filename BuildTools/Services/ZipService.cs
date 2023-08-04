using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BuildTools
{
    interface IZipService
    {
        void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName);

        void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName);
    }

    class ZipService : IZipService
    {
        private IFileSystemProvider fileSystem;

        public ZipService(IFileSystemProvider fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        #region CreateFromDirectory

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

                using (var entryStream = zipArchiveEntry.Open())
                    stream.CopyTo(entryStream);

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

        #endregion
        #region ExtractToDirectory

        public void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
        {
            if (sourceArchiveFileName == null)
                throw new ArgumentNullException(nameof(sourceArchiveFileName));

            using (var archive = new ZipArchive(fileSystem.ReadFile(sourceArchiveFileName)))
            {
                ExtractToDirectory(archive, destinationDirectoryName);
            }
        }

        private void ExtractToDirectory(ZipArchive source, string destinationDirectoryName)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));

            foreach (var entry in source.Entries)
            {
                ExtractRelativeToDirectory(entry, destinationDirectoryName);
            }
        }

        private void ExtractRelativeToDirectory(ZipArchiveEntry source, string destinationDirectoryName)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));

            // Note that this will give us a good DirectoryInfo even if destinationDirectoryName exists:
            var di = Directory.CreateDirectory(destinationDirectoryName);

            var destinationDirectoryFullPath = di.FullName;

            if (!destinationDirectoryFullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                destinationDirectoryFullPath += Path.DirectorySeparatorChar;

            var fileDestinationPath = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, source.FullName));

            //Assume case sensitive operating system to protect against work case scenario
            if (!fileDestinationPath.StartsWith(destinationDirectoryFullPath, StringComparison.Ordinal))
                throw new IOException("Extracting Zip entry would have resulted in a file outside the specified destination directory.");

            if (Path.GetFileName(fileDestinationPath).Length == 0)
            {
                // If it is a directory:

                if (source.Length != 0)
                    throw new IOException("Zip entry name ends in directory separator character but contains data.");

                Directory.CreateDirectory(fileDestinationPath);
            }
            else
            {
                // If it is a file:
                // Create containing directory:
                fileSystem.CreateDirectory(Path.GetDirectoryName(fileDestinationPath));
                ExtractToFile(source, fileDestinationPath);
            }
        }

        private void ExtractToFile(ZipArchiveEntry source, string destinationFileName)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            using (Stream fs = fileSystem.WriteFile(destinationFileName, FileMode.CreateNew))
            {
                using (var es = source.Open())
                    es.CopyTo(fs);
            }

            fileSystem.SetFileLastWriteTime(destinationFileName, source.LastWriteTime.DateTime);
        }

        #endregion
    }
}
