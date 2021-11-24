using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Task1
{
    [Serializable]
    public class DIException : Exception
    {
        public DIException()
        {
        }

        public DIException(string message)
            : base(message)
        {
        }

        public DIException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DIException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
