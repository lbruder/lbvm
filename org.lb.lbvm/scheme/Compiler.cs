using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using org.lb.lbvm.Properties;
using org.lb.lbvm.exceptions;
using org.lb.lbvm.runtime;

namespace org.lb.lbvm.scheme
{
    // TODO: let, or, and   /   macro system (if macros, then move COND to macro aswell)

    internal sealed class Compiler
    {
        private readonly List<string> CompiledSource = new List<string>();
        private readonly NameGenerator NameGenerator = new NameGenerator();

        public static IEnumerable<string> Compile(string source)
        {
            return new Compiler("(define (##compiler__main##) " + Resources.SchemeInitScript + "\n" + source + ") (##compiler__main##)").CompiledSource.ToArray();
        }

        private Compiler(string source)
        {
            var readSource = new Reader().ReadAll(source).ToList();
            CompileBlock(readSource, false);
            Emit("END");
        }

        private void CompileStatement(object o, bool tailCall, bool quoting = false)
        {
            if (o is bool) Emit((bool)o ? "PUSHTRUE" : "PUSHFALSE");
            else if (o is int) Emit("PUSHINT " + (int)o);
            else if (o is double) Emit("PUSHDBL " + ((double)o).ToString(CultureInfo.InvariantCulture));
            else if (o is string) Emit("PUSHSTR " + StringObject.Escape((string)o));
            else if (Symbols.NilSymbol.Equals(o)) Emit("PUSHNIL");
            else if (o is char) Emit("PUSHCHR " + (byte)(char)o);
            else if (o is Symbol) Emit((quoting ? "PUSHSYM " : "PUSHVAR ") + o);
            else if (o is List<object>)
            {
                if (quoting) CompileQuotedList((List<object>)o);
                else CompileList((List<object>)o, tailCall);
            }
            else throw new CompilerException("Internal error: I don't know how to compile " + (quoting ? "quoted " : "") + "object of type " + o.GetType());
        }

        private void Emit(string line)
        {
            CompiledSource.Add(line);
            //System.Diagnostics.Debug.Print("EMIT   " + line);
        }

        private void CompileList(List<object> value, bool tailCall)
        {
            if (value.Count == 0) throw new CompilerException("Empty list cannot be called as a function");
            object firstValue = value[0];

            if (firstValue is Symbol)
            {
                var function = Symbols.GetFunction(firstValue.ToString());
                if (function != null)
                {
                    AssertParameterCount(function.Arity, value.Count - 1, function.Opcode);
                    for (int i = 1; i <= function.Arity; ++i) CompileStatement(value[i], false);
                    Emit(function.Opcode);
                    return;
                }
            }

            if (Symbols.DefineSymbol.Equals(firstValue)) CompileDefine(value);
            else if (Symbols.LambdaSymbol.Equals(firstValue)) CompileLambda(value);
            else if (Symbols.SetSymbol.Equals(firstValue)) CompileSet(value);
            else if (Symbols.QuoteSymbol.Equals(firstValue)) CompileQuote(value);
            else if (Symbols.IfSymbol.Equals(firstValue)) CompileIf(value, tailCall);
            else if (Symbols.BeginSymbol.Equals(firstValue)) CompileBegin(value, tailCall);
            else if (Symbols.CondSymbol.Equals(firstValue)) CompileCond(value, tailCall);
            else CompileFunctionCall(value, tailCall);
        }

        private void AssertParameterCount(int expected, int got, string function)
        {
            if (expected != got) throw new CompilerException(function + ": Expected " + expected + " parameter(s), got " + got);
        }

        private void CompileLambda(List<object> value)
        {
            if (!(value[1] is List<object>)) throw new CompilerException("Invalid lambda form");
            List<object> nameAndParams = new List<object>();
            nameAndParams.Add(NameGenerator.NextLambdaName());
            nameAndParams.AddRange((List<object>)value[1]);
            CompileFunctionDefinition(nameAndParams, value.Skip(2).ToList());
        }

        private void CompileDefine(List<object> value)
        {
            if (value[1] is List<object>) CompileFunctionDefinition((List<object>)value[1], value.Skip(2).ToList());
            else CompileVariableDefinition(value);
        }

