using System;
using System.Collections.Generic;
using System.Linq;

namespace org.lb.lbvm.scheme
{
    public sealed class CompilerException : Exception
    {
        public CompilerException(string message)
            : base(message)
        {
        }
    }

    public sealed class Compiler
    {
        private readonly List<string> CompiledSource = new List<string>();
        private readonly Symbol defineSymbol = new Symbol("define");
        private readonly Symbol ifSymbol = new Symbol("if");
        private readonly Symbol numericEqualSymbol = new Symbol("=");
        private readonly Symbol plusSymbol = new Symbol("+");
        private readonly Symbol minusSymbol = new Symbol("-");
        private readonly Symbol starSymbol = new Symbol("*");
        private readonly Symbol slashSymbol = new Symbol("/");

        public static string[] Compile(string source)
        {
            return new Compiler(source).CompiledSource.ToArray();
        }

        private Compiler(string source)
        {
            var readSource = new Reader().ReadAll(source).ToList();

            for (int i = 0; i < readSource.Count; ++i)
            {
                bool isLastStatement = i == readSource.Count - 1;
                CompileStatement(readSource[i], false);
                if (!isLastStatement) Emit("POP");
            }
            
            Emit("END");
        }

        private void CompileStatement(object o, bool tailCall)
        {
            if (o is bool) CompileBoolConstant((bool)o);
            else if (o is int) CompileIntegerConstant((int)o);
            else if (o is double) CompileDoubleConstant((double)o);
            else if (o is string) CompileStringConstant((string)o);
            else if (o is Symbol) CompileSymbol((Symbol)o);
            else if (o is List<object>) CompileList((List<object>)o, tailCall);
            else throw new CompilerException("Internal error: I don't know how to compile object of type " + o.GetType());
        }

        private void Emit(string line)
        {
            CompiledSource.Add(line);
            System.Diagnostics.Debug.Print("EMIT   " + line);
        }

        private void CompileBoolConstant(bool value)
        {
            Emit(value ? "PUSHTRUE" : "PUSHFALSE");
        }

        private void CompileIntegerConstant(int value)
        {
            Emit("PUSHINT " + value);
        }

        private void CompileDoubleConstant(double value)
        {
            throw new CompilerException("Compiling DOUBLEs not implemented yet");
        }

        private void CompileStringConstant(string value)
        {
            throw new CompilerException("Compiling STRINGs not implemented yet");
        }

        private void CompileSymbol(Symbol value)
        {
            Emit("PUSHVAR " + value.Name);
        }

        private void CompileList(List<object> value, bool tailCall)
        {
            if (value.Count > 2 && defineSymbol.Equals(value[0])) CompileDefine(value);
            else if (value.Count == 4 && ifSymbol.Equals(value[0])) CompileIf(value, tailCall);
            else if (value.Count == 3 && numericEqualSymbol.Equals(value[0])) CompileNumericOperation(value, "NUMEQUAL");
            else if (value.Count == 3 && plusSymbol.Equals(value[0])) CompileNumericOperation(value, "ADD");
            else if (value.Count == 3 && minusSymbol.Equals(value[0])) CompileNumericOperation(value, "SUB");
            else if (value.Count == 3 && starSymbol.Equals(value[0])) CompileNumericOperation(value, "MUL");
            else if (value.Count == 3 && slashSymbol.Equals(value[0])) CompileNumericOperation(value, "DIV");
            else CompileFunctionCall(value, tailCall);
        }

        private void CompileDefine(List<object> value)
        {
            if (value[1] is List<object>) CompileFunctionDefinition(value, (List<object>)value[1], value.Skip(2).ToList());
            else CompileVariableDefinition(value);
        }

        private void CompileFunctionDefinition(IEnumerable<object> value, List<object> functionNameAndParameters, List<object> body)
        {
            if (!functionNameAndParameters.All(i => i is Symbol)) throw new CompilerException("Syntax error in function definition: Not all values are symbols");

            string name = ((Symbol)functionNameAndParameters[0]).Name;
            List<string> parameters = functionNameAndParameters.Skip(1).Select(i => ((Symbol)i).Name).ToList();
            List<string> freeVariables = FindFreeVariablesInLambda(parameters, body).ToList();

            string functionLine = "FUNCTION " + name + " " + string.Join(" ", parameters);
            if (freeVariables.Count > 0) functionLine += " &closingover " + string.Join(" ", freeVariables);
            Emit(functionLine);
            for (int i = 0; i < body.Count; ++i)
            {
                bool isLastStatement = i == body.Count - 1;
                CompileStatement(body[i], isLastStatement);
                if (!isLastStatement) Emit("POP");
            }
            Emit("RET"); // HACK: If last statement was a TAILCALL, the RET is not needed
            Emit("ENDFUNCTION");
        }

        private IEnumerable<string> FindFreeVariablesInLambda(List<string> parameters, List<object> body)
        {
            return new List<string>(); // TODO
        }

        private void CompileVariableDefinition(List<object> value)
        {
            throw new CompilerException("Compiling VARDEFs not implemented yet");
        }

        private void CompileIf(List<object> value, bool tailCall)
        {
            CompileStatement(value[1], false);
            string falseLabel = GenerateLabel();
            string doneLabel = GenerateLabel();
            Emit("BFALSE " + falseLabel);
            CompileStatement(value[2], tailCall);
            Emit("JMP " + doneLabel);
            Emit(falseLabel + ":");
            CompileStatement(value[3], tailCall);
            Emit(doneLabel + ":");
        }

        private int nextGeneratedLabelNumber;

        private string GenerateLabel()
        {
            return "##compiler__label##" + nextGeneratedLabelNumber++;
        }

        private void CompileNumericOperation(List<object> values, string op)
        {
            CompileStatement(values[1], false);
            CompileStatement(values[2], false);
            Emit(op);
        }

        private void CompileFunctionCall(List<object> value, bool tailCall)
        {
            foreach (object o in value) CompileStatement(o, false);
            Emit((tailCall ? "TAILCALL " : "CALL ") + (value.Count - 1));
        }
    }
}
