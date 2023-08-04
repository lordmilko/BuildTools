using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace BuildTools
{
    class PackageFilesHashtableConverter : IHashtableConverter
    {
        public static readonly PackageFilesHashtableConverter Instance = new PackageFilesHashtableConverter();

        private static string[] packageContextProps = typeof(PackageFileContext).GetProperties().Select(v => v.Name).ToArray();

        public object Convert(Hashtable value)
        {
            var keys = value.Keys.Cast<string>().ToArray();

            var packageFiles = new PackageFiles();

            foreach (var key in keys)
            {
                var val = (object[])value[key];

                switch (key)
                {
                    case "C#":
                        packageFiles.CSharp = ProcessFiles(key, val);
                        break;

                    case "PowerShell":
                        packageFiles.PowerShell = ProcessFiles(key, val);
                        break;

                    case "Redist":
                        packageFiles.Redist = ProcessFiles(key, val);
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle package files type '{key}'.");
                }
            }

            return packageFiles;
        }

        private PackageFileItem[] ProcessFiles(string type, object[] files)
        {
            var results = new List<PackageFileItem>();

            foreach (var file in files)
            {
                if (file is string s)
                    results.Add(new PackageFileItem(s));
                else if (file is Hashtable ht)
                {
                    var keys = ht.Keys.Cast<string>().ToArray();

                    string name = null;
                    ScriptBlock condition = null;

                    foreach (var key in keys)
                    {
                        object val = ht[key];

                        switch (key)
                        {
                            case "Name":
                                name = (string) val;
                                break;

                            case "Condition":
                                condition = (ScriptBlock) val;
                                break;

                            default:
                                throw new NotImplementedException($"Don't know how to handle property '{key}' in a '{type}' package files");
                        }
                    }

                    if (name == null)
                        throw new InvalidOperationException($"Property '{nameof(name)}' was missing in a {type} {nameof(Hashtable)}");

                    if (condition == null)
                        throw new InvalidOperationException($"Property '{nameof(condition)}' was missing in a {type} {nameof(Hashtable)}");

                    var props = condition.Ast.FindAll(v => v is MemberExpressionAst, true)
                        .Cast<MemberExpressionAst>()
                        .Where(v => v.Expression.ToString() == "$_")
                        .Select(v => v.Member.ToString())
                        .ToArray();

                    var unknown = props.Except(packageContextProps, StringComparer.OrdinalIgnoreCase).ToArray();

                    if (unknown.Length > 0)
                        throw new InvalidOperationException($"Illegal {(unknown.Length == 1 ? "property was" : "properties were")} referenced in {nameof(ScriptBlock)} {type}{name}: {string.Join(", ", unknown)}");

                    Func<PackageFileContext, bool> func = arg =>
                    {
                        var result = condition.InvokeWithContext(null,
                            new List<PSVariable>
                            {
                                new PSVariable("_", arg)
                            }
                        );

                        var val = result?.FirstOrDefault()?.BaseObject;

                        if (val == null)
                            throw new InvalidOperationException($"Expected {nameof(ScriptBlock)} '{condition}' to return a value.");

                        if (val is bool)
                            return (bool) val;

                        throw new InvalidOperationException($"Expected a return value of type {typeof(bool).Name} however got return value '{val}' of type {val.GetType().Name}");
                    };

                    results.Add(new PackageFileItem(name, func));
                }
                else
                    throw new NotImplementedException($"Don't know how to handle a value of type {file.GetType().Name} in {type} package files");
            }

            return results.ToArray();
        }
    }
}