        private void CompileFunctionDefinition(List<object> functionNameAndParameters, List<object> body)
        {
            CodeInspection.AssertAllFunctionParametersAreSymbols(functionNameAndParameters);

            string name = functionNameAndParameters[0].ToString();
            List<string> parameters = functionNameAndParameters.Skip(1).Select(i => i.ToString()).ToList();
            bool hasRestParameter = parameters.Any(i => i == ".");
            string restParameter = "";
            if (hasRestParameter)
            {
                if (!(parameters.Count > 1 && parameters[parameters.Count - 2] == ".")) throw new CompilerException(name + ": There may be only one rest parameter in function definition");
                restParameter = parameters[parameters.Count - 1];
                parameters.RemoveRange(parameters.Count - 2, 2);
            }

            HashSet<string> defines = new HashSet<string>();
            List<string> freeVariables = CodeInspection.FindFreeVariablesInLambda(parameters, body, defines).ToList();
            freeVariables.Remove(restParameter);

            string functionLine = "FUNCTION " + name + " " + string.Join(" ", parameters);
            if (hasRestParameter) functionLine += " &rest " + restParameter;
            if (freeVariables.Count > 0) functionLine += " &closingover " + string.Join(" ", freeVariables);
            if (defines.Count > 0) functionLine += " &localdefines " + string.Join(" ", defines);
            Emit(functionLine);
            CompileBlock(body, true);
            Emit("RET");
            Emit("ENDFUNCTION");
            Emit("PUSHVAR " + name);
        }

        private void CompileVariableDefinition(List<object> value)
        {
            AssertParameterCount(2, value.Count - 1, "define variable");
            if (!(value[1] is Symbol)) throw new CompilerException("Target of define is not a symbol");
            Symbol target = (Symbol)value[1];
            CompileStatement(value[2], false);
            Emit("DEFINE " + target);
            Emit("PUSHVAR " + target);
        }

        private void CompileSet(List<object> value)
        {
            AssertParameterCount(2, value.Count - 1, "set!");
            if (!(value[1] is Symbol)) throw new CompilerException("Target of set! is not a symbol");
            Symbol target = (Symbol)value[1];
            CompileStatement(value[2], false);
            Emit("SET " + target);
            Emit("PUSHVAR " + target);
        }

        private void CompileQuote(List<object> value)
        {
            AssertParameterCount(1, value.Count - 1, "quote");
            CompileStatement(value[1], false, true);
        }

        private void CompileQuotedList(List<object> value)
        {
            Emit("PUSHVAR list");
            foreach (object o in value) CompileStatement(o, false, true);
            Emit("CALL " + value.Count);
        }

        private void CompileIf(List<object> value, bool tailCall)
        {
            CompileStatement(value[1], false);
            string falseLabel = NameGenerator.NextLabel();
            string doneLabel = NameGenerator.NextLabel();
            Emit("BFALSE " + falseLabel);
            CompileStatement(value[2], tailCall);
            Emit("JMP " + doneLabel);
            Emit(falseLabel + ":");
            CompileStatement(value[3], tailCall);
            Emit(doneLabel + ":");
        }

        private void CompileBegin(IEnumerable<object> value, bool tailCall)
        {
            CompileBlock(value.Skip(1).ToList(), tailCall);
        }

        private void CompileCond(IEnumerable<object> value, bool tailCall)
        {
            string doneLabel = NameGenerator.NextLabel();
            foreach (object o in value.Skip(1))
            {
                if (!(o is List<object>)) throw new CompilerException("Invalid COND form");
                var list = (List<object>)o;
                if (list.Count < 2) throw new CompilerException("Invalid COND form");

                if (Symbols.ElseSymbol.Equals(list[0]))
                {
                    CompileBlock(list.Skip(1).ToList(), tailCall);
                    Emit("JMP " + doneLabel);
                }
                else
                {
                    CompileStatement(list[0], false);
                    string falseLabel = NameGenerator.NextLabel();
                    Emit("BFALSE " + falseLabel);
                    CompileBlock(list.Skip(1).ToList(), tailCall);
                    Emit("JMP " + doneLabel);
                    Emit(falseLabel + ":");
                }
            }
            Emit(doneLabel + ":");
        }

        private void CompileBlock(List<object> statements, bool tailCall)
        {
            for (int i = 0; i < statements.Count; ++i)
            {
                bool isLastStatement = i == statements.Count - 1;
                CompileStatement(statements[i], tailCall && isLastStatement);
                if (!isLastStatement) Emit("POP");
            }
        }

        private void CompileFunctionCall(List<object> value, bool tailCall)
        {
            foreach (object o in value) CompileStatement(o, false);
            Emit((tailCall ? "TAILCALL " : "CALL ") + (value.Count - 1));
        }
    }
}
