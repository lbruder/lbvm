using System.Collections.Generic;

namespace org.lb.lbvm.runtime
{
    internal struct Call
    {
        public readonly int Ip;
        public readonly int NumberOfParameters;

        public Call(int ip, int numberOfParameters)
        {
            Ip = ip;
            NumberOfParameters = numberOfParameters;
        }
    }
    
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

    internal sealed class IP
    {
        public int Value;
        
        public IP(int value)
        {
            Value = value;
        }
    }

    internal sealed class Symbol
    {
        public readonly string Name;
        public readonly int Number;

        public Symbol(int number, string name)
        {
            Name = name;
            Number = number;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
