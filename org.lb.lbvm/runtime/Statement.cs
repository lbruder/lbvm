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
        internal abstract void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack);
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
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
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
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack) { }
    }

    public sealed class PopStatement : Statement
    {
        internal PopStatement() : base(1, "POP") { }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Pop();
            ip += Length;
        }
    }

    public sealed class PushintStatement : Statement
    {
        internal PushintStatement(int number) : base(5, "PUSHINT " + number) { this.Number = number; }
        private readonly int Number;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(Number);
            ip += Length;
        }
    }

    public sealed class DefineStatement : Statement
    {
        internal DefineStatement(Symbol symbol) : base(5, "DEFINE " + symbol) { this.Symbol = symbol; }
        private readonly Symbol Symbol;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            object o = valueStack.Pop();
            if (o is Variable) envStack.Set(Symbol, (Variable)o); // Link to variable, e.g. in Closure
            else
            {
                if (envStack.HasVariable(Symbol))
                    envStack.Get(Symbol).SetValue(o);
                else
                    envStack.Set(Symbol, new Variable(o));
            }
            ip += Length;
        }
    }

    public sealed class PushvarStatement : Statement
    {
        internal PushvarStatement(Symbol symbol) : base(5, "PUSHVAR " + symbol) { this.Symbol = symbol; }
        private readonly Symbol Symbol;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(envStack.Get(Symbol).GetValue());
            ip += Length;
        }
    }

    public sealed class NumeqStatement : BinaryStatement
    {
        internal NumeqStatement() : base("NUMEQUAL") { }
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
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            object o = valueStack.Pop();
            if (o is bool && (bool)o == false) ip = Target;
            else ip += Length;
        }
    }

    public sealed class EnterStatement : Statement
    {
        internal EnterStatement(int numberOfParameters, Symbol symbol) : base(9, "ENTER " + numberOfParameters + " " + symbol) { this.NumberOfParameters = numberOfParameters; this.Symbol = symbol; }
        private readonly int NumberOfParameters;
        private readonly Symbol Symbol;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            if (callStack.GetLastNumberOfParameters() != NumberOfParameters)
                throw new RuntimeException(Symbol + ": Invalid parameter count");
            envStack.PushNew();
            ip += Length;
        }
    }

    public sealed class EnterRestStatement : Statement
    {
        internal EnterRestStatement(int numberOfParameters, int numberOfParametersToSkip, Symbol symbol) : base(13, "ENTERR " + numberOfParameters + " " + numberOfParametersToSkip + " " + symbol) { this.NumberOfParameters = numberOfParameters; this.NumberOfParametersToSkip = numberOfParametersToSkip; this.Symbol = symbol; }
        private readonly int NumberOfParameters;
        private readonly int NumberOfParametersToSkip;
        private readonly Symbol Symbol;
        private readonly ValueStack skipStack = new ValueStack();
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            int provided = callStack.GetLastNumberOfParameters();
            if (provided < NumberOfParameters - 1) // -1 as there may be an empty list as rest parameter
                throw new RuntimeException(Symbol + ": Invalid parameter count");

            object restParameter = Nil.GetInstance();
            for (int i = 0; i < NumberOfParametersToSkip; ++i) skipStack.Push(valueStack.Pop());
            int numberOfValuesForRestParameter = 1 + provided - NumberOfParameters;
            for (int i = 0; i < numberOfValuesForRestParameter; ++i) restParameter = new Pair(valueStack.Pop(), restParameter);
            valueStack.Push(restParameter);
            for (int i = 0; i < NumberOfParametersToSkip; ++i) valueStack.Push(skipStack.Pop());

            envStack.PushNew();
            ip += Length;
        }
    }

    // ReSharper disable RedundantAssignment
    public sealed class RetStatement : Statement
    {
        internal RetStatement() : base(1, "RET") { }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            envStack.Pop();
            ip = callStack.GetLastIp();
            callStack.Pop();
        }
    }
    // ReSharper restore RedundantAssignment

    public sealed class CallStatement : Statement
    {
        internal CallStatement(int numberOfPushedArguments) : base(5, "CALL " + numberOfPushedArguments) { this.NumberOfPushedArguments = numberOfPushedArguments; }
        private readonly int NumberOfPushedArguments;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            object target = valueStack.GetFromTop(NumberOfPushedArguments);
            if (target is IP)
            {
                callStack.Push(ip + Length, NumberOfPushedArguments);
                ip = ((IP)target).Value;
                return;
            }
            if (target is Closure)
            {
                Closure c = (Closure)target;
                foreach (var value in c.ClosedOverValues) valueStack.Push(value);
                callStack.Push(ip + Length, NumberOfPushedArguments + c.ClosedOverValues.Count);
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
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            envStack.Pop();
            int oldIp = callStack.GetLastIp();
            callStack.Pop();

            object target = valueStack.GetFromTop(NumberOfPushedArguments);
            if (target is IP)
            {
                callStack.Push(oldIp, NumberOfPushedArguments);
                ip = ((IP)target).Value;
                return;
            }
            if (target is Closure)
            {
                Closure c = (Closure)target;
                foreach (var value in c.ClosedOverValues) valueStack.Push(value);
                callStack.Push(oldIp, NumberOfPushedArguments + c.ClosedOverValues.Count);
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
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            ip = Target;
        }
    }
    // ReSharper restore RedundantAssignment

    public sealed class PushlabelStatement : Statement
    {
        internal PushlabelStatement(int number) : base(5, "PUSHLABEL 0x" + number.ToString("x4")) { this.Number = number; }
        private readonly int Number;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(new IP(Number));
            ip += Length;
        }
    }

    public sealed class SetStatement : Statement
    {
        internal SetStatement(Symbol symbol) : base(5, "SET " + symbol) { this.Symbol = symbol; }
        private readonly Symbol Symbol;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            object o = valueStack.Pop();
            envStack.Get(Symbol).SetValue(o);
            ip += Length;
        }
    }

    public sealed class PushsymStatement : Statement
    {
        internal PushsymStatement(Symbol symbol) : base(5, "PUSHSYM " + symbol) { this.Symbol = symbol; }
        private readonly Symbol Symbol;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(Symbol);
            ip += Length;
        }
    }

    public sealed class PushboolStatement : Statement
    {
        internal PushboolStatement(bool value) : base(1, "PUSH" + (value ? "TRUE" : "FALSE")) { this.Value = value; }
        private readonly bool Value;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(Value);
            ip += Length;
        }
    }

    public sealed class MakeClosureStatement : Statement
    {
        internal MakeClosureStatement(int numberOfPushedArguments) : base(5, "MAKECLOSURE " + numberOfPushedArguments) { this.NumberOfPushedArguments = numberOfPushedArguments; }
        private readonly int NumberOfPushedArguments;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            List<Variable> values = new List<Variable>();

            for (int i = 0; i < NumberOfPushedArguments; ++i)
            {
                Symbol symbol = (Symbol)valueStack.Pop();
                values.Add(envStack.Get(symbol));
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
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(Number);
            ip += Length;
        }
    }

    public sealed class MakevarStatement : Statement
    {
        internal MakevarStatement(Symbol symbol) : base(5, "MAKEVAR " + symbol) { this.Symbol = symbol; }
        private readonly Symbol Symbol;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            envStack.Set(Symbol, new Variable());
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
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(valueStack.Pop() is Pair);
            ip += Length;
        }
    }

    public sealed class Pair1Statement : Statement
    {
        internal Pair1Statement() : base(1, "PAIR1") { }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(((Pair)valueStack.Pop()).First);
            ip += Length;
        }
    }

    public sealed class Pair2Statement : Statement
    {
        internal Pair2Statement() : base(1, "PAIR2") { }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(((Pair)valueStack.Pop()).Second);
            ip += Length;
        }
    }

    public sealed class PushnilStatement : Statement
    {
        internal PushnilStatement() : base(1, "PUSHNIL") { }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(Nil.GetInstance());
            ip += Length;
        }
    }

    public sealed class RandomStatement : Statement
    {
        private static readonly Random random = new Random();
        internal RandomStatement() : base(1, "RANDOM") { }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
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
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(valueStack.Pop() is Nil);
            ip += Length;
        }
    }

    public sealed class PrintStatement : Statement
    {
        private readonly InputOutputChannel printer;
        internal PrintStatement(InputOutputChannel printer) : base(1, "PRINT") { this.printer = printer; }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            object o = valueStack.TopOfStack();
            if (o is bool) printer.Print(((bool)o) ? "#t" : "#f");
            else printer.Print(string.Format(CultureInfo.InvariantCulture, "{0}", o));
            ip += Length;
        }
    }

    public sealed class PushstrStatement : Statement
    {
        internal PushstrStatement(string value) : base(5 + value.Length, "PUSHSTR \"" + StringObject.Escape(value) + "\"") { this.value = new StringObject(value); }
        private readonly StringObject value;
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(value);
            ip += Length;
        }
    }

    public sealed class IsnumberStatement : Statement
    {
        internal IsnumberStatement() : base(1, "ISNUMBER") { }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            object o = valueStack.Pop();
            valueStack.Push(o is int || o is double);
            ip += Length;
        }
    }

    public sealed class IsstringStatement : Statement
    {
        internal IsstringStatement() : base(1, "ISSTRING") { }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(valueStack.Pop() is StringObject);
            ip += Length;
        }
    }

    public sealed class StreqStatement : BinaryStatement
    {
        private readonly bool ci;
        internal StreqStatement(bool ci) : base("STREQUAL" + (ci ? "CI" : "")) { this.ci = ci; }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is StringObject) && (under_tos is StringObject)) return ((StringObject)under_tos).Compare(tos, ci) == 0;
            throw new RuntimeException(this + ": Expected two strings as arguments");
        }
    }

    public sealed class StrltStatement : BinaryStatement
    {
        private readonly bool ci;
        internal StrltStatement(bool ci) : base("STRLT" + (ci ? "CI" : "")) { this.ci = ci; }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is StringObject) && (under_tos is StringObject)) return ((StringObject)under_tos).Compare(tos, ci) < 0;
            throw new RuntimeException(this + ": Expected two strings as arguments");
        }
    }

    public sealed class StrgtStatement : BinaryStatement
    {
        private readonly bool ci;
        internal StrgtStatement(bool ci) : base("STRGT" + (ci ? "CI" : "")) { this.ci = ci; }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is StringObject) && (under_tos is StringObject)) return ((StringObject)under_tos).Compare(tos, ci) > 0;
            throw new RuntimeException(this + ": Expected two strings as arguments");
        }
    }

    public sealed class StrlengthStatement : Statement
    {
        internal StrlengthStatement() : base(1, "STRLEN") { }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            valueStack.Push(((StringObject)valueStack.Pop()).Value.Length);
            ip += Length;
        }
    }

    public sealed class SubstrStatement : Statement
    {
        internal SubstrStatement() : base(1, "SUBSTR") { }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            int end = (int)valueStack.Pop();
            int start = (int)valueStack.Pop();
            StringObject str = (StringObject)valueStack.Pop();
            valueStack.Push(new StringObject(str.Value.Substring(start, end-start)));
            ip += Length;
        }
    }

    public sealed class StrappendStatement : BinaryStatement
    {
        internal StrappendStatement() : base("STRAPPEND") { }
        protected override object operation(object tos, object under_tos)
        {
            if ((tos is StringObject) && (under_tos is StringObject)) return new StringObject(((StringObject)under_tos).Value + ((StringObject)tos).Value);
            throw new RuntimeException(this + ": Expected two strings as arguments");
        }
    }

    public sealed class ErrorStatement : Statement
    {
        internal ErrorStatement() : base(1, "ERROR") { }
        internal override void Execute(ref int ip, ValueStack valueStack, EnvironmentStack envStack, CallStack callStack)
        {
            throw new RuntimeException("Error in program: Jump into the middle of a statement");
        }
    }
}
