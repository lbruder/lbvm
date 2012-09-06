using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using org.lb.lbvm.Properties;
using org.lb.lbvm.runtime;

namespace org.lb.lbvm.scheme
{
    // TODO:
    //boolean?
    //symbol?
    //char?
    //vector?
    //procedure?

    // Conversions number->string, string->symbol etc.

    // TODO: lambda, begin
    // TODO: let, or, and   /   macro system (if macros, then move COND to macro aswell)

    public sealed class Compiler
    {
        private readonly List<string> CompiledSource = new List<string>();
        private readonly Symbol defineSymbol = Symbol.fromString("define");
        private readonly Symbol setSymbol = Symbol.fromString("set!");
        private readonly Symbol ifSymbol = Symbol.fromString("if");
        private readonly Symbol quoteSymbol = Symbol.fromString("quote");
        private readonly Symbol elseSymbol = Symbol.fromString("else");
        private readonly Symbol condSymbol = Symbol.fromString("cond");
        private readonly Symbol consSymbol = Symbol.fromString("cons");
        private readonly Symbol conspSymbol = Symbol.fromString("pair?");
        private readonly Symbol carSymbol = Symbol.fromString("car");
        private readonly Symbol cdrSymbol = Symbol.fromString("cdr");
        private readonly Symbol nilSymbol = Symbol.fromString("nil");
        private readonly Symbol numericEqualSymbol = Symbol.fromString("=");
        private readonly Symbol eqSymbol = Symbol.fromString("eq?");
        private readonly Symbol nullSymbol = Symbol.fromString("null?");
        private readonly Symbol plusSymbol = Symbol.fromString("+");
        private readonly Symbol minusSymbol = Symbol.fromString("-");
        private readonly Symbol starSymbol = Symbol.fromString("*");
        private readonly Symbol slashSymbol = Symbol.fromString("/");
        private readonly Symbol imodSymbol = Symbol.fromString("sys:imod");
        private readonly Symbol idivSymbol = Symbol.fromString("quotient");
        private readonly Symbol ltSymbol = Symbol.fromString("<");
        private readonly Symbol gtSymbol = Symbol.fromString(">");
        private readonly Symbol leSymbol = Symbol.fromString("<=");
        private readonly Symbol geSymbol = Symbol.fromString(">=");
        private readonly Symbol displaySymbol = Symbol.fromString("display");
        private readonly Symbol randomSymbol = Symbol.fromString("random");
        private readonly Symbol numberpSymbol = Symbol.fromString("number?");
        private readonly Symbol stringpSymbol = Symbol.fromString("string?");
        private readonly Symbol stringeqSymbol = Symbol.fromString("string=?");
        private readonly Symbol stringeqciSymbol = Symbol.fromString("string-ci=?");
        private readonly Symbol stringltSymbol = Symbol.fromString("string<?");
        private readonly Symbol stringltciSymbol = Symbol.fromString("string-ci<?");
        private readonly Symbol stringgtSymbol = Symbol.fromString("string>?");
        private readonly Symbol stringgtciSymbol = Symbol.fromString("string-ci>?");
        private readonly Symbol stringlengthSymbol = Symbol.fromString("string-length");
        private readonly Symbol substringSymbol = Symbol.fromString("substring");
        private readonly Symbol stringappendSymbol = Symbol.fromString("string-append");
        private readonly string[] specialFormSymbols = { "if", "define", "lambda", "quote", "begin", "cond", "set!" };
        private readonly List<Symbol> optimizedFunctionSymbols;
        private readonly Dictionary<Symbol, string> unaryFunctions;
        private readonly Dictionary<Symbol, string> binaryFunctions;

        public static string[] Compile(string source)
        {
            return new Compiler("(define (##compiler__main##) " + Resources.SchemeInitScript + "\n" + source + ") (##compiler__main##)").CompiledSource.ToArray();
        }

        private Compiler(string source)
        {
            optimizedFunctionSymbols = new List<Symbol> { numericEqualSymbol, plusSymbol, minusSymbol, starSymbol, slashSymbol, imodSymbol, idivSymbol,
                leSymbol, ltSymbol, geSymbol, gtSymbol, elseSymbol, consSymbol, conspSymbol, carSymbol, cdrSymbol, randomSymbol, eqSymbol, nullSymbol,
                numberpSymbol, stringpSymbol, stringeqSymbol, stringeqciSymbol, stringltSymbol, stringltciSymbol, stringgtSymbol, stringgtciSymbol,
                stringlengthSymbol, substringSymbol, stringappendSymbol, displaySymbol };

            unaryFunctions = new Dictionary<Symbol, string> { { conspSymbol, "ISPAIR" }, { carSymbol, "PAIR1" }, { cdrSymbol, "PAIR2" }, { displaySymbol, "PRINT" },
                { randomSymbol, "RANDOM" }, { nullSymbol, "ISNULL" },  { numberpSymbol, "ISNUMBER" }, { stringpSymbol, "ISSTRING" }, { stringlengthSymbol, "STRLEN" }};

            binaryFunctions = new Dictionary<Symbol, string> { { numericEqualSymbol, "NUMEQUAL" }, { plusSymbol, "ADD" }, { minusSymbol, "SUB" }, { starSymbol, "MUL" },
                { slashSymbol, "DIV" }, { imodSymbol, "IMOD" }, { idivSymbol, "IDIV" }, { ltSymbol, "NUMLT" }, { leSymbol, "NUMLE" }, { gtSymbol, "NUMGT" }, { geSymbol, "NUMGE" },
                { consSymbol, "MAKEPAIR" }, { eqSymbol, "OBJEQUAL" }, { stringeqSymbol, "STREQUAL" }, { stringeqciSymbol, "STREQUALCI" }, { stringltSymbol, "STRLT" },
                { stringltciSymbol, "STRLTCI" }, { stringgtSymbol, "STRGT" }, { stringgtciSymbol, "STRGTCI" }, { stringappendSymbol, "STRAPPEND" } };

            var readSource = new Reader().ReadAll(source).ToList();
            CompileBlock(readSource, false);
            Emit("END");
        }

