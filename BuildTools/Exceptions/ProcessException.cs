using System;
using System.Runtime.Serialization;

namespace BuildTools
{
    [Serializable]
    public class ProcessException : Exception
    {
        public ProcessException()
        {
        }

        public ProcessException(string message) : base(message)
        {
        }

        public ProcessException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
