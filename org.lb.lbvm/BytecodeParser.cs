using System;
using System.Collections.Generic;

namespace org.lb.lbvm
{
    public sealed class InvalidOpcodeException : Exception
    {
        public InvalidOpcodeException(string message)
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

    internal sealed class BytecodeParser
    {
        private readonly string[] symbolTable;
        private readonly byte[] bytecode;
        private int offset;
        private readonly List<Statement> statements = new List<Statement>();

        private BytecodeParser(byte[] bytecode, string[] symbolTable)
        {
            this.bytecode = bytecode;
            this.symbolTable = symbolTable;
            this.offset = 0;
        }

        internal static Statement[] Parse(byte[] bytecode, string[] symbolTable)
        {
            return new BytecodeParser(bytecode, symbolTable).ParseStatements();
        }

        private Statement[] ParseStatements()
        {
            offset = 0;
            statements.Clear();
            var errorStatement = new ErrorStatement();
            while (offset < bytecode.Length)
            {
                ParseStatement();
                while (statements.Count < offset) statements.Add(errorStatement);
            }
            return statements.ToArray();
        }

        private void ParseStatement()
        {
            int tmp;
            byte opcode = bytecode[offset++];
            switch (opcode)
            {
                case 0x00: statements.Add(new EndStatement()); return;
                case 0x01: statements.Add(new PopStatement()); return;
                case 0x02: statements.Add(new PushintStatement(ReadInt())); return;
                case 0x03: tmp = ReadInt(); statements.Add(new DefineStatement(tmp, GetSymbolTableEntry(tmp))); return;
                case 0x04: tmp = ReadInt(); statements.Add(new PushvarStatement(tmp, GetSymbolTableEntry(tmp))); return;
                case 0x05: statements.Add(new NumeqStatement()); return;
                case 0x06: statements.Add(new AddStatement()); return;
                case 0x07: statements.Add(new SubStatement()); return;
                case 0x08: statements.Add(new MulStatement()); return;
                case 0x09: statements.Add(new DivStatement()); return;
                case 0x0a: statements.Add(new IdivStatement()); return;
                case 0x0b: statements.Add(new BfalseStatement(ReadInt())); return;
                case 0x0c: statements.Add(new EnterStatement(ReadInt(), GetSymbolTableEntry(ReadInt()))); return;
                case 0x0d: statements.Add(new RetStatement()); return;
                case 0x0e: statements.Add(new CallStatement(ReadInt())); return;
                case 0x0f: statements.Add(new TailcallStatement(ReadInt())); return;
                case 0x10: statements.Add(new JmpStatement(ReadInt())); return;
                case 0x11: statements.Add(new PushlabelStatement(ReadInt())); return;
                case 0x12: statements.Add(new ImodStatement()); return;
                case 0x13: tmp = ReadInt(); statements.Add(new SetStatement(tmp, GetSymbolTableEntry(tmp))); return;
                case 0x14: tmp = ReadInt(); statements.Add(new PushsymStatement(tmp, GetSymbolTableEntry(tmp))); return;
                case 0x15: statements.Add(new PushboolStatement(true)); return;
                case 0x16: statements.Add(new PushboolStatement(false)); return;
                case 0x17: statements.Add(new MakeClosureStatement(ReadInt())); return;
                case 0x18: statements.Add(new NumltStatement()); return;
                case 0x19: statements.Add(new NumleStatement()); return;
                case 0x1a: statements.Add(new NumgtStatement()); return;
                case 0x1b: statements.Add(new NumgeStatement()); return;
                case 0x1c: statements.Add(new PushdblStatement(ReadDouble())); return;
                default: throw new InvalidOpcodeException("Invalid opcode: 0x" + opcode.ToString("x2"));
            }
        }

        private int ReadInt()
        {
            int ret = BitConverter.ToInt32(bytecode, offset);
            offset += 4;
            return ret;
        }

        private string GetSymbolTableEntry(int no)
        {
            if (no >= 0 && no < symbolTable.Length) return symbolTable[no];
            throw new SymbolTableEntryNotFoundException("Symbol table entry not found");
        }

        private double ReadDouble()
        {
            double ret = BitConverter.ToDouble(bytecode, offset);
            offset += 8;
            return ret;
        }
    }
}
