using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace BuildTools
{
    class PackageTestsHashtableConverter : IHashtableConverter
    {
        public static readonly PackageTestsHashtableConverter Instance = new PackageTestsHashtableConverter();

        public object Convert(Hashtable value)
        {
            var keys = value.Keys.Cast<string>().ToArray();

            var packageTests = new PackageTests();

            foreach (var key in keys)
            {
                var val = (object[])value[key];

                switch (key)
                {
                    case "C#":
                        packageTests.CSharp = ProcessTests(key, val);
                        break;

                    case "PowerShell":
                        packageTests.PowerShell = ProcessTests(key, val);
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle package test type '{key}'.");
                }
            }

            return packageTests;
        }

        private IPackageTest[] ProcessTests(string type, object[] tests)
        {
            var results = new List<IPackageTest>();

            foreach (var item in tests)
            {
                if (!(item is Hashtable ht))
                    throw new InvalidOperationException($"Expected package tests for type '{type}' to contain a collection of values of type '{nameof(Hashtable)}' however a value of type '{item.GetType().Name}' was encountered.");

                var keys = ht.Keys.Cast<string>().ToArray();

                string command = null;
                string result = null;
                string kind = null;

                foreach (var key in keys)
                {
                    var val = ht[key]?.ToString();

                    switch (key.ToLower())
                    {
                        case "command":
                            command = val;
                            break;

                        case "result":
                            result = val;
                            break;

                        case "kind":
                            kind = val;
                            break;

                        default:
                            throw new NotImplementedException($"Don't know how to handle property '{key}' in a '{type}' package test");
                    }
                }

                if (kind == null)
                {
                    if (command != null && result != null)
                        results.Add(new ScriptPackageTest(command, result));
                    else
                        throw new NotImplementedException(); //dont know what it is
                }
                else
                {
                    switch (kind.ToLower())
                    {
                        case "cmdlet":
                        case "function":
                            if (command == null || result == null)
                                throw new NotImplementedException($"Package test of type '{kind}' requires both {nameof(command)} and {nameof(result)} be specified");

                            results.Add(new PSCommandPackageTest(command, result, (CommandTypes) Enum.Parse(typeof(CommandTypes), kind, true)));
                            break;

                        case "cmdletexport":
                            if (command == null)
                                throw new NotImplementedException($"Package test of type '{kind}' requires {nameof(command)} be specified");

                            results.Add(new PSExportPackageTest(command, CommandTypes.Cmdlet));
                            break;

                        case "aliasexport":
                            if (command == null)
                                throw new NotImplementedException($"Package test of type '{kind}' requires {nameof(command)} be specified");

                            results.Add(new PSExportPackageTest(command, CommandTypes.Alias));
                            break;

                        default:
                            throw new NotImplementedException($"Don't know how to handle a '{type}' package test of type '{kind}'");
                    }
                }
            }

            return results.ToArray();
        }
    }
}
