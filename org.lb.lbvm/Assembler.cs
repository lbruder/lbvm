using System;
using System.Collections.Generic;
using System.Globalization;

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
            if (line.ToUpper().StartsWith("FUNCTION"))
            {
                HandleFunction(line.Split());
                return;
            }

            if (line.ToUpper().StartsWith("ENDFUNCTION"))
            {
                HandleEndFunction(line.Split());
                return;
            }

            if (line.EndsWith(":"))
            {
                AddLabel(line.TrimEnd(':'));
                return;
            }

            AddStatement(line.Split(' '));
        }

        private enum Mode
        {
            Parameter,
            ClosingOverVariable,
            LocalDefines
        };

        private sealed class FunctionStatement
        {
            public readonly string LabelStart;
            public readonly string LabelEnd;
            public readonly string Name;
            public readonly List<string> ClosingOverVariables;
            public FunctionStatement(string name, string labelStart, string labelEnd, List<string> closingOverVariables)
            {
                LabelStart = labelStart;
                LabelEnd = labelEnd;
                Name = name;
                ClosingOverVariables = closingOverVariables;
            }
        }

        private readonly Stack<FunctionStatement> functionStatements = new Stack<FunctionStatement>();

        private void HandleFunction(string[] line)
        {
            if (line.Length < 1) throw new AssemblerException("Syntax error in FUNCTION definition");
            string name = line[1];
            string labelStart = generateLabel();
            string labelEnd = generateLabel();
            List<string> parameters = new List<string>();
            List<string> closingOverVariables = new List<string>();
            List<string> localDefines = new List<string>();
            Mode mode = Mode.Parameter;
            for (int i = 2; i < line.Length; ++i)
            {
                if (line[i] == "") continue;
                if (line[i].ToLower() == "&closingover") mode = Mode.ClosingOverVariable;
                else if (line[i].ToLower() == "&localdefines") mode = Mode.LocalDefines;
                else if (mode == Mode.Parameter) parameters.Add(line[i]);
                else if (mode == Mode.ClosingOverVariable) closingOverVariables.Add(line[i]);
                else if (mode == Mode.LocalDefines) localDefines.Add(line[i]);
                else throw new AssemblerException("Internal error 1 in assembler");
            }
            functionStatements.Push(new FunctionStatement(name, labelStart, labelEnd, closingOverVariables));

            ParseLine("JMP " + labelEnd);
            ParseLine(labelStart + ":");
            ParseLine("ENTER " + (parameters.Count + closingOverVariables.Count) + " " + name);
            foreach (string v in localDefines) ParseLine("MAKEVAR " + v);
            var closingReversed = new List<string>(closingOverVariables);
            closingReversed.Reverse();
            foreach (string v in closingReversed) ParseLine("DEFINE " + v);
            var parametersReversed = new List<string>(parameters);
            parametersReversed.Reverse();
            foreach (string p in parametersReversed) ParseLine("DEFINE " + p);
            ParseLine("POP");
        }

        private int generatedLabelNumber;

        private string generateLabel()
        {
            return "##generated_label##" + generatedLabelNumber++;
        }

        private void HandleEndFunction(string[] line)
        {
            var functionToEnd = functionStatements.Pop();
            ParseLine(functionToEnd.LabelEnd + ":");
            ParseLine("PUSHLABEL " + functionToEnd.LabelStart);
            ParseLine("DEFINE " + functionToEnd.Name);
            if (functionToEnd.ClosingOverVariables.Count > 0)
            {
                ParseLine("PUSHVAR " + functionToEnd.Name);
                foreach (var v in functionToEnd.ClosingOverVariables) ParseLine("PUSHSYM " + v);
                ParseLine("MAKECLOSURE " + functionToEnd.ClosingOverVariables.Count);
                ParseLine("SET " + functionToEnd.Name);
            }
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
                case "PUSHVAR": AssertParameterCount(parameterCount, 1, opcode); Emit(0x04); EmitSymbol(line[1]); break;
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
                case "PUSHLABEL": AssertParameterCount(parameterCount, 1, opcode); Emit(0x11); EmitLabel(line[1]); break;
                case "IMOD": AssertParameterCount(parameterCount, 0, opcode); Emit(0x12); break;
                case "SET": AssertParameterCount(parameterCount, 1, opcode); Emit(0x13); EmitSymbol(line[1]); break;
                case "PUSHSYM": AssertParameterCount(parameterCount, 1, opcode); Emit(0x14); EmitSymbol(line[1]); break;
                case "PUSHTRUE": AssertParameterCount(parameterCount, 0, opcode); Emit(0x15); break;
                case "PUSHFALSE": AssertParameterCount(parameterCount, 0, opcode); Emit(0x16); break;
                case "MAKECLOSURE": AssertParameterCount(parameterCount, 1, opcode); Emit(0x17); EmitInt(int.Parse(line[1])); break;
                case "NUMLT": AssertParameterCount(parameterCount, 0, opcode); Emit(0x18); break;
                case "NUMLE": AssertParameterCount(parameterCount, 0, opcode); Emit(0x19); break;
                case "NUMGT": AssertParameterCount(parameterCount, 0, opcode); Emit(0x1a); break;
                case "NUMGE": AssertParameterCount(parameterCount, 0, opcode); Emit(0x1b); break;
                case "PUSHDBL": AssertParameterCount(parameterCount, 1, opcode); Emit(0x1c); EmitDouble(double.Parse(line[1], NumberStyles.Any, CultureInfo.InvariantCulture)); break;
                case "MAKEVAR": AssertParameterCount(parameterCount, 1, opcode); Emit(0x1d); EmitSymbol(line[1]); break;
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
            foreach (byte b in BitConverter.GetBytes(valueAsInt)) Emit(b);
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

        private void EmitDouble(double valueAsDouble)
        {
            foreach (byte b in BitConverter.GetBytes(valueAsDouble)) Emit(b);
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
