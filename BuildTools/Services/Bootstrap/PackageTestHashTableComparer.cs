using System;
using System.Collections.Generic;

namespace BuildTools
{
    class PackageTestHashTableComparer : IComparer<object>
    {
        public static readonly PackageTestHashTableComparer Instance = new PackageTestHashTableComparer();

        public int Compare(object x, object y)
        {
            var s1 = x.ToString();
            var s2 = y.ToString();

            int GetPos(string str)
            {
                switch (str.ToLower())
                {
                    case "command":
                        return 1;

                    case "result":
                        return 2;

                    case "kind":
                        return 3;

                    default:
                        throw new InvalidOperationException($"Don't know how to handle package test property '{str}'.");
                }
            }

            var p1 = GetPos(s1);
            var p2 = GetPos(s2);

            return p1.CompareTo(p2);
        }
    }
}
