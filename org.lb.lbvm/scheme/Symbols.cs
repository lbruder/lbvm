using System.Linq;
using org.lb.lbvm.runtime;

namespace org.lb.lbvm.scheme
{
    // TODO:
    //boolean?
    //symbol?
    //vector?
    //procedure?

    internal sealed class FunctionDefinition
    {
        public readonly int Arity;
        public readonly string Name;
        public readonly string Opcode;
        public FunctionDefinition(int arity, string name, string opcode)
        {
            Arity = arity;
            Name = name;
            Opcode = opcode;
        }
    }

    internal static class Symbols
    {
        internal static readonly Symbol IfSymbol = Symbol.fromString("if");
        internal static readonly Symbol DefineSymbol = Symbol.fromString("define");
        internal static readonly Symbol LambdaSymbol = Symbol.fromString("lambda");
        internal static readonly Symbol QuoteSymbol = Symbol.fromString("quote");
        internal static readonly Symbol BeginSymbol = Symbol.fromString("begin");
        internal static readonly Symbol CondSymbol = Symbol.fromString("cond");
        internal static readonly Symbol SetSymbol = Symbol.fromString("set!");

        internal static readonly Symbol ElseSymbol = Symbol.fromString("else");
        internal static readonly Symbol NilSymbol = Symbol.fromString("nil");

        private static readonly string[] specialFormSymbols = { "if", "define", "lambda", "quote", "begin", "cond", "set!" };

        private static readonly FunctionDefinition[] functions =
        {
            new FunctionDefinition(1, "pair?", "ISPAIR"),
            new FunctionDefinition(1, "car", "PAIR1"),
            new FunctionDefinition(1, "cdr", "PAIR2"),
            new FunctionDefinition(1, "display", "PRINT"),
            new FunctionDefinition(1, "random", "RANDOM"),
            new FunctionDefinition(1, "number?", "ISNUMBER"),
            new FunctionDefinition(1, "string?", "ISSTRING"),
            new FunctionDefinition(1, "string-length", "STRLEN"),
            new FunctionDefinition(1, "char?", "ISCHAR"),
            new FunctionDefinition(1, "char->integer", "CHRTOINT"),
            new FunctionDefinition(1, "integer->char", "INTTOCHR"),
            new FunctionDefinition(1, "sys:make-string", "MAKESTR"),
            new FunctionDefinition(1, "string->symbol", "STRTOSYM"),
            new FunctionDefinition(1, "symbol->string", "SYMTOSTR"),
            new FunctionDefinition(1, "null?", "ISNULL"),

            new FunctionDefinition(2, "=", "NUMEQUAL"),
            new FunctionDefinition(2, "+", "ADD"),
            new FunctionDefinition(2, "-", "SUB"),
            new FunctionDefinition(2, "*", "MUL"),
            new FunctionDefinition(2, "/", "DIV"),
            new FunctionDefinition(2, "sys:imod", "IMOD"),
            new FunctionDefinition(2, "quotient", "IDIV"),
            new FunctionDefinition(2, "<", "NUMLT"),
            new FunctionDefinition(2, "<=", "NUMLE"),
            new FunctionDefinition(2, ">", "NUMGT"),
            new FunctionDefinition(2, ">=", "NUMGE"),
            new FunctionDefinition(2, "cons", "MAKEPAIR"),
            new FunctionDefinition(2, "eq?", "OBJEQUAL"),
            new FunctionDefinition(2, "string=?", "STREQUAL"),
            new FunctionDefinition(2, "string-ci=?", "STREQUALCI"),
            new FunctionDefinition(2, "string<?", "STRLT"),
            new FunctionDefinition(2, "string-ci<?", "STRLTCI"),
            new FunctionDefinition(2, "string>?", "STRGT"),
            new FunctionDefinition(2, "string-ci>?", "STRGTCI"),
            new FunctionDefinition(2, "string-append", "STRAPPEND"),
            new FunctionDefinition(2, "char=?", "CHREQUAL"),
            new FunctionDefinition(2, "char-ci=?", "CHREQUALCI"),
            new FunctionDefinition(2, "char<?", "CHRLT"),
            new FunctionDefinition(2, "char-ci<?", "CHRLTCI"),
            new FunctionDefinition(2, "char>?", "CHRGT"),
            new FunctionDefinition(2, "char-ci>?", "CHRGTCI"),
            new FunctionDefinition(2, "string-ref", "STRREF"),
            new FunctionDefinition(2, "sys:strtonum", "STRTONUM"),
            new FunctionDefinition(2, "sys:numtostr", "NUMTOSTR"),
        
            new FunctionDefinition(3, "substring", "SUBSTR"),
            new FunctionDefinition(3, "string-set!", "SETSTRREF")
        };

        internal static FunctionDefinition GetFunction(string name)
        {
            return functions.FirstOrDefault(i => i.Name == name);
        }

        internal static bool IsSpecialFormSymbol(string symbol)
        {
            return specialFormSymbols.Contains(symbol);
        }

        internal static bool IsOptimizedFunctionSymbol(object symbol)
        {
            if (!(symbol is Symbol)) return false;
            string name = symbol.ToString();
            if (name == "else") return true;
            if (name == "nil") return true;
            return GetFunction(name) != null;
        }
    }
}