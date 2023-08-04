using System;

namespace BuildTools
{
    public class PackageFileItem
    {
        public string Name { get; }

        public Func<PackageFileContext, bool> Condition { get; }

        public PackageFileItem(string name, Func<PackageFileContext, bool> condition = null)
        {
            Name = name;
            Condition = condition;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
