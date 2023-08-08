namespace BuildTools
{
    /// <summary>
    /// Specifies high level categories that commands fall into.
    /// </summary>
    public enum CommandCategory
    {
        /// <summary>
        /// The command manages the build.
        /// </summary>
        Build,

        /// <summary>
        /// The command is provides facilities typically intended to be run in CI.
        /// </summary>
        CI,

        /// <summary>
        /// The command provides assistance with using the build environment.
        /// </summary>
        Help,

        /// <summary>
        /// The command is used for managing tests.
        /// </summary>
        Test,

        /// <summary>
        /// The command is a miscellaneous utility.
        /// </summary>
        Utility,

        /// <summary>
        /// The command is used for managing version details.
        /// </summary>
        Version
    }
}
