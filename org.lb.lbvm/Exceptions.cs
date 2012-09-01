using System;

namespace org.lb.lbvm
{
    public sealed class AssemblerException : Exception
    {
        public AssemblerException(string message)
            : base(message)
        {
        }
    }

    public sealed class CompilerException : Exception
    {
        public CompilerException(string message)
            : base(message)
        {
        }
    }

    public sealed class InvalidOpcodeException : Exception
    {
        public InvalidOpcodeException(string message)
            : base(message)
        {
        }
    }

    public sealed class ReaderException : Exception
    {
        public ReaderException(string message)
            : base(message)
        {
        }
    }

    public sealed class RuntimeException : Exception
    {
        public RuntimeException(string message)
            : base(message)
        {
        }
    }

    public sealed class SymbolTableEntryNotFoundException : Exception
    {
        public SymbolTableEntryNotFoundException(string message)
            : base(message)
        {
        }
    }
}
