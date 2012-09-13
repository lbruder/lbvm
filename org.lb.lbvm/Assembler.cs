using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using org.lb.lbvm.runtime;

namespace org.lb.lbvm
{
    internal sealed class Assembler
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

        private enum Mode
        {
            Parameter,
            Rest,
            ClosingOverVariable,
            LocalDefines
        };

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
            foreach (string line in sourceLines) ParseLine(line.Trim());
            SetLabelPositions();
            Program = new Program(1, Bytecode.ToArray(), SymbolTable.ToArray());
        }

        private void ParseLine(string line)
        {
            if (line.ToUpper().StartsWith("FUNCTION")) HandleFunction(line.Split());
            else if (line.ToUpper().StartsWith("ENDFUNCTION")) HandleEndFunction(line.Split());
            else if (line.EndsWith(":")) AddLabel(line.TrimEnd(':'));
            else AddStatement(SplitLine(line));
        }

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
            if (line.Length < 1) throw new exceptions.AssemblerException("Syntax error in FUNCTION definition");
            string name = line[1];
            string labelStart = generateLabel();
            string labelEnd = generateLabel();
            List<string> parameters = new List<string>();
            List<string> closingOverVariables = new List<string>();
            List<string> localDefines = new List<string>();
            string restParameter = "";
            Mode mode = Mode.Parameter;
            foreach (var operand in line.Skip(2))
            {
                if (operand == "") continue;
                if (operand.ToLower() == "&closingover") mode = Mode.ClosingOverVariable;
                else if (operand.ToLower() == "&rest") mode = Mode.Rest;
                else if (operand.ToLower() == "&localdefines") mode = Mode.LocalDefines;
                else if (mode == Mode.Parameter) parameters.Add(operand);
                else if (mode == Mode.ClosingOverVariable) closingOverVariables.Add(operand);
                else if (mode == Mode.LocalDefines) localDefines.Add(operand);
                else if (mode == Mode.Rest)
                {
                    if (restParameter != "") throw new exceptions.AssemblerException(name + ": Only one rest parameter allowed in function definition");
                    restParameter = operand;
                }
                else throw new exceptions.AssemblerException("Internal error 1 in assembler");
            }
            functionStatements.Push(new FunctionStatement(name, labelStart, labelEnd, closingOverVariables));
            if (restParameter != "") parameters.Add(restParameter);

            ParseLine("JMP " + labelEnd);
            ParseLine(labelStart + ":");
            if (restParameter == "")
                ParseLine("ENTER " + (parameters.Count + closingOverVariables.Count) + " " + name);
            else
                ParseLine("ENTERR " + (parameters.Count + closingOverVariables.Count) + " " + closingOverVariables.Count + " " + name);
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
            if (Labels.ContainsKey(label)) throw new exceptions.AssemblerException("Label defined twice: " + label);
            Labels[label] = Bytecode.Count;
        }

        private void AddStatement(string[] line)
        {
            int parameterCount = line.Length - 1;
            string opcode = line[0].ToUpper();

            var NullaryOpcodes = new Dictionary<string, byte>{
                {"END", 0x00}, {"POP", 0x01}, {"NUMEQUAL", 0x05}, {"ADD", 0x06}, {"SUB", 0x07}, {"MUL", 0x08}, {"DIV", 0x09}, {"IDIV", 0x0a},
                {"RET", 0x0d}, {"IMOD", 0x12}, {"PUSHTRUE", 0x15}, {"PUSHFALSE", 0x16}, {"NUMLT", 0x18}, {"NUMLE", 0x19}, {"NUMGT", 0x1a}, {"NUMGE", 0x1b},
                {"MAKEPAIR", 0x1e}, {"ISPAIR", 0x1f}, {"PAIR1", 0x20}, {"PAIR2", 0x21}, {"PUSHNIL", 0x22}, {"RANDOM", 0x24},
                {"OBJEQUAL", 0x25}, {"ISNULL", 0x26}, {"PRINT", 0x27}, {"ISNUMBER", 0x29}, {"ISSTRING", 0x2a}, {"STREQUAL", 0x2b}, {"STREQUALCI", 0x2c},
                {"STRLT", 0x2d}, {"STRLTCI", 0x2e}, {"STRGT", 0x2f}, {"STRGTCI", 0x30}, {"STRLEN", 0x31}, {"SUBSTR", 0x32}, {"STRAPPEND", 0x33}, 
                {"ISCHAR", 0x35}, {"CHREQUAL", 0x36}, {"CHREQUALCI", 0x37}, {"CHRLT", 0x38}, {"CHRLTCI", 0x39}, {"CHRGT", 0x3a}, {"CHRGTCI", 0x3b},
                {"CHRTOINT", 0x3c}, {"INTTOCHR", 0x3d}, {"STRREF", 0x3e}, {"SETSTRREF", 0x3f}, {"MAKESTR", 0x40}, {"STRTONUM", 0x41}, {"NUMTOSTR", 0x42},
                {"STRTOSYM", 0x43}, {"SYMTOSTR", 0x44}, {"THROW", 0x45}, {"ISBOOL", 0x46}, {"ISSYMBOL", 0x47}, {"ISINT", 0x48}, {"ISFLOAT", 0x49}, {"ERROR", 0xff} };

            var UnaryIntOpcodes = new Dictionary<string, byte> { { "PUSHINT", 0x02 }, { "CALL", 0x0e }, { "TAILCALL", 0x0f }, { "MAKECLOSURE", 0x17 } };
            var UnarySymbolOpcodes = new Dictionary<string, byte> { { "DEFINE", 0x03 }, { "PUSHVAR", 0x04 }, { "SET", 0x13 }, { "PUSHSYM", 0x14 }, { "MAKEVAR", 0x1d } };
            var UnaryLabelOpcodes = new Dictionary<string, byte> { { "BFALSE", 0x0b }, { "JMP", 0x10 }, { "PUSHLABEL", 0x11 } };

            if (NullaryOpcodes.ContainsKey(opcode))
            {
                AssertParameterCount(parameterCount, 0, opcode);
                Emit(NullaryOpcodes[opcode]);
            }
            else if (UnaryIntOpcodes.ContainsKey(opcode))
            {
                AssertParameterCount(parameterCount, 1, opcode);
                Emit(UnaryIntOpcodes[opcode]);
                EmitInt(int.Parse(line[1]));
            }
            else if (UnarySymbolOpcodes.ContainsKey(opcode))
            {
                AssertParameterCount(parameterCount, 1, opcode);
                Emit(UnarySymbolOpcodes[opcode]);
                EmitSymbol(line[1]);
            }
            else if (UnaryLabelOpcodes.ContainsKey(opcode))
            {
                AssertParameterCount(parameterCount, 1, opcode);
                Emit(UnaryLabelOpcodes[opcode]);
                EmitLabel(line[1]);
            }
            else
                switch (opcode)
                {
                    case "PUSHDBL": AssertParameterCount(parameterCount, 1, opcode); Emit(0x1c); EmitDouble(double.Parse(line[1], NumberStyles.Any, CultureInfo.InvariantCulture)); break;
                    case "ENTER": AssertParameterCount(parameterCount, 2, opcode); Emit(0x0c); EmitInt(int.Parse(line[1])); EmitSymbol(line[2]); break;
                    case "ENTERR": AssertParameterCount(parameterCount, 3, opcode); Emit(0x23); EmitInt(int.Parse(line[1])); EmitInt(int.Parse(line[2])); EmitSymbol(line[3]); break;
                    case "PUSHSTR": AssertParameterCount(parameterCount, 1, opcode); Emit(0x28); EmitString(line[1]); break;
                    case "PUSHCHR": AssertParameterCount(parameterCount, 1, opcode); Emit(0x34); EmitInt(int.Parse(line[1])); break;
                    default: throw new exceptions.AssemblerException("Invalid opcode: " + opcode);
                }
        }

        private void AssertParameterCount(int parameterCount, int wanted, string opcode)
        {
            if (parameterCount != wanted) throw new exceptions.AssemblerException("Invalid parameter count in opcode " + opcode);
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

        private void EmitString(string value)
        {
            value = StringObject.Unescape(value);
            EmitInt(value.Length);
            foreach (byte b in value) Emit(b);
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

        private static string[] SplitLine(string line)
        {
            List<string> ret = new List<string>();
            line = line.TrimEnd();
            while (line != "") ret.Add(GetNextPartOfString(ref line));
            return ret.ToArray();
        }

        private static string GetNextPartOfString(ref string line)
        {
            line = line.TrimStart();
            string ret;
            if (line.StartsWith("\""))
            {
                int positionOfQuote = line.IndexOf('"', 1);
                if (positionOfQuote == -1) throw new exceptions.AssemblerException("Unterminated string literal");
                ret = line.Substring(1, positionOfQuote - 1);
                line = line.Substring(positionOfQuote + 1);
            }
            else
            {
                int positionOfWhitespace = line.IndexOfAny(" \n\r\t".ToCharArray());
                if (positionOfWhitespace == -1)
                {
                    ret = line;
                    line = "";
                }
                else
                {
                    ret = line.Substring(0, positionOfWhitespace);
                    line = line.Substring(positionOfWhitespace + 1);
                }
            }
            return ret;
        }
    }
}