        private void CompileStatement(object o, bool tailCall)
        {
            if (o is bool) Emit((bool)o ? "PUSHTRUE" : "PUSHFALSE");
            else if (o is int) Emit("PUSHINT " + (int)o);
            else if (o is double) Emit("PUSHDBL " + ((double)o).ToString(CultureInfo.InvariantCulture));
            else if (o is string) Emit("PUSHSTR \"" + StringObject.Escape((string)o) + "\"");
            else if (nilSymbol.Equals(o)) Emit("PUSHNIL");
            else if (o is Symbol) Emit("PUSHVAR " + o);
            else if (o is List<object>) CompileList((List<object>)o, tailCall);
            else throw new CompilerException("Internal error: I don't know how to compile object of type " + o.GetType());
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

            if (firstValue is Symbol && unaryFunctions.ContainsKey((Symbol)firstValue))
            {
                CompileUnaryOperation(value, unaryFunctions[(Symbol)firstValue]);
                return;
            }

            if (firstValue is Symbol && binaryFunctions.ContainsKey((Symbol)firstValue))
            {
                CompileBinaryOperation(value, binaryFunctions[(Symbol)firstValue]);
                return;
            }

            if (defineSymbol.Equals(firstValue)) CompileDefine(value);
            else if (setSymbol.Equals(firstValue)) CompileSet(value);
            else if (quoteSymbol.Equals(firstValue)) CompileQuote(value);
            else if (ifSymbol.Equals(firstValue)) CompileIf(value, tailCall);
            else if (condSymbol.Equals(firstValue)) CompileCond(value, tailCall);
            else if (substringSymbol.Equals(firstValue)) CompileSubstring(value);
            else CompileFunctionCall(value, tailCall);
        }

        private void CompileDefine(List<object> value)
        {
            if (value[1] is List<object>) CompileFunctionDefinition(value, (List<object>)value[1], value.Skip(2).ToList());
            else CompileVariableDefinition(value);
        }

        private void CompileSubstring(List<object> value)
        {
            AssertParameterCount(value.Count - 1, 3, "substr");
            CompileStatement(value[1], false);
            CompileStatement(value[2], false);
            CompileStatement(value[3], false);
            Emit("SUBSTR");
        }

        private void CompileFunctionDefinition(IEnumerable<object> value, List<object> functionNameAndParameters, List<object> body)
        {
            AssertAllFunctionParametersAreSymbols(functionNameAndParameters);

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
            List<string> freeVariables = FindFreeVariablesInLambda(parameters, body, defines).ToList();
            foreach (string i in defines) freeVariables.Remove(i);
            freeVariables.Remove(restParameter);

            string functionLine = "FUNCTION " + name + " " + string.Join(" ", parameters);
            if (hasRestParameter) functionLine += " &rest " + restParameter;
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
                if (list.Count == 0) return;
                if (defineSymbol.Equals(list[0]) && list[1] is List<object>) // define function
                {
                    List<object> nameAndParameters = (List<object>) list[1];
                    AssertAllFunctionParametersAreSymbols(nameAndParameters);
                    string name = nameAndParameters[0].ToString();
                    definedVariables.Add(name);
                    var parameters = nameAndParameters.Skip(1).ToList();
                    foreach (var i in FindFreeVariablesInLambda(parameters.Select(i => i.ToString()), list.Skip(2), new HashSet<string>()))
                        if (!definedVariables.Contains(i)) accessedVariables.Add(i);
                }
                else if (defineSymbol.Equals(list[0]) && list[1] is Symbol) // define variable
                {
                    definedVariables.Add(list[1].ToString());
                }
                else if (quoteSymbol.Equals(list[0]))
                {
                    // Ignore quoted stuff
                }
                else // Function call TODO: Lambda
                {
                    // Special handling for first parameter: +, -, *, /, =...
                    bool first = true;
                    foreach (object i in list)
                    {
                        if (!(first && optimizedFunctionSymbols.Contains(i)))
                            FindAccessedVariables(i, accessedVariables, definedVariables);
                        first = false;
                    }
                }
            }
            else if (o is Symbol)
            {
                string symbol = o.ToString();
                if (!specialFormSymbols.Contains(symbol) && !definedVariables.Contains(symbol))
                    accessedVariables.Add(symbol);
            }
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
            CompileQuotedValue(value[1]);
        }

        private void CompileQuotedValue(object o)
        {
            if (o is bool) Emit((bool)o ? "PUSHTRUE" : "PUSHFALSE");
            else if (o is int) Emit("PUSHINT " + (int)o);
            else if (o is double) Emit("PUSHDBL " + ((double)o).ToString(CultureInfo.InvariantCulture));
            else if (o is string) Emit("PUSHSTR \"" + StringObject.Escape((string)o) + "\"");
            else if (nilSymbol.Equals(o)) Emit("PUSHNIL");
            else if (o is Symbol) Emit("PUSHSYM " + o);
            else if (o is List<object>) CompileQuotedList((List<object>)o);
            else throw new CompilerException("TODO: Quoting " + o.GetType());
        }

        private void CompileQuotedList(List<object> value)
        {
            Emit("PUSHVAR list");
            foreach (object o in value) CompileQuotedValue(o);
            Emit("CALL " + value.Count);
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
            if (expected != got) throw new CompilerException(function + ": Expected " + expected + " parameter(s), got " + got);
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
