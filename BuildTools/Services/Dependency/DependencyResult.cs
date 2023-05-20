using System;
using System.Diagnostics;

namespace BuildTools
{
    [DebuggerDisplay("Name = {Name,nq}, Type = {Type,nq}, Action = {Action,nq}, Version = {Version}")]
    public class DependencyResult
    {
        public string Name { get; }

        public DependencyType Type { get; }

        public Version Version { get; }

        public DependencyAction Action { get; }

        internal DependencyResult(Dependency dependency, Version version, DependencyAction action)
        {
            Name = dependency.Name;
            Type = dependency.Type;
            Version = version;
            Action = action;
        }
    }
}