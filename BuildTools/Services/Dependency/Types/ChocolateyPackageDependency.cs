namespace BuildTools
{
    class ChocolateyPackageDependency : Dependency
    {
        private readonly string commandName;

        public string CommandName => commandName ?? Name;

        public ChocolateyPackageDependency(string name, string version = null, string minimumVersion = null, string displayName = null, string commandName = null, bool condition = true) : base(name, DependencyType.Chocolatey, version, minimumVersion, displayName, condition)
        {
            this.commandName = commandName;
        }
    }
}
