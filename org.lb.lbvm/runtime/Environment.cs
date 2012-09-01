using System.Collections.Generic;

namespace org.lb.lbvm.runtime
{
    internal sealed class Environment
    {
        private readonly Dictionary<int, Variable> values = new Dictionary<int, Variable>();

        public bool HasVariable(int symbolNumber)
        {
            return values.ContainsKey(symbolNumber);
        }

        public void Set(int symbolNumber, Variable value)
        {
            values[symbolNumber] = value;
        }

        public Variable Get(int symbolNumber, string symbolName)
        {
            if (!HasVariable(symbolNumber)) throw new RuntimeException("Unknown variable '" + symbolName + "'");
            return values[symbolNumber];
        }
    }
}
