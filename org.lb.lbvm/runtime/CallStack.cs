namespace org.lb.lbvm.runtime
{
    internal sealed class CallStack
    {
        private readonly int[] ipStack = new int[1024];
        private readonly int[] numberOfParametersStack = new int[1024];
        private int count;

        public void Push(int ip, int numberOfParameters)
        {
            if (count == ipStack.Length - 1) throw new RuntimeException("Call stack overflow");
            ipStack[count] = ip;
            numberOfParametersStack[count] = numberOfParameters;
            count++;
        }

        public void Pop()
        {
            if (count == 0) throw new RuntimeException("Call stack underflow");
            count--;
        }

        public int GetLastIp()
        {
            return ipStack[count - 1];
        }

        public int GetLastNumberOfParameters()
        {
            return numberOfParametersStack[count - 1];
        }

        public int Count()
        {
            return count;
        }
    }

    //internal sealed class CallStack
    //{
    //    private struct Call
    //    {
    //        public readonly int Ip;
    //        public readonly int NumberOfParameters;

    //        public Call(int ip, int numberOfParameters)
    //        {
    //            Ip = ip;
    //            NumberOfParameters = numberOfParameters;
    //        }
    //    }

    //    private readonly Stack<Call> stack = new Stack<Call>();

    //    public void Push(int ip, int numberOfParameters)
    //    {
    //        stack.Push(new Call(ip, numberOfParameters));
    //    }

    //    public void Pop()
    //    {
    //        stack.Pop();
    //    }

    //    public int GetLastIp()
    //    {
    //        return stack.Peek().Ip;
    //    }

    //    public int GetLastNumberOfParameters()
    //    {
    //        return stack.Peek().NumberOfParameters;
    //    }

    //    public int Count()
    //    {
    //        return stack.Count;
    //    }
    //}
}
