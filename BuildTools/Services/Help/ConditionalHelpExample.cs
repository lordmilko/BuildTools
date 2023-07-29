using System;

namespace BuildTools
{
    class ConditionalHelpExample : HelpExample
    {
        public Delegate Predicate { get; }

        public ConditionalHelpExample(Delegate predicate, string name, string description) : base(name, description)
        {
            Predicate = predicate;
        }
    }
}
