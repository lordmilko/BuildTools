﻿namespace BuildTools
{
    class TestResultConfig
    {
        public string Name { get; set; }

        public string[] Path { get; set; }

        public TestType[] Type { get; set; }

        public TestOutcome? Outcome { get; set; }

        public bool Integration { get; set; }

        public TestTarget Target => new TestTarget(Type);
    }
}
