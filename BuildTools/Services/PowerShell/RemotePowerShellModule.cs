using System;

namespace BuildTools.PowerShell
{
    class RemotePowerShellModule : IPowerShellModule
    {
        public string Name { get; }
        public string Path { get; }
        public Version Version { get; }

        public RemotePowerShellModule(string name, string path, Version version)
        {
            Name = name;
            Path = path;
            Version = version;
        }

        public override string ToString()
        {
            return $"{Name} {Version}";
        }
    }
}
