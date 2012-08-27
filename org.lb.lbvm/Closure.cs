using System.Collections.Generic;

namespace org.lb.lbvm
{
    internal sealed class Closure
    {
        public readonly int Target;
        public readonly List<Variable> ClosedOverValues;

        public Closure(int target, List<Variable> closedOverValues)
        {
            this.Target = target;
            this.ClosedOverValues = closedOverValues;
        }
    }
}
