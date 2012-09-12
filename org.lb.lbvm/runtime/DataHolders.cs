using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace org.lb.lbvm.runtime
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

    public interface OutputPort
    {
        void Print(string value);
    }

    internal sealed class Pair : IEnumerable<object>
    {
        public readonly object First;
        public readonly object Second;

        public Pair(object first, object second)
        {
            this.First = first;
            this.Second = second;
        }

        public IEnumerator<object> GetEnumerator()
        {
            yield return First;
            for (object i = Second; ; i = ((Pair)i).Second)
            {
                if (i is Pair)
                    yield return ((Pair)i).First;
                else if (i is Nil)
                    break;
                else
                {
                    yield return i;
                    break;
                }
            }
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal sealed class StringObject
    {
        public string Value { get; private set; }

        public StringObject(string value)
        {
            Value = value;
        }

        public StringObject(int numberOfCharacters)
        {
            Value = new String(' ', numberOfCharacters);
        }

        public override string ToString()
        {
            return Value;
        }

        public int Compare(object obj, bool ci)
        {
            StringObject other = obj as StringObject;
            if (other == null) return 1;
            return string.Compare(Value, other.Value, ci ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        public void SetCharAt(int index, char c)
        {
            Value = Value.Substring(0, index) + c + Value.Substring(index + 1, Value.Length - index - 1);
        }

        public static string Escape(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        public static string Unescape(string value)
        {
            return value
                .Replace("\\\"", "\"")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\\", "\\");
        }
    }

    internal sealed class Symbol
    {
        private readonly string Name;
        private readonly int hashCode;

        private static readonly Dictionary<string, Symbol> cache = new Dictionary<string, Symbol>();

        public static Symbol fromString(string symbol)
        {
            Symbol ret;
            if (cache.TryGetValue(symbol, out ret)) return ret;
            ret = new Symbol(symbol);
            cache[symbol] = ret;
            return ret;
        }

        private Symbol(string name)
        {
            Name = name;
            hashCode = name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return obj == this;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }
    }
}
