﻿using System.Collections.Generic;
using System.Text;

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
        public readonly int Value;

        public IP(int value)
        {
            Value = value;
        }
    }

    internal sealed class Nil
    {
        private static readonly Nil instance = new Nil();

        private Nil()
        {
        }

        public static Nil GetInstance()
        {
            return instance;
        }

        public override string ToString()
        {
            return "()";
        }
    }

    internal sealed class Pair
    {
        public readonly object First;
        public readonly object Second;

        public Pair(object first, object second)
        {
            this.First = first;
            this.Second = second;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("(");
            sb.Append(First);

            for (object i = Second; ; i = ((Pair)i).Second)
            {
                if (i is Pair)
                {
                    sb.Append(" ");
                    sb.Append(((Pair)i).First);
                }
                else if (i is Nil)
                {
                    break;
                }
                else
                {
                    sb.Append(" . ");
                    sb.Append(i);
                    break;
                }
            }
            sb.Append(")");
            return sb.ToString();
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