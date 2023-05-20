using System;

namespace BuildTools
{
    abstract class Dependency
    {
        public string Name { get; }

        public Version Version { get; }

        public Version MinimumVersion { get; }

        public string DisplayName { get; }

        public DependencyType Type { get; }

        protected Dependency(
            string name,
            DependencyType type,
            string version = null,
            string minimumVersion = null,
            string displayName = null)
        {
            Name = name;
            Type = type;

            if (version != null)
                Version = new Version(version);

            if (minimumVersion != null)
                MinimumVersion = new Version(minimumVersion);

            DisplayName = displayName;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}