using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using org.lb.lbvm.Properties;

namespace org.lb.lbvm.scheme
{
    // TODO: lambda, quote, begin
    // TODO: let, or, and / macro system (if macros, then move COND to macro aswell)

    public sealed class Compiler
    {
        private readonly List<string> CompiledSource = new List<string>();
        private readonly Symbol defineSymbol = new Symbol("define");
        private readonly Symbol ifSymbol = new Symbol("if");
        private readonly Symbol elseSymbol = new Symbol("else");
        private readonly Symbol condSymbol = new Symbol("cond");
        private readonly Symbol consSymbol = new Symbol("cons");
        private readonly Symbol conspSymbol = new Symbol("pair?");
        private readonly Symbol carSymbol = new Symbol("car");
        private readonly Symbol cdrSymbol = new Symbol("cdr");
        private readonly Symbol nilSymbol = new Symbol("nil");
        private readonly Symbol numericEqualSymbol = new Symbol("=");
        private readonly Symbol plusSymbol = new Symbol("+");
        private readonly Symbol minusSymbol = new Symbol("-");
        private readonly Symbol starSymbol = new Symbol("*");
        private readonly Symbol slashSymbol = new Symbol("/");
        private readonly Symbol remSymbol = new Symbol("rem");
        private readonly Symbol ltSymbol = new Symbol("<");
        private readonly Symbol gtSymbol = new Symbol(">");
        private readonly Symbol leSymbol = new Symbol("<=");
        private readonly Symbol geSymbol = new Symbol(">=");
        private readonly string[] specialFormSymbols = { "if", "define", "lambda", "quote", "begin", "cond" };
        private readonly List<Symbol> optimizedFunctionSymbols;

        public static string[] Compile(string source)
        {
            return new Compiler("(define (##compiler__main##) " + Resources.SchemeInitScript + "\n" + source + ") (##compiler__main##)").CompiledSource.ToArray();
        }

        private Compiler(string source)
        {
            optimizedFunctionSymbols = new List<Symbol> { numericEqualSymbol, plusSymbol, minusSymbol, starSymbol, slashSymbol, remSymbol,
                leSymbol, ltSymbol, geSymbol, gtSymbol, elseSymbol, consSymbol, conspSymbol, carSymbol, cdrSymbol};
            var readSource = new Reader().ReadAll(source).ToList();
            CompileBlock(readSource, false);
            Emit("END");
        }

        private void CompileStatement(object o, bool tailCall)
        {
            if (o is bool) Emit((bool)o ? "PUSHTRUE" : "PUSHFALSE");
            else if (o is int) Emit("PUSHINT " + (int)o);
            else if (o is double) Emit("PUSHDBL " + ((double)o).ToString(CultureInfo.InvariantCulture));
            else if (o is string) Emit("PUSHSTR \"" + EscapeString((string)o) + "\"");
            else if (nilSymbol.Equals(o)) Emit("PUSHNIL");
            else if (o is Symbol) Emit("PUSHVAR " + ((Symbol)o).Name);
            else if (o is List<object>) CompileList((List<object>)o, tailCall);
            else throw new CompilerException("Internal error: I don't know how to compile object of type " + o.GetType());
        }

        private void Emit(string line)
        {
            CompiledSource.Add(line);
            System.Diagnostics.Debug.Print("EMIT   " + line);
        }

