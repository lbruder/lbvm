using System;
using System.Collections.Generic;
using System.Linq;

namespace org.lb.lbvm
{
    internal sealed class BytecodeParser
    {
        private readonly runtime.Symbol[] symbolTable;
        private readonly byte[] bytecode;
        private readonly InputOutputChannel printer;
        private int offset;
        private readonly List<runtime.Statement> statements = new List<runtime.Statement>();

        private BytecodeParser(byte[] bytecode, IEnumerable<string> symbolTable, InputOutputChannel printer)
        {
            this.bytecode = bytecode;
            this.symbolTable = symbolTable.Select(runtime.Symbol.fromString).ToArray();
            this.printer = printer;
            this.offset = 0;
        }

        internal static runtime.Statement[] Parse(byte[] bytecode, IEnumerable<string> symbolTable, InputOutputChannel printer)
        {
            return new BytecodeParser(bytecode, symbolTable, printer).ParseStatements();
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
                case 0x03: tmp = ReadInt(); statements.Add(new runtime.DefineStatement(GetSymbolTableEntry(tmp))); return;
                case 0x04: tmp = ReadInt(); statements.Add(new runtime.PushvarStatement(GetSymbolTableEntry(tmp))); return;
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
                case 0x13: tmp = ReadInt(); statements.Add(new runtime.SetStatement(GetSymbolTableEntry(tmp))); return;
                case 0x14: tmp = ReadInt(); statements.Add(new runtime.PushsymStatement(GetSymbolTableEntry(tmp))); return;
                case 0x15: statements.Add(new runtime.PushboolStatement(true)); return;
                case 0x16: statements.Add(new runtime.PushboolStatement(false)); return;
                case 0x17: statements.Add(new runtime.MakeClosureStatement(ReadInt())); return;
                case 0x18: statements.Add(new runtime.NumltStatement()); return;
                case 0x19: statements.Add(new runtime.NumleStatement()); return;
                case 0x1a: statements.Add(new runtime.NumgtStatement()); return;
                case 0x1b: statements.Add(new runtime.NumgeStatement()); return;
                case 0x1c: statements.Add(new runtime.PushdblStatement(ReadDouble())); return;
                case 0x1d: tmp = ReadInt(); statements.Add(new runtime.MakevarStatement(GetSymbolTableEntry(tmp))); return;
                case 0x1e: statements.Add(new runtime.MakepairStatement()); return;
                case 0x1f: statements.Add(new runtime.IspairStatement()); return;
                case 0x20: statements.Add(new runtime.Pair1Statement()); return;
                case 0x21: statements.Add(new runtime.Pair2Statement()); return;
                case 0x22: statements.Add(new runtime.PushnilStatement()); return;
                case 0x23: statements.Add(new runtime.EnterRestStatement(ReadInt(), ReadInt(), GetSymbolTableEntry(ReadInt()))); return;
                case 0x24: statements.Add(new runtime.RandomStatement()); return;
                case 0x25: statements.Add(new runtime.ObjequalStatement()); return;
                case 0x26: statements.Add(new runtime.IsnullStatement()); return;
                case 0x27: statements.Add(new runtime.PrintStatement(printer)); return;
                case 0x28: statements.Add(new runtime.PushstrStatement(ReadString())); return;
                case 0x29: statements.Add(new runtime.IsnumberStatement()); return;
                case 0x2a: statements.Add(new runtime.IsstringStatement()); return;
                case 0x2b: statements.Add(new runtime.StreqStatement(false)); return;
                case 0x2c: statements.Add(new runtime.StreqStatement(true)); return;
                case 0x2d: statements.Add(new runtime.StrltStatement(false)); return;
                case 0x2e: statements.Add(new runtime.StrltStatement(true)); return;
                case 0x2f: statements.Add(new runtime.StrgtStatement(false)); return;
                case 0x30: statements.Add(new runtime.StrgtStatement(true)); return;
                case 0x31: statements.Add(new runtime.StrlengthStatement()); return;
                case 0x32: statements.Add(new runtime.SubstrStatement()); return;
                case 0x33: statements.Add(new runtime.StrappendStatement()); return;
                default: throw new InvalidOpcodeException("Invalid opcode: 0x" + opcode.ToString("x2"));
            }
        }

        private int ReadInt()
        {
            int ret = BitConverter.ToInt32(bytecode, offset);
            offset += 4;
            return ret;
        }

        private runtime.Symbol GetSymbolTableEntry(int no)
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

        private string ReadString()
        {
            int length = ReadInt();
            char[] value = new char[length];
            Array.Copy(bytecode, offset, value, 0, length);
            offset += length;
            return new string(value);
        }
    }
}
