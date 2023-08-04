using System;
using System.Linq;
using System.Management.Automation;

namespace BuildTools
{
    public class PSExportPackageTest : IPackageTest
    {
        public string Command { get; }

        public CommandTypes Type { get; }

        public PSExportPackageTest(string command, CommandTypes type)
        {
            Command = command;
            Type = type;
        }

        public void Test(object exports)
        {
            if (exports is object[] o && o.Contains(Command))
                return;

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            var word = Type switch
            {
                CommandTypes.Cmdlet => "cmdlets",
                CommandTypes.Alias => "aliases"
            };
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

            var actual = exports is object[] arr ? string.Join(", ", arr) : exports?.ToString() ?? "null";

            throw new InvalidOperationException($"Module manifest was not updated to specify exported {word}. Expected {word} to contain '{Command}' however value was '{actual}'");
        }
    }
}
