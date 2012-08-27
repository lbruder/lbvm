using System.Collections.Generic;

namespace org.lb.lbvm
{
    internal sealed class Environment
    {
        private readonly Dictionary<int, Variable> values = new Dictionary<int, Variable>();

        public void Set(int symbolNumber, Variable value)
        {
            values[symbolNumber] = value;
        }

        public Variable Get(int symbolNumber)
        {
            return values[symbolNumber];
        }
    }
}
