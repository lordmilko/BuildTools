using System;
using System.Collections;
using System.IO;
using BuildTools.PowerShell;

namespace BuildTools
{
    internal class UpdateBuildManifestService
    {
        private readonly IFileSystemProvider fileSystem;
        private readonly IPowerShellService powerShell;
        private readonly NewBuildEnvironmentService newBuildEnvironmentService;

        public UpdateBuildManifestService(
            IFileSystemProvider fileSystem,
            IPowerShellService powerShell,
            NewBuildEnvironmentService newBuildEnvironmentService)
        {
            this.fileSystem = fileSystem;
            this.powerShell = powerShell;
            this.newBuildEnvironmentService = newBuildEnvironmentService;
        }

        public void Execute(string path)
        {
            if (!fileSystem.FileExists(path))
                throw new FileNotFoundException($"Could not find build environment config file '{path}'", path);

            var psd1Contents = fileSystem.ReadFileText(path);

            var hashTable = (Hashtable) powerShell.InvokeAndUnwrap(psd1Contents);

            if (hashTable == null)
                throw new InvalidOperationException($"Config file '{path}' did not contain a {nameof(Hashtable)}");

            var valueProvider = new HashTableConfigSettingValueProvider(hashTable);

            var str = newBuildEnvironmentService.CreateConfigContents(valueProvider);

            fileSystem.WriteFileText(path, str);
        }
    }
}
