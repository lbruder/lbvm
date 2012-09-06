using System.Collections.Generic;

namespace org.lb.lbvm.runtime
{
    internal sealed class EnvironmentStack
    {
        private readonly Dictionary<Symbol, Variable>[] stack = new Dictionary<Symbol, Variable>[1024];
        int count;
        private Dictionary<Symbol, Variable> TOS;

        public void PushNew()
        {
            if (count == stack.Length - 1) throw new RuntimeException("Environment stack overflow");
            if (stack[count] == null) stack[count] = new Dictionary<Symbol, Variable>();
            else stack[count].Clear();
            count++;
            TOS = stack[count - 1];
        }

        public void Pop()
        {
            if (count == 0) throw new RuntimeException("Environment stack underflow");
            count--;
            TOS = stack[count - 1];
        }

        public int Count()
        {
            return count;
        }

        public bool HasVariable(Symbol symbol)
        {
            return TOS.ContainsKey(symbol);
        }

        public void Set(Symbol symbol, Variable value)
        {
            TOS[symbol] = value;
        }

        public Variable Get(Symbol symbol)
        {
            if (!HasVariable(symbol)) throw new RuntimeException("Unknown variable '" + symbol + "'");
            return TOS[symbol];
        }
    }
}
