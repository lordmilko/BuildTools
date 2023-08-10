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

        public bool Condition { get; }

        protected Dependency(
            string name,
            DependencyType type,
            string version = null,
            string minimumVersion = null,
            string displayName = null,
            bool condition = true)
        {
            Name = name;
            Type = type;

            if (version != null)
                Version = new Version(version);

            if (minimumVersion != null)
                MinimumVersion = new Version(minimumVersion);

            DisplayName = displayName;
            Condition = condition;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
