using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace org.lb.lbvm
{
    public sealed class Program
    {
        public readonly int Version;
        public readonly Statement[] Statements;
        public readonly byte[] Bytecode;
        public readonly string[] SymbolTable;

        internal Program(int version, byte[] bytecode, string[] symbolTable)
        {
            Version = version;
            Statements = BytecodeParser.Parse(bytecode, symbolTable);
            Bytecode = bytecode;
            SymbolTable = symbolTable;
            // TODO: Sprungziele ueberpruefen
        }

        public static Program FromStream(Stream data)
        {
            return new ProgramFileReader(data).ProgramFile;
        }

        public void ToStream(Stream data)
        {
            new ProgramFileWriter(this).Write(data);
        }

        public object Run()
        {
            int ip = 0;
            var valueStack = new Stack<object>();
            var envStack = new Stack<Environment>();
            var callStack = new Stack<Call>();

            envStack.Push(new Environment()); // Global env

            for (var current = Statements[ip]; !(current is EndStatement); current = Statements[ip])
            {
                Debug.Print("0x" + ip.ToString("x4") + ": " + current);
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
