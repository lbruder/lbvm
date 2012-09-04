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

    public interface InputOutputChannel
    {
        void Print(string value);
    }

    public sealed class Program: InputOutputChannel
    {
        public readonly int Version;
        public readonly runtime.Statement[] Statements;
        public readonly byte[] Bytecode;
        public readonly string[] SymbolTable;

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
            int ip = 0;
            var valueStack = new Stack<object>();
            var envStack = new Stack<runtime.Environment>();
            var callStack = new Stack<runtime.Call>();

            envStack.Push(new runtime.Environment()); // Global env

            for (var current = Statements[ip]; !(current is runtime.EndStatement); current = Statements[ip])
            {
                //Debug.Print("0x" + ip.ToString("x4") + ": " + current);
                current.Execute(ref ip, valueStack, envStack, callStack);
            }

            if (envStack.Count == 0) throw new Exception("Bad program: Global environment deleted!");
            if (envStack.Count > 1) throw new Exception("Bad program: Environment stack not cleaned up");
            if (callStack.Count > 1) throw new Exception("Bad program: Call stack not cleaned up");
            if (valueStack.Count != 1) throw new Exception("Bad program: Value stack not cleaned up");

            return valueStack.Pop();
        }
    }
}
