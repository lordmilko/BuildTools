﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BuildTools
{
    /// <summary>
    /// Represents a collection of arguments to be specified to a process.
    /// </summary>
    struct ArgList : IEnumerable
    {
        private List<string> arguments;

        public string[] Arguments => arguments.ToArray();

        public void Add(object argument)
        {
            if (arguments == null)
                arguments = new List<string>();

            arguments.Add(argument.ToString());
        }

        public static implicit operator ArgList(string value)
        {
            //Don't need to worry about splitting the value up. It's just as valid post-merged rather than being split and re-merged
            return new ArgList { value };
        }

        public static implicit operator ArgList(List<string> values)
        {
            var result = new ArgList();

            foreach (var item in values)
                result.Add(item);

            return result;
        }

        public static implicit operator string(ArgList args)
        {
            return args.ToString();
        }

        public override string ToString()
        {
            if (arguments == null)
                return string.Empty;

            return string.Join(" ", arguments);
        }

        public IEnumerator GetEnumerator()
        {
            if (arguments == null)
                return Enumerable.Empty<string>().GetEnumerator();

            return arguments.GetEnumerator();
        }
    }
}