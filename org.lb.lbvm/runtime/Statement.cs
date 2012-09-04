using System;
using System.Collections.Generic;
using System.Globalization;

namespace org.lb.lbvm.runtime
{
    public abstract class Statement
    {
        public readonly int Length;
        private readonly string Disassembled;
        public override string ToString() { return Disassembled; }
        internal abstract void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack);
        protected Statement(int length, string disassembled)
        {
            Length = length;
            Disassembled = disassembled;
        }
    }

    public abstract class BinaryStatement : Statement
    {
        internal BinaryStatement(string opcode) : base(1, opcode) { }
        protected abstract object operation(object tos, object under_tos);
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object tos = valueStack.Pop();
            object under_tos = valueStack.Pop();
            valueStack.Push(operation(tos, under_tos));
            ip += Length;
        }
    }

    public sealed class EndStatement : Statement
    {
        internal EndStatement() : base(1, "END") { }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack) { }
    }

    public sealed class PopStatement : Statement
    {
        internal PopStatement() : base(1, "POP") { }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Pop();
            ip += Length;
        }
    }

    public sealed class PushintStatement : Statement
    {
        internal PushintStatement(int number) : base(5, "PUSHINT " + number) { this.Number = number; }
        private readonly int Number;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(Number);
            ip += Length;
        }
    }

    public sealed class DefineStatement : Statement
    {
        internal DefineStatement(int symbolNumber, string symbol) : base(5, "DEFINE " + symbol) { this.SymbolNumber = symbolNumber; this.Symbol = symbol; }
        private readonly int SymbolNumber;
        private readonly string Symbol;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o = valueStack.Pop();
            var env = envStack.Peek();
            if (o is Variable) env.Set(SymbolNumber, (Variable)o); // Link to variable, e.g. in Closure
            else
            {
                if (env.HasVariable(SymbolNumber))
                    env.Get(SymbolNumber, Symbol).SetValue(o);
                else
                    env.Set(SymbolNumber, new Variable(o));
            }
            ip += Length;
        }
    }

    public sealed class PushvarStatement : Statement
    {
        internal PushvarStatement(int symbolNumber, string symbol) : base(5, "PUSHVAR " + symbol) { this.SymbolNumber = symbolNumber; this.Symbol = symbol; }
        private readonly int SymbolNumber;
        private readonly string Symbol;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(envStack.Peek().Get(SymbolNumber, Symbol).GetValue());
            ip += Length;
        }
    }

    public sealed class NumeqStatement : BinaryStatement
    {
        internal NumeqStatement() : base("NUMEQ") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is int) && (under_tos is int)) return (int)under_tos == (int)tos;
            return Convert.ToDouble(under_tos) == Convert.ToDouble(tos);
        }
    }

    public sealed class AddStatement : BinaryStatement
    {
        internal AddStatement() : base("ADD") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is int) && (under_tos is int)) return (int)under_tos + (int)tos;
            return Convert.ToDouble(under_tos) + Convert.ToDouble(tos);
        }
    }

    public sealed class SubStatement : BinaryStatement
    {
        internal SubStatement() : base("SUB") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is int) && (under_tos is int)) return (int)under_tos - (int)tos;
            return Convert.ToDouble(under_tos) - Convert.ToDouble(tos);
        }
    }

    public sealed class MulStatement : BinaryStatement
    {
        internal MulStatement() : base("MUL") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is int) && (under_tos is int)) return (int)under_tos * (int)tos;
            return Convert.ToDouble(under_tos) * Convert.ToDouble(tos);
        }
    }

    public sealed class DivStatement : BinaryStatement
    {
        internal DivStatement() : base("DIV") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is int) && (under_tos is int)) return (int)under_tos / (int)tos;
            return Convert.ToDouble(under_tos) / Convert.ToDouble(tos);
        }
    }

    public sealed class IdivStatement : BinaryStatement
    {
        internal IdivStatement() : base("IDIV") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is int) && (under_tos is int)) return (int)under_tos / (int)tos;
            return (int)Convert.ToDouble(under_tos) / (int)Convert.ToDouble(tos);
        }
    }

    public sealed class ImodStatement : BinaryStatement
    {
        internal ImodStatement() : base("IMOD") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is int) && (under_tos is int)) return (int)under_tos % (int)tos;
            return (int)Convert.ToDouble(under_tos) % (int)Convert.ToDouble(tos);
        }
    }

    public sealed class BfalseStatement : Statement
    {
        internal BfalseStatement(int target) : base(5, "BFALSE 0x" + target.ToString("x4")) { this.Target = target; }
        private readonly int Target;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o = valueStack.Pop();
            if (o is bool && (bool)o == false) ip = Target;
            else ip += Length;
        }
    }

    public sealed class EnterStatement : Statement
    {
        internal EnterStatement(int numberOfParameters, string symbol) : base(9, "ENTER " + numberOfParameters + " " + symbol) { this.NumberOfParameters = numberOfParameters; this.Symbol = symbol; }
        private readonly int NumberOfParameters;
        private readonly string Symbol;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            if (callStack.Peek().NumberOfParameters != NumberOfParameters)
                throw new RuntimeException(Symbol + ": Invalid parameter count");
            envStack.Push(new Environment());
            ip += Length;
        }
    }

    public sealed class EnterRestStatement : Statement
    {
        internal EnterRestStatement(int numberOfParameters, int numberOfParametersToSkip, string symbol) : base(13, "ENTERR " + numberOfParameters + " " + numberOfParametersToSkip + " " + symbol) { this.NumberOfParameters = numberOfParameters; this.NumberOfParametersToSkip = numberOfParametersToSkip; this.Symbol = symbol; }
        private readonly int NumberOfParameters;
        private readonly int NumberOfParametersToSkip;
        private readonly string Symbol;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            int provided = callStack.Peek().NumberOfParameters;
            if (provided < NumberOfParameters - 1) // -1 as there may be an empty list as rest parameter
                throw new RuntimeException(Symbol + ": Invalid parameter count");

            object restParameter = Nil.GetInstance();
            var skipStack = new Stack<object>();
            for (int i = 0; i < NumberOfParametersToSkip; ++i) skipStack.Push(valueStack.Pop());
            int numberOfValuesForRestParameter = 1 + provided - NumberOfParameters;
            for (int i = 0; i < numberOfValuesForRestParameter; ++i) restParameter = new Pair(valueStack.Pop(), restParameter);
            valueStack.Push(restParameter);
            for (int i = 0; i < NumberOfParametersToSkip; ++i) valueStack.Push(skipStack.Pop());

            envStack.Push(new Environment());
            ip += Length;
        }
    }

    // ReSharper disable RedundantAssignment
    public sealed class RetStatement : Statement
    {
        internal RetStatement() : base(1, "RET") { }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            envStack.Pop();
            ip = callStack.Pop().Ip;
        }
    }
    // ReSharper restore RedundantAssignment

    public sealed class CallStatement : Statement
    {
        internal CallStatement(int numberOfPushedArguments) : base(5, "CALL " + numberOfPushedArguments) { this.NumberOfPushedArguments = numberOfPushedArguments; }
        private readonly int NumberOfPushedArguments;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            // HACK: Jump to valueStack[tos - NumberOfPushedArguments]
            var vs = valueStack.ToArray();
            object target = vs[NumberOfPushedArguments];
            if (target is IP)
            {
                callStack.Push(new Call(ip + Length, NumberOfPushedArguments));
                ip = ((IP)target).Value;
                return;
            }
            if (target is Closure)
            {
                Closure c = (Closure)target;
                foreach (var value in c.ClosedOverValues) valueStack.Push(value);
                callStack.Push(new Call(ip + Length, NumberOfPushedArguments + c.ClosedOverValues.Count));
                ip = c.Target;
                return;
            }
            throw new RuntimeException("Invalid CALL target");
        }
    }

    public sealed class TailcallStatement : Statement
    {
        internal TailcallStatement(int numberOfPushedArguments) : base(5, "TAILCALL " + numberOfPushedArguments) { this.NumberOfPushedArguments = numberOfPushedArguments; }
        private readonly int NumberOfPushedArguments;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            envStack.Pop();
            int oldIp = callStack.Pop().Ip;

            // HACK: Jump to valueStack[tos - NumberOfPushedArguments]
            var vs = valueStack.ToArray();
            object target = vs[NumberOfPushedArguments];
            if (target is IP)
            {
                callStack.Push(new Call(oldIp, NumberOfPushedArguments));
                ip = ((IP)target).Value;
                return;
            }
            if (target is Closure)
            {
                Closure c = (Closure)target;
                foreach (var value in c.ClosedOverValues) valueStack.Push(value);
                callStack.Push(new Call(oldIp, NumberOfPushedArguments + c.ClosedOverValues.Count));
                ip = c.Target;
                return;
            }
            throw new RuntimeException("Invalid CALL target");
        }
    }

    // ReSharper disable RedundantAssignment
    public sealed class JmpStatement : Statement
    {
        internal JmpStatement(int target) : base(5, "JMP 0x" + target.ToString("x4")) { this.Target = target; }
        private readonly int Target;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            ip = Target;
        }
    }
    // ReSharper restore RedundantAssignment

    public sealed class PushlabelStatement : Statement
    {
        internal PushlabelStatement(int number) : base(5, "PUSHLABEL 0x" + number.ToString("x4")) { this.Number = number; }
        private readonly int Number;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(new IP(Number));
            ip += Length;
        }
    }

    public sealed class SetStatement : Statement
    {
        internal SetStatement(int symbolNumber, string symbol) : base(5, "SET " + symbol) { this.SymbolNumber = symbolNumber; this.Symbol = symbol; }
        private readonly int SymbolNumber;
        private readonly string Symbol;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o = valueStack.Pop();
            envStack.Peek().Get(SymbolNumber, Symbol).SetValue(o);
            ip += Length;
        }
    }

    public sealed class PushsymStatement : Statement
    {
        internal PushsymStatement(int symbolNumber, string symbol) : base(5, "PUSHSYM " + symbol) { this.Symbol = new Symbol(symbolNumber, symbol); }
        private readonly Symbol Symbol;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(Symbol);
            ip += Length;
        }
    }

    public sealed class PushboolStatement : Statement
    {
        internal PushboolStatement(bool value) : base(1, "PUSH" + (value ? "TRUE" : "FALSE")) { this.Value = value; }
        private readonly bool Value;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(Value);
            ip += Length;
        }
    }

    public sealed class MakeClosureStatement : Statement
    {
        internal MakeClosureStatement(int numberOfPushedArguments) : base(5, "MAKECLOSURE " + numberOfPushedArguments) { this.NumberOfPushedArguments = numberOfPushedArguments; }
        private readonly int NumberOfPushedArguments;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            List<Variable> values = new List<Variable>();

            for (int i = 0; i < NumberOfPushedArguments; ++i)
            {
                Symbol symbol = (Symbol)valueStack.Pop();
                values.Add(envStack.Peek().Get(symbol.Number, symbol.Name));
            }
            values.Reverse();
            valueStack.Push(new Closure((IP)valueStack.Pop(), values));
            ip += Length;
        }
    }

    public sealed class NumltStatement : BinaryStatement
    {
        internal NumltStatement() : base("NUMLT") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is int) && (under_tos is int)) return (int)under_tos < (int)tos;
            return Convert.ToDouble(under_tos) < Convert.ToDouble(tos);
        }
    }

    public sealed class NumleStatement : BinaryStatement
    {
        internal NumleStatement() : base("NUMLE") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is int) && (under_tos is int)) return (int)under_tos <= (int)tos;
            return Convert.ToDouble(under_tos) <= Convert.ToDouble(tos);
        }
    }

    public sealed class NumgtStatement : BinaryStatement
    {
        internal NumgtStatement() : base("NUMGT") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is int) && (under_tos is int)) return (int)under_tos > (int)tos;
            return Convert.ToDouble(under_tos) > Convert.ToDouble(tos);
        }
    }

    public sealed class NumgeStatement : BinaryStatement
    {
        internal NumgeStatement() : base("NUMGE") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is int) && (under_tos is int)) return (int)under_tos >= (int)tos;
            return Convert.ToDouble(under_tos) >= Convert.ToDouble(tos);
        }
    }

    public sealed class PushdblStatement : Statement
    {
        internal PushdblStatement(double number) : base(9, "PUSHDBL " + number.ToString(CultureInfo.InvariantCulture)) { this.Number = number; }
        private readonly double Number;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(Number);
            ip += Length;
        }
    }

    public sealed class MakevarStatement : Statement
    {
        internal MakevarStatement(int symbolNumber, string symbol) : base(5, "MAKEVAR " + symbol) { this.SymbolNumber = symbolNumber; }
        private readonly int SymbolNumber;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            envStack.Peek().Set(SymbolNumber, new Variable());
            ip += Length;
        }
    }

    public sealed class MakepairStatement : BinaryStatement
    {
        internal MakepairStatement() : base("MAKEPAIR") { }
        protected override object operation(object tos, object under_tos)
        {
            return new Pair(under_tos, tos);
        }
    }

    public sealed class IspairStatement : Statement
    {
        internal IspairStatement() : base(1, "ISPAIR") { }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(valueStack.Pop() is Pair);
            ip += Length;
        }
    }

    public sealed class Pair1Statement : Statement
    {
        internal Pair1Statement() : base(1, "PAIR1") { }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(((Pair)valueStack.Pop()).First);
            ip += Length;
        }
    }

    public sealed class Pair2Statement : Statement
    {
        internal Pair2Statement() : base(1, "PAIR2") { }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(((Pair)valueStack.Pop()).Second);
            ip += Length;
        }
    }

    public sealed class PushnilStatement : Statement
    {
        internal PushnilStatement() : base(1, "PUSHNIL") { }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(Nil.GetInstance());
            ip += Length;
        }
    }

    public sealed class RandomStatement : Statement
    {
        private static readonly Random random = new Random();
        internal RandomStatement() : base(1, "RANDOM") { }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o = valueStack.Pop();
            valueStack.Push(random.Next(Convert.ToInt32(o)));
            ip += Length;
        }
    }

    public sealed class ObjequalStatement : BinaryStatement
    {
        internal ObjequalStatement() : base("OBJEQUAL") { }
        protected override object operation(object tos, object under_tos)
        {
            return tos == under_tos || tos.Equals(under_tos);
        }
    }

    public sealed class IsnullStatement : Statement
    {
        internal IsnullStatement() : base(1, "ISNULL") { }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(valueStack.Pop() is Nil);
            ip += Length;
        }
    }

    public sealed class PrintStatement : Statement
    {
        private readonly InputOutputChannel printer;
        internal PrintStatement(InputOutputChannel printer) : base(1, "PRINT") { this.printer = printer; }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o = valueStack.Peek();
            if (o is bool) printer.Print(((bool)o) ? "#t" : "#f");
            else printer.Print(string.Format(CultureInfo.InvariantCulture, "{0}", o));
            ip += Length;
        }
    }

    public sealed class PushstrStatement : Statement
    {
        internal PushstrStatement(string value) : base(5+value.Length, "PUSHSTR \"" + Assembler.EscapeString(value) + "\"") { this.value = value; }
        private readonly string value;
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(value);
            ip += Length;
        }
    }

    public sealed class ErrorStatement : Statement
    {
        internal ErrorStatement() : base(1, "ERROR") { }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            throw new RuntimeException("Error in program: Jump into the middle of a statement");
        }
    }
}
