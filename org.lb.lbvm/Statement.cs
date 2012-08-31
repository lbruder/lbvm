using System;
using System.Collections.Generic;

// ReSharper disable RedundantAssignment

namespace org.lb.lbvm
{
    public abstract class Statement
    {
        public abstract int Length { get; }
        protected abstract string Disassembled { get; }
        public override string ToString() { return Disassembled; }
        internal abstract void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack);
    }

    public sealed class EndStatement : Statement
    {
        internal EndStatement() { }
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "END"; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack) { }
    }

    public sealed class PopStatement : Statement
    {
        internal PopStatement() { }
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "POP"; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Pop();
            ip += Length;
        }
    }

    public sealed class PushintStatement : Statement
    {
        internal PushintStatement(int number) { this.Number = number; }
        private readonly int Number;
        public override int Length { get { return 5; } }
        protected override string Disassembled { get { return "PUSHINT " + Number; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(Number);
            ip += Length;
        }
    }

    public sealed class DefineStatement : Statement
    {
        internal DefineStatement(int symbolNumber, string symbol) { this.SymbolNumber = symbolNumber; this.Symbol = symbol; }
        private readonly int SymbolNumber;
        private readonly string Symbol;
        public override int Length { get { return 5; } }
        protected override string Disassembled { get { return "DEFINE " + Symbol; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o = valueStack.Pop();
            if (o is Variable) envStack.Peek().Set(SymbolNumber, (Variable)o); // Link to variable, e.g. in Closure
            else envStack.Peek().Set(SymbolNumber, new Variable(o));
            valueStack.Push(new Symbol(SymbolNumber, Symbol));
            ip += Length;
        }
    }

    public sealed class PushvarStatement : Statement
    {
        internal PushvarStatement(int symbolNumber, string symbol) { this.SymbolNumber = symbolNumber; this.Symbol = symbol; }
        private readonly int SymbolNumber;
        private readonly string Symbol;
        public override int Length { get { return 5; } }
        protected override string Disassembled { get { return "PUSHVAR " + Symbol; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(envStack.Peek().Get(SymbolNumber).GetValue());
            ip += Length;
        }
    }

    public sealed class NumeqStatement : Statement
    {
        internal NumeqStatement() { }
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "NUMEQ"; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o1 = valueStack.Pop();
            object o2 = valueStack.Pop();
            if (!(o1 is int) || !(o2 is int)) throw new Exception("HACK: Numeq -- Check for non-Integer values");
            valueStack.Push((int)o1 == (int)o2);
            ip += Length;
        }
    }

    public sealed class AddStatement : Statement
    {
        internal AddStatement() { }
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "ADD"; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o1 = valueStack.Pop();
            object o2 = valueStack.Pop();
            if (!(o1 is int) || !(o2 is int)) throw new Exception("HACK: Add -- Check for non-Integer values");
            valueStack.Push((int)o1 + (int)o2);
            ip += Length;
        }
    }

    public sealed class SubStatement : Statement
    {
        internal SubStatement() { }
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "SUB"; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o1 = valueStack.Pop();
            object o2 = valueStack.Pop();
            if (!(o1 is int) || !(o2 is int)) throw new Exception("HACK: Sub -- Check for non-Integer values");
            valueStack.Push((int)o2 - (int)o1);
            ip += Length;
        }
    }

    public sealed class MulStatement : Statement
    {
        internal MulStatement() { }
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "MUL"; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o1 = valueStack.Pop();
            object o2 = valueStack.Pop();
            if (!(o1 is int) || !(o2 is int)) throw new Exception("HACK: Mul -- Check for non-Integer values");
            valueStack.Push((int)o1 * (int)o2);
            ip += Length;
        }
    }

    public sealed class DivStatement : Statement
    {
        internal DivStatement() { }
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "DIV"; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o1 = valueStack.Pop();
            object o2 = valueStack.Pop();
            if (!(o1 is int) || !(o2 is int)) throw new Exception("HACK: Div -- Check for non-Integer values");
            valueStack.Push((int)o2 / (int)o1);
            ip += Length;
        }
    }

    public sealed class IdivStatement : Statement
    {
        internal IdivStatement() { }
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "IDIV"; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o1 = valueStack.Pop();
            object o2 = valueStack.Pop();
            if (!(o1 is int) || !(o2 is int)) throw new Exception("HACK: Idiv -- Check for non-Integer values");
            valueStack.Push((int)o2 / (int)o1);
            ip += Length;
        }
    }

    public sealed class ImodStatement : Statement
    {
        internal ImodStatement() { }
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "IMOD"; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o1 = valueStack.Pop();
            object o2 = valueStack.Pop();
            if (!(o1 is int) || !(o2 is int)) throw new Exception("HACK: Imod -- Check for non-Integer values");
            valueStack.Push((int)o2 % (int)o1);
            ip += Length;
        }
    }

    public sealed class BfalseStatement : Statement
    {
        internal BfalseStatement(int target) { this.Target = target; }
        private readonly int Target;
        public override int Length { get { return 5; } }
        protected override string Disassembled { get { return "BFALSE 0x" + Target.ToString("x4"); } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o = valueStack.Pop();
            if (o is bool && (bool)o == false) ip = Target;
            else ip += Length;
        }
    }

    public sealed class EnterStatement : Statement
    {
        internal EnterStatement(int numberOfParameters, string symbol) { this.NumberOfParameters = numberOfParameters; this.Symbol = symbol; }
        private readonly int NumberOfParameters;
        private readonly string Symbol;
        public override int Length { get { return 9; } }
        protected override string Disassembled { get { return "ENTER " + NumberOfParameters + " " + Symbol; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            if (callStack.Peek().NumberOfParameters != NumberOfParameters)
                throw new Exception("Error in program: Function called with wrong number of arguments");
            envStack.Push(new Environment());
            ip += Length;
        }
    }

    public sealed class RetStatement : Statement
    {
        internal RetStatement() { }
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "RET"; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            envStack.Pop();
            ip = callStack.Pop().Ip;
        }
    }

    public sealed class CallStatement : Statement
    {
        internal CallStatement(int numberOfPushedArguments) { this.NumberOfPushedArguments = numberOfPushedArguments; }
        private readonly int NumberOfPushedArguments;
        public override int Length { get { return 5; } }
        protected override string Disassembled { get { return "CALL " + NumberOfPushedArguments; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            // HACK: Jump to valueStack[tos - NumberOfPushedArguments]
            var vs = valueStack.ToArray();
            object target = vs[NumberOfPushedArguments];
            if (target is int)
            {
                callStack.Push(new Call(ip + Length, NumberOfPushedArguments));
                ip = (int)vs[NumberOfPushedArguments];
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
            throw new Exception("Invalid CALL target");
        }
    }

    public sealed class TailcallStatement : Statement
    {
        internal TailcallStatement(int numberOfPushedArguments) { this.NumberOfPushedArguments = numberOfPushedArguments; }
        private readonly int NumberOfPushedArguments;
        public override int Length { get { return 5; } }
        protected override string Disassembled { get { return "TAILCALL " + NumberOfPushedArguments; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            envStack.Pop();
            int oldIp = callStack.Pop().Ip;

            // HACK: Jump to valueStack[tos - NumberOfPushedArguments]
            var vs = valueStack.ToArray();
            object target = vs[NumberOfPushedArguments];
            if (target is int)
            {
                callStack.Push(new Call(oldIp, NumberOfPushedArguments));
                ip = (int)vs[NumberOfPushedArguments];
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
            throw new Exception("Invalid CALL target");
        }
    }

    public sealed class JmpStatement : Statement
    {
        internal JmpStatement(int target) { this.Target = target; }
        private readonly int Target;
        public override int Length { get { return 5; } }
        protected override string Disassembled { get { return "JMP 0x" + Target.ToString("x4"); } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            ip = Target;
        }
    }

    public sealed class PushlabelStatement : Statement
    {
        internal PushlabelStatement(int number) { this.Number = number; }
        private readonly int Number;
        public override int Length { get { return 5; } }
        protected override string Disassembled { get { return "PUSHLABEL 0x" + Number.ToString("x4"); } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(Number);
            ip += Length;
        }
    }

    public sealed class SetStatement : Statement
    {
        internal SetStatement(int symbolNumber, string symbol) { this.SymbolNumber = symbolNumber; this.Symbol = symbol; }
        private readonly int SymbolNumber;
        private readonly string Symbol;
        public override int Length { get { return 5; } }
        protected override string Disassembled { get { return "SET " + Symbol; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            object o = valueStack.Peek();
            envStack.Peek().Get(SymbolNumber).SetValue(o);
            ip += Length;
        }
    }

    public sealed class PushsymStatement : Statement
    {
        internal PushsymStatement(int symbolNumber, string symbol) { this.Symbol = new Symbol(symbolNumber, symbol); }
        private readonly Symbol Symbol;
        public override int Length { get { return 5; } }
        protected override string Disassembled { get { return "PUSHSYM " + Symbol; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(Symbol);
            ip += Length;
        }
    }

    public sealed class PushboolStatement : Statement
    {
        internal PushboolStatement(bool value) { this.Value = value; }
        private readonly bool Value;
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "PUSH " + (Value ? "TRUE" : "FALSE"); } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            valueStack.Push(Value);
            ip += Length;
        }
    }

    public sealed class MakeClosureStatement : Statement
    {
        internal MakeClosureStatement(int numberOfPushedArguments) { this.NumberOfPushedArguments = numberOfPushedArguments; }
        private readonly int NumberOfPushedArguments;
        public override int Length { get { return 5; } }
        protected override string Disassembled { get { return "MAKECLOSURE " + NumberOfPushedArguments; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            List<Variable> values = new List<Variable>();

            for (int i = 0; i < NumberOfPushedArguments; ++i)
                values.Add(envStack.Peek().Get(((Symbol)valueStack.Pop()).Number));
            valueStack.Push(new Closure((int)valueStack.Pop(), values));
            ip += Length;
        }
    }

    public sealed class ErrorStatement : Statement
    {
        internal ErrorStatement() { }
        public override int Length { get { return 1; } }
        protected override string Disassembled { get { return "ERROR"; } }
        internal override void Execute(ref int ip, Stack<object> valueStack, Stack<Environment> envStack, Stack<Call> callStack)
        {
            throw new Exception("Error in program: Jump into the middle of a statement");
        }
    }
}

// ReSharper restore RedundantAssignment
