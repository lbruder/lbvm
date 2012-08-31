using System.Collections.Generic;

namespace org.lb.lbvm
{
    internal sealed class Closure
    {
        public readonly int Target;
        public readonly List<Variable> ClosedOverValues;

        public Closure(IP target, List<Variable> closedOverValues)
        {
            this.Target = target.Value;
            this.ClosedOverValues = closedOverValues;
        }
    }
}
