namespace BuildTools
{
    class BuildProject
    {
        /// <summary>
        /// Gets the name of the project file.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the full path to the project file.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the name of the project file (excluding directory).
        /// </summary>
        public string FileName { get; }
    }
}