        private static string EscapeString(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private void CompileList(List<object> value, bool tailCall)
        {
            if (value.Count == 0) throw new CompilerException("Empty list cannot be called as a function");
            object firstValue = value[0];

            if (defineSymbol.Equals(firstValue)) CompileDefine(value);
            else if (ifSymbol.Equals(firstValue)) CompileIf(value, tailCall);
            else if (condSymbol.Equals(firstValue)) CompileCond(value, tailCall);
            else if (numericEqualSymbol.Equals(firstValue)) CompileBinaryOperation(value, "NUMEQUAL");
            else if (plusSymbol.Equals(firstValue)) CompileBinaryOperation(value, "ADD");
            else if (minusSymbol.Equals(firstValue)) CompileBinaryOperation(value, "SUB");
            else if (starSymbol.Equals(firstValue)) CompileBinaryOperation(value, "MUL");
            else if (slashSymbol.Equals(firstValue)) CompileBinaryOperation(value, "DIV");
            else if (remSymbol.Equals(firstValue)) CompileBinaryOperation(value, "IMOD");
            else if (ltSymbol.Equals(firstValue)) CompileBinaryOperation(value, "NUMLT");
            else if (leSymbol.Equals(firstValue)) CompileBinaryOperation(value, "NUMLE");
            else if (gtSymbol.Equals(firstValue)) CompileBinaryOperation(value, "NUMGT");
            else if (geSymbol.Equals(firstValue)) CompileBinaryOperation(value, "NUMGE");
            else if (consSymbol.Equals(firstValue)) CompileBinaryOperation(value, "MAKEPAIR");
            else if (conspSymbol.Equals(firstValue)) CompileUnaryOperation(value, "ISPAIR");
            else if (carSymbol.Equals(firstValue)) CompileUnaryOperation(value, "PAIR1");
            else if (cdrSymbol.Equals(firstValue)) CompileUnaryOperation(value, "PAIR2");
            else CompileFunctionCall(value, tailCall);
        }

        private void CompileDefine(List<object> value)
        {
            if (value[1] is List<object>) CompileFunctionDefinition(value, (List<object>)value[1], value.Skip(2).ToList());
            else CompileVariableDefinition(value);
        }

        private void CompileFunctionDefinition(IEnumerable<object> value, List<object> functionNameAndParameters, List<object> body)
        {
            AssertAllFunctionParametersAreSymbols(functionNameAndParameters);

            string name = ((Symbol)functionNameAndParameters[0]).Name;
            List<string> parameters = functionNameAndParameters.Skip(1).Select(i => ((Symbol)i).Name).ToList();
            HashSet<string> defines = new HashSet<string>();
            List<string> freeVariables = FindFreeVariablesInLambda(parameters, body, defines).ToList();
            foreach (string i in defines) freeVariables.Remove(i);

            string functionLine = "FUNCTION " + name + " " + string.Join(" ", parameters);
            if (freeVariables.Count > 0) functionLine += " &closingover " + string.Join(" ", freeVariables);
            if (defines.Count > 0) functionLine += " &localdefines " + string.Join(" ", defines);
            Emit(functionLine);
            for (int i = 0; i < body.Count; ++i)
            {
                bool isLastStatement = i == body.Count - 1;
                CompileStatement(body[i], isLastStatement);
                if (!isLastStatement) Emit("POP");
            }
            Emit("RET"); // HACK: If last statement was a TAILCALL, the RET is not needed
            Emit("ENDFUNCTION");
            Emit("PUSHVAR " + name);
        }

        private static void AssertAllFunctionParametersAreSymbols(IEnumerable<object> parameters)
        {
            if (!parameters.All(i => i is Symbol))
                throw new CompilerException("Syntax error in function definition: Not all parameter names are symbols");
        }

        // HACK: HashSet parameter is ugly
        private IEnumerable<string> FindFreeVariablesInLambda(IEnumerable<string> parameters, IEnumerable<object> body, HashSet<string> localVariablesDefinedInLambda)
        {
            HashSet<string> accessedVariables = new HashSet<string>();
            foreach (object o in body) FindAccessedVariables(o, accessedVariables, localVariablesDefinedInLambda);
            accessedVariables.Remove("nil");
            foreach (string p in parameters) accessedVariables.Remove(p);
            return accessedVariables;
        }

        private void FindAccessedVariables(object o, HashSet<string> accessedVariables, HashSet<string> definedVariables)
        {
            if (o is List<object>)
            {
                var list = (List<object>)o;
                if (list.Count > 1 && defineSymbol.Equals(list[0]) && list[1] is List<object>) // define function
                {
                    string name = ((Symbol)((List<object>)list[1])[0]).Name;
                    definedVariables.Add(name);
                    var parameters = ((List<object>)list[1]).Skip(1).ToList();
                    AssertAllFunctionParametersAreSymbols(parameters);
                    foreach (var i in FindFreeVariablesInLambda(parameters.Select(i => ((Symbol)i).Name), list.Skip(2), new HashSet<string>()))
                        if (!definedVariables.Contains(i)) accessedVariables.Add(i);
                }
                else // Function call TODO: Lambdas, simple DEFINEs
                {
                    // Special handling for first parameter: +, -, *, /, =...
                    bool first = true;
                    foreach (object i in list)
                    {
                        if (first && optimizedFunctionSymbols.Contains(i)) first = false;
                        else FindAccessedVariables(i, accessedVariables, definedVariables);
                    }
                }
            }
            else if (o is Symbol)
            {
                string symbol = ((Symbol)o).Name;
                if (!specialFormSymbols.Contains(symbol) && !definedVariables.Contains(symbol))
                    accessedVariables.Add(symbol);
            }
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

        private void AssertParameterCount(int expected, int got, string function)
        {
            if (expected != got) throw new CompilerException(function + ": Expected " + expected + " parameters, got " + got);
        }

        private int nextGeneratedLabelNumber;

        private string GenerateLabel()
        {
            return "##compiler__label##" + nextGeneratedLabelNumber++;
        }

        private void CompileCond(IEnumerable<object> value, bool tailCall)
        {
            string doneLabel = GenerateLabel();
            foreach (object o in value.Skip(1))
            {
                if (!(o is List<object>)) throw new CompilerException("Invalid COND form");
                var list = (List<object>)o;
                if (list.Count < 2) throw new CompilerException("Invalid COND form");

                if (elseSymbol.Equals(list[0]))
                {
                    CompileBlock(list.Skip(1).ToList(), tailCall);
                    Emit("JMP " + doneLabel);
                }
                else
                {
                    CompileStatement(list[0], false);
                    string falseLabel = GenerateLabel();
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

        private void CompileUnaryOperation(List<object> values, string op)
        {
            AssertParameterCount(2, values.Count, op);
            CompileStatement(values[1], false);
            Emit(op);
        }

        private void CompileBinaryOperation(List<object> values, string op)
        {
            AssertParameterCount(3, values.Count, op);
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
