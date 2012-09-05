using System.Collections.Generic;

namespace org.lb.lbvm.runtime
{
    internal sealed class EnvironmentStack
    {
        private readonly Dictionary<int, Variable>[] stack = new Dictionary<int, Variable>[1024];
        int count;
        private Dictionary<int, Variable> TOS;

        public void PushNew()
        {
            if (count == stack.Length - 1) throw new RuntimeException("Environment stack overflow");
            if (stack[count] == null) stack[count] = new Dictionary<int, Variable>();
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

        public bool HasVariable(int symbolNumber)
        {
            return TOS.ContainsKey(symbolNumber);
        }

        public void Set(int symbolNumber, Variable value)
        {
            TOS[symbolNumber] = value;
        }

        public Variable Get(int symbolNumber, string symbolName)
        {
            if (!HasVariable(symbolNumber)) throw new RuntimeException("Unknown variable '" + symbolName + "'");
            return TOS[symbolNumber];
        }
    }

    //internal sealed class EnvironmentStack
    //{
    //    private readonly Stack<Dictionary<int, Variable>> stack = new Stack<Dictionary<int, Variable>>();

    //    public void PushNew()
    //    {
    //        stack.Push(new Dictionary<int, Variable>());
    //    }

    //    public void Pop()
    //    {
    //        stack.Pop();
    //    }

    //    public int Count()
    //    {
    //        return stack.Count;
    //    }

    //    public bool HasVariable(int symbolNumber)
    //    {
    //        return stack.Peek().ContainsKey(symbolNumber);
    //    }

    //    public void Set(int symbolNumber, Variable value)
    //    {
    //        stack.Peek()[symbolNumber] = value;
    //    }

    //    public Variable Get(int symbolNumber, string symbolName)
    //    {
    //        if (!HasVariable(symbolNumber)) throw new RuntimeException("Unknown variable '" + symbolName + "'");
    //        return stack.Peek()[symbolNumber];
    //    }
    //}
}
