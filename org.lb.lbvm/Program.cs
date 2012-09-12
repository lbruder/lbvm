using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using org.lb.lbvm.runtime;

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

    public sealed class Program : OutputPort
    {
        public readonly int Version;
        private readonly Statement[] Statements;
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

        public object Run(params object[] parameters)
        {
            var envStack = new EnvironmentStack();
            envStack.PushNew(); // Global env
            envStack.Set(Symbol.fromString("sys:args"), new Variable(CliToVm(parameters)));

            int ip = 0;
            var valueStack = new ValueStack();
            var callStack = new CallStack();

            for (var current = Statements[ip]; !(current is EndStatement); current = Statements[ip])
            {
                //System.Diagnostics.Debug.Print("0x" + ip.ToString("x4") + ": " + current);
                current.Execute(ref ip, valueStack, envStack, callStack);
            }

            if (envStack.Count() == 0) throw new exceptions.RuntimeException("Bad program: Global environment deleted!");
            if (envStack.Count() > 1) throw new exceptions.RuntimeException("Bad program: Environment stack not cleaned up");
            if (callStack.Count() > 1) throw new exceptions.RuntimeException("Bad program: Call stack not cleaned up");
            if (valueStack.Count() == 0) throw new exceptions.RuntimeException("Bad program: Value stack empty after running");
            if (valueStack.Count() > 1) throw new exceptions.RuntimeException("Bad program: Value stack not cleaned up");
            return valueStack.Pop();
        }

        private static object CliToVm(object o)
        {
            if (o == null) return Nil.GetInstance();
            if (o is bool) return o;
            if (o is int) return o;
            if (o is double) return o;
            if (o is string) return new StringObject((string)o);
            if (o is char) return o;
            if (o is IEnumerable) return ((IEnumerable)o).Cast<object>().Reverse<object>().Aggregate((object)Nil.GetInstance(), (acc, i) => new Pair(CliToVm(i), acc));
            throw new exceptions.RuntimeException("Parameter of type " + o.GetType() + " could not be converted to VM type");
        }
    }
}
