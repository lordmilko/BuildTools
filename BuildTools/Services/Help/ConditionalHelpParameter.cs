using System;

namespace BuildTools
{
    class ConditionalHelpParameter : HelpParameter
    {
        public Delegate Predicate { get; }

        public ConditionalHelpParameter(Delegate predicate, string name, string description) : base(name, description)
        {
            Predicate = predicate;
        }
    }
}