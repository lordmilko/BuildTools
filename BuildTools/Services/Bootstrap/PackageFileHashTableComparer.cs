using System;
using System.Collections.Generic;

namespace BuildTools
{
    class PackageFileHashTableComparer : IComparer<object>
    {
        public static readonly PackageFileHashTableComparer Instance = new PackageFileHashTableComparer();

        public int Compare(object x, object y)
        {
            var s1 = x.ToString();
            var s2 = y.ToString();

            int GetPos(string str)
            {
                switch (str.ToLower())
                {
                    case "name":
                        return 1;

                    case "condition":
                        return 2;

                    default:
                        throw new InvalidOperationException($"Don't know how to handle package file property '{str}'.");
                }
            }

            var p1 = GetPos(s1);
            var p2 = GetPos(s2);

            return p1.CompareTo(p2);
        }
    }
}
