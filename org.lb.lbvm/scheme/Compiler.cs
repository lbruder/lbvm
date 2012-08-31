﻿using System;
using System.Collections.Generic;
using System.Globalization;
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

    // TODO: lambda, quote, begin
    // TODO: cond, or, and / macro system

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
        private readonly Symbol ltSymbol = new Symbol("<");
        private readonly Symbol gtSymbol = new Symbol(">");
        private readonly Symbol leSymbol = new Symbol("<=");
        private readonly Symbol geSymbol = new Symbol(">=");
        private readonly string[] specialFormSymbols = { "if", "define", "lambda", "quote", "begin" };
        private readonly List<Symbol> optimizedSymbols;

        public static string[] Compile(string source)
        {
            return new Compiler(source).CompiledSource.ToArray();
        }

        private Compiler(string source)
        {
            optimizedSymbols = new List<Symbol> { numericEqualSymbol, plusSymbol, minusSymbol, starSymbol, slashSymbol, leSymbol, ltSymbol, geSymbol, gtSymbol };
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
            if (o is bool) Emit((bool)o ? "PUSHTRUE" : "PUSHFALSE");
            else if (o is int) Emit("PUSHINT " + (int)o);
            else if (o is double) Emit("PUSHDBL " + ((double)o).ToString(CultureInfo.InvariantCulture));
            else if (o is string) Emit("PUSHSTR \"" + EscapeString((string)o) + "\"");
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
            if (value.Count > 1 && defineSymbol.Equals(value[0])) CompileDefine(value);
            else if (value.Count == 4 && ifSymbol.Equals(value[0])) CompileIf(value, tailCall);
            else if (value.Count == 3 && numericEqualSymbol.Equals(value[0])) CompileNumericOperation(value, "NUMEQUAL");
            else if (value.Count == 3 && plusSymbol.Equals(value[0])) CompileNumericOperation(value, "ADD");
            else if (value.Count == 3 && minusSymbol.Equals(value[0])) CompileNumericOperation(value, "SUB");
            else if (value.Count == 3 && starSymbol.Equals(value[0])) CompileNumericOperation(value, "MUL");
            else if (value.Count == 3 && slashSymbol.Equals(value[0])) CompileNumericOperation(value, "DIV");
            else if (value.Count == 3 && ltSymbol.Equals(value[0])) CompileNumericOperation(value, "NUMLT");
            else if (value.Count == 3 && leSymbol.Equals(value[0])) CompileNumericOperation(value, "NUMLE");
            else if (value.Count == 3 && gtSymbol.Equals(value[0])) CompileNumericOperation(value, "NUMGT");
            else if (value.Count == 3 && geSymbol.Equals(value[0])) CompileNumericOperation(value, "NUMGE");
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
            Emit("PUSHVAR " + name);
        }

        private static void AssertAllFunctionParametersAreSymbols(IEnumerable<object> parameters)
        {
            if (!parameters.All(i => i is Symbol))
                throw new CompilerException("Syntax error in function definition: Not all parameter names are symbols");
        }

        private IEnumerable<string> FindFreeVariablesInLambda(IEnumerable<string> parameters, IEnumerable<object> body)
        {
            HashSet<string> accessedVariables = new HashSet<string>();
            HashSet<string> definedVariables = new HashSet<string>();
            foreach (object o in body) FindAccessedVariables(o, accessedVariables, definedVariables);
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
                    foreach (var i in FindFreeVariablesInLambda(parameters.Select(i => ((Symbol)i).Name), list.Skip(2)))
                        if (!definedVariables.Contains(i)) accessedVariables.Add(i);
                }
                else // Function call TODO: Lambda
                {
                    // Special handling for first parameter: +, -, *, /, =...
                    bool first = true;
                    foreach (object i in list)
                    {
                        if (first && optimizedSymbols.Contains(i)) first = false;
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
