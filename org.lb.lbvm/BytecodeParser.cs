using System;
using System.Collections.Generic;

namespace org.lb.lbvm
{
    internal sealed class BytecodeParser
    {
        private readonly string[] symbolTable;
        private readonly byte[] bytecode;
        private int offset;
        private readonly List<runtime.Statement> statements = new List<runtime.Statement>();

        private BytecodeParser(byte[] bytecode, string[] symbolTable)
        {
            this.bytecode = bytecode;
            this.symbolTable = symbolTable;
            this.offset = 0;
        }

        internal static runtime.Statement[] Parse(byte[] bytecode, string[] symbolTable)
        {
            return new BytecodeParser(bytecode, symbolTable).ParseStatements();
        }

        private runtime.Statement[] ParseStatements()
        {
            offset = 0;
            statements.Clear();
            var errorStatement = new runtime.ErrorStatement();
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
                case 0x00: statements.Add(new runtime.EndStatement()); return;
                case 0x01: statements.Add(new runtime.PopStatement()); return;
                case 0x02: statements.Add(new runtime.PushintStatement(ReadInt())); return;
                case 0x03: tmp = ReadInt(); statements.Add(new runtime.DefineStatement(tmp, GetSymbolTableEntry(tmp))); return;
                case 0x04: tmp = ReadInt(); statements.Add(new runtime.PushvarStatement(tmp, GetSymbolTableEntry(tmp))); return;
                case 0x05: statements.Add(new runtime.NumeqStatement()); return;
                case 0x06: statements.Add(new runtime.AddStatement()); return;
                case 0x07: statements.Add(new runtime.SubStatement()); return;
                case 0x08: statements.Add(new runtime.MulStatement()); return;
                case 0x09: statements.Add(new runtime.DivStatement()); return;
                case 0x0a: statements.Add(new runtime.IdivStatement()); return;
                case 0x0b: statements.Add(new runtime.BfalseStatement(ReadInt())); return;
                case 0x0c: statements.Add(new runtime.EnterStatement(ReadInt(), GetSymbolTableEntry(ReadInt()))); return;
                case 0x0d: statements.Add(new runtime.RetStatement()); return;
                case 0x0e: statements.Add(new runtime.CallStatement(ReadInt())); return;
                case 0x0f: statements.Add(new runtime.TailcallStatement(ReadInt())); return;
                case 0x10: statements.Add(new runtime.JmpStatement(ReadInt())); return;
                case 0x11: statements.Add(new runtime.PushlabelStatement(ReadInt())); return;
                case 0x12: statements.Add(new runtime.ImodStatement()); return;
                case 0x13: tmp = ReadInt(); statements.Add(new runtime.SetStatement(tmp, GetSymbolTableEntry(tmp))); return;
                case 0x14: tmp = ReadInt(); statements.Add(new runtime.PushsymStatement(tmp, GetSymbolTableEntry(tmp))); return;
                case 0x15: statements.Add(new runtime.PushboolStatement(true)); return;
                case 0x16: statements.Add(new runtime.PushboolStatement(false)); return;
                case 0x17: statements.Add(new runtime.MakeClosureStatement(ReadInt())); return;
                case 0x18: statements.Add(new runtime.NumltStatement()); return;
                case 0x19: statements.Add(new runtime.NumleStatement()); return;
                case 0x1a: statements.Add(new runtime.NumgtStatement()); return;
                case 0x1b: statements.Add(new runtime.NumgeStatement()); return;
                case 0x1c: statements.Add(new runtime.PushdblStatement(ReadDouble())); return;
                case 0x1d: tmp = ReadInt(); statements.Add(new runtime.MakevarStatement(tmp, GetSymbolTableEntry(tmp))); return;
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
