using System;
using System.Collections.Generic;

namespace org.lb.lbvm
{
    public sealed class AssemblerException : Exception
    {
        public AssemblerException(string message)
            : base(message)
        {
        }
    }

    public sealed class Assembler
    {
        private sealed class WantedLabel
        {
            public readonly string Label;
            public readonly int BytecodePosition;

            public WantedLabel(string label, int bytecodePosition)
            {
                Label = label;
                BytecodePosition = bytecodePosition;
            }
        }

        private readonly IEnumerable<string> SourceLines;
        private readonly List<byte> Bytecode = new List<byte>();
        private readonly List<string> SymbolTable = new List<string>();
        private readonly Program Program;
        private readonly Dictionary<string, int> Labels = new Dictionary<string, int>();
        private readonly List<WantedLabel> WantedLabels = new List<WantedLabel>();

        public static Program Assemble(IEnumerable<string> sourceLines)
        {
            return new Assembler(sourceLines).Program;
        }

        private Assembler(IEnumerable<string> sourceLines)
        {
            SourceLines = sourceLines;
            CreateStatements();
            SetLabelPositions();
            Program = new Program(1, Bytecode.ToArray(), SymbolTable.ToArray());
        }

        private void CreateStatements()
        {
            foreach (string line in SourceLines)
                ParseLine(line.Trim());
        }

        private void ParseLine(string line)
        {
            if (line.EndsWith(":"))
            {
                AddLabel(line.TrimEnd(':'));
                return;
            }
            AddStatement(line.Split(' '));
        }

        private void AddLabel(string label)
        {
            if (Labels.ContainsKey(label)) throw new AssemblerException("Label defined twice: " + label);
            Labels[label] = Bytecode.Count;
        }

        private void AddStatement(string[] line)
        {
            int parameterCount = line.Length - 1;
            string opcode = line[0].ToUpper();

            switch (opcode)
            {
                case "END": AssertParameterCount(parameterCount, 0, opcode); Emit(0x00); break;
                case "POP": AssertParameterCount(parameterCount, 0, opcode); Emit(0x01); break;
                case "PUSHINT": AssertParameterCount(parameterCount, 1, opcode); Emit(0x02); EmitInt(int.Parse(line[1])); break;
                case "DEFINE": AssertParameterCount(parameterCount, 1, opcode); Emit(0x03); EmitSymbol(line[1]); break;
                case "GET": AssertParameterCount(parameterCount, 1, opcode); Emit(0x04); EmitSymbol(line[1]); break;
                case "NUMEQUAL": AssertParameterCount(parameterCount, 0, opcode); Emit(0x05); break;
                case "ADD": AssertParameterCount(parameterCount, 0, opcode); Emit(0x06); break;
                case "SUB": AssertParameterCount(parameterCount, 0, opcode); Emit(0x07); break;
                case "MUL": AssertParameterCount(parameterCount, 0, opcode); Emit(0x08); break;
                case "DIV": AssertParameterCount(parameterCount, 0, opcode); Emit(0x09); break;
                case "IDIV": AssertParameterCount(parameterCount, 0, opcode); Emit(0x0a); break;
                case "BFALSE": AssertParameterCount(parameterCount, 1, opcode); Emit(0x0b); EmitLabel(line[1]); break;
                case "ENTER": AssertParameterCount(parameterCount, 2, opcode); Emit(0x0c); EmitInt(int.Parse(line[1])); EmitSymbol(line[2]); break;
                case "RET": AssertParameterCount(parameterCount, 0, opcode); Emit(0x0d); break;
                case "CALL": AssertParameterCount(parameterCount, 1, opcode); Emit(0x0e); EmitInt(int.Parse(line[1])); break;
                case "TAILCALL": AssertParameterCount(parameterCount, 1, opcode); Emit(0x0f); EmitInt(int.Parse(line[1])); break;
                case "JMP": AssertParameterCount(parameterCount, 1, opcode); Emit(0x10); EmitLabel(line[1]); break;
                case "GETLABEL": AssertParameterCount(parameterCount, 1, opcode); Emit(0x11); EmitLabel(line[1]); break;
                case "IMOD": AssertParameterCount(parameterCount, 0, opcode); Emit(0x12); break;
                case "SET": AssertParameterCount(parameterCount, 1, opcode); Emit(0x13); EmitSymbol(line[1]); break;
                case "ERROR": AssertParameterCount(parameterCount, 0, opcode); Emit(0xff); break;
                default: throw new AssemblerException("Invalid opcode: " + opcode);
            }
        }

        private void AssertParameterCount(int parameterCount, int wanted, string opcode)
        {
            if (parameterCount != wanted) throw new AssemblerException("Invalid parameter count in opcode " + opcode);
        }

        private void Emit(byte code)
        {
            Bytecode.Add(code);
        }

        private void EmitInt(int valueAsInt)
        {
            for (int i = 0; i < 4; ++i)
            {
                Emit((byte)(valueAsInt % 256));
                valueAsInt /= 256;
            }
        }

        private void EmitSymbol(string symbol)
        {
            if (!SymbolTable.Contains(symbol)) SymbolTable.Add(symbol);
            EmitInt(SymbolTable.IndexOf(symbol));
        }

        private void EmitLabel(string label)
        {
            WantedLabels.Add(new WantedLabel(label, Bytecode.Count));
            EmitInt(0);
        }

        private void SetLabelPositions()
        {
            foreach (var wanted in WantedLabels)
            {
                int bytecodePosition = wanted.BytecodePosition;
                int labelTarget = Labels[wanted.Label];
                for (int i = 0; i < 4; ++i)
                {
                    Bytecode[bytecodePosition++] = (byte)(labelTarget % 256);
                    labelTarget /= 256;
                }
            }
        }
    }
}
