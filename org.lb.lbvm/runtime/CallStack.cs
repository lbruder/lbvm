namespace org.lb.lbvm.runtime
{
    internal sealed class CallStack
    {
        private readonly int[] ipStack = new int[1024];
        private readonly int[] numberOfParametersStack = new int[1024];
        private int count;

        public void Push(int ip, int numberOfParameters)
        {
            if (count == ipStack.Length - 1) throw new exceptions.RuntimeException("Call stack overflow");
            ipStack[count] = ip;
            numberOfParametersStack[count] = numberOfParameters;
            count++;
        }

        public void Pop()
        {
            if (count == 0) throw new exceptions.RuntimeException("Call stack underflow");
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
}
