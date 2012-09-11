using System.Collections.Generic;
using System.Linq;
using org.lb.lbvm.exceptions;
using org.lb.lbvm.runtime;

// ReSharper disable UnusedParameter.Global

namespace org.lb.lbvm.scheme
{
    internal static class CodeInspection
    {
        public static void AssertAllFunctionParametersAreSymbols(IEnumerable<object> parameters)
        {
            if (parameters == null) throw new CompilerException("Internal error in function definition: Parameter names == null");
            if (!parameters.All(i => i is Symbol))
                throw new CompilerException("Syntax error in function definition: Not all parameter names are symbols");
        }

        // HACK: HashSet parameter is ugly

        public static IEnumerable<string> FindFreeVariablesInLambda(IEnumerable<string> parameters, IEnumerable<object> body, HashSet<string> localVariablesDefinedInLambda)
        {
            HashSet<string> accessedVariables = new HashSet<string>();
            foreach (object o in body) FindAccessedVariables(o, accessedVariables, localVariablesDefinedInLambda);
            accessedVariables.Remove("nil");
            foreach (string p in parameters) accessedVariables.Remove(p);
            return accessedVariables;
        }

        private static void FindAccessedVariables(object o, HashSet<string> accessedVariables, HashSet<string> definedVariables)
        {
            if (o is List<object>)
            {
                var list = (List<object>)o;
                if (list.Count == 0) return;

                if (Symbols.DefineSymbol.Equals(list[0]) && list[1] is List<object>) HandleFunctionDefinition(accessedVariables, definedVariables, list);
                else if (Symbols.LambdaSymbol.Equals(list[0]) && list[1] is List<object>) HandleLambda(accessedVariables, definedVariables, list);
                else if (Symbols.DefineSymbol.Equals(list[0]) && list[1] is Symbol) HandleVariableDefinition(accessedVariables, definedVariables, list);
                else if (Symbols.QuoteSymbol.Equals(list[0])) HandleQuote(accessedVariables, list);
                else HandleFunctionCall(accessedVariables, definedVariables, list);
            }
            else if (o is Symbol)
            {
                string symbol = o.ToString();
                if (!Symbols.IsSpecialFormSymbol(symbol) && !definedVariables.Contains(symbol))
                    accessedVariables.Add(symbol);
            }
        }

        private static void HandleFunctionDefinition(HashSet<string> accessedVariables, HashSet<string> definedVariables, List<object> list)
        {
            List<object> nameAndParameters = (List<object>)list[1];
            AssertAllFunctionParametersAreSymbols(nameAndParameters);
            string name = nameAndParameters[0].ToString();
            definedVariables.Add(name);
            var parameters = nameAndParameters.Skip(1).ToList();
            foreach (var i in FindFreeVariablesInLambda(parameters.Select(i => i.ToString()), list.Skip(2), new HashSet<string>()))
                if (!definedVariables.Contains(i)) accessedVariables.Add(i);
        }

        private static void HandleLambda(HashSet<string> accessedVariables, HashSet<string> definedVariables, List<object> list)
        {
            List<object> parameters = (List<object>)list[1];
            AssertAllFunctionParametersAreSymbols(parameters);
            foreach (var i in FindFreeVariablesInLambda(parameters.Select(i => i.ToString()), list.Skip(2), new HashSet<string>()))
                if (!definedVariables.Contains(i)) accessedVariables.Add(i);
        }

        private static void HandleVariableDefinition(HashSet<string> accessedVariables, HashSet<string> definedVariables, List<object> list)
        {
            definedVariables.Add(list[1].ToString());
            FindAccessedVariables(list[2], accessedVariables, definedVariables);
        }

        private static void HandleQuote(HashSet<string> accessedVariables, List<object> list)
        {
            if (list[1] is List<object>)
                accessedVariables.Add("list");
        }

        private static void HandleFunctionCall(HashSet<string> accessedVariables, HashSet<string> definedVariables, IEnumerable<object> list)
        {
            // Special handling for first parameter: +, -, *, /, =...
            bool first = true;
            foreach (object i in list)
            {
                if (!(first && Symbols.IsOptimizedFunctionSymbol(i)))
                    FindAccessedVariables(i, accessedVariables, definedVariables);
                first = false;
            }
        }
    }
}

// ReSharper restore UnusedParameter.Global
