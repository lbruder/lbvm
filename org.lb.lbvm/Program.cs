using System;
using System.Collections.Generic;
using System.IO;

namespace org.lb.lbvm
{
    public sealed class PrintEventArgs : EventArgs
    {
        public readonly string Value;
        public PrintEventArgs(string value)
        {
            Value = value;
        }
    }

    public sealed class Program : runtime.OutputPort
    {
        public readonly int Version;
        private readonly runtime.Statement[] Statements;
        internal readonly byte[] Bytecode;
        internal readonly string[] SymbolTable;

        public event EventHandler<PrintEventArgs> OnPrint = delegate { };

        public void Print(string value)
        {
            OnPrint(this, new PrintEventArgs(value));
        }

        internal Program(int version, byte[] bytecode, string[] symbolTable)
        {
            Version = version;
            Statements = BytecodeParser.Parse(bytecode, symbolTable, this);
            Bytecode = bytecode;
            SymbolTable = symbolTable;
        }

        public static Program FromSchemeSource(string source)
        {
            return FromAssemblerSource(scheme.Compiler.Compile(source));
        }

        public static Program FromAssemblerSource(IEnumerable<string> lines)
        {
            return Assembler.Assemble(lines);
        }

        public static Program FromStream(Stream data)
        {
            return new ProgramFileReader(data).ProgramFile;
        }

        public void WriteToStream(Stream data)
        {
            new ProgramFileWriter(this).Write(data);
        }

        public object Run()
        {
            var envStack = new runtime.EnvironmentStack();
            envStack.PushNew(); // Global env
            return RunWithEnvironmentStack(envStack);
        }

        internal object RunWithEnvironmentStack(runtime.EnvironmentStack envStack)
        {
            int ip = 0;
            var valueStack = new runtime.ValueStack();
            var callStack = new runtime.CallStack();

            if (envStack.Count() == 0) throw new exceptions.RuntimeException("Internal error: No global environment present!");
            if (envStack.Count() > 1) throw new exceptions.RuntimeException("Internal error: Invalid environment stack");

            for (var current = Statements[ip]; !(current is runtime.EndStatement); current = Statements[ip])
            {
                //Debug.Print("0x" + ip.ToString("x4") + ": " + current);
                current.Execute(ref ip, valueStack, envStack, callStack);
            }

            if (envStack.Count() == 0) throw new exceptions.RuntimeException("Bad program: Global environment deleted!");
            if (envStack.Count() > 1) throw new exceptions.RuntimeException("Bad program: Environment stack not cleaned up");
            if (callStack.Count() > 1) throw new exceptions.RuntimeException("Bad program: Call stack not cleaned up");
            if (valueStack.Count() == 0) throw new exceptions.RuntimeException("Bad program: Value stack empty after running");
            if (valueStack.Count() > 1) throw new exceptions.RuntimeException("Bad program: Value stack not cleaned up");
            return valueStack.Pop();
        }
    }
}
