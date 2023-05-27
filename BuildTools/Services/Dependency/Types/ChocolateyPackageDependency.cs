namespace BuildTools
{
    class ChocolateyPackageDependency : Dependency
    {
        private readonly string commandName;

        public string CommandName => commandName ?? Name;

        public ChocolateyPackageDependency(string name, string version = null, string minimumVersion = null, string displayName = null, string commandName = null) : base(name, DependencyType.Chocolatey, version, minimumVersion, displayName)
        {
            this.commandName = commandName;
        }
    }
}