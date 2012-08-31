using System;

namespace org.lb.lbvm
{
    public sealed class RuntimeException : Exception
    {
        public RuntimeException(string message)
            : base(message)
        {
        }
    }
}
