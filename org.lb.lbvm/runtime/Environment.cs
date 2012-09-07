using System.Collections.Specialized;

namespace org.lb.lbvm.runtime
{
    internal sealed class EnvironmentStack
    {
        private readonly HybridDictionary[] stack = new HybridDictionary[1024];
        int count;
        private HybridDictionary TOS;

        public void PushNew()
        {
            if (count == stack.Length - 1) throw new exceptions.RuntimeException("Environment stack overflow");
            if (stack[count] == null) stack[count] = new HybridDictionary();
            else stack[count].Clear();
            count++;
            TOS = stack[count - 1];
        }

        public void Pop()
        {
            if (count == 0) throw new exceptions.RuntimeException("Environment stack underflow");
            count--;
            TOS = stack[count - 1];
        }

        public int Count()
        {
            return count;
        }

        public bool HasVariable(Symbol symbol)
        {
            return TOS.Contains(symbol);
        }

        public void Set(Symbol symbol, Variable value)
        {
            TOS[symbol] = value;
        }

        public Variable Get(Symbol symbol)
        {
            if (!HasVariable(symbol)) throw new exceptions.RuntimeException("Unknown variable '" + symbol + "'");
            return (Variable)TOS[symbol];
        }
    }
}
