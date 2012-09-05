namespace org.lb.lbvm.runtime
{
    internal sealed class ValueStack
    {
        private readonly object[] stack = new object[1048576];
        private int count;

        public void Push(object obj)
        {
            if (count == stack.Length - 1) throw new RuntimeException("Value stack overflow");
            stack[count] = obj;
            count++;
        }

        public object Pop()
        {
            if (count == 0) throw new RuntimeException("Value stack underflow");
            return stack[--count];
        }

        public object TopOfStack()
        {
            return stack[count - 1];
        }

        public object GetFromTop(int distanceFromTopOfStack)
        {
            int position = count - distanceFromTopOfStack - 1;
            if (position < 0) throw new RuntimeException("Value stack underflow");
            return stack[position];
        }

        public int Count()
        {
            return count;
        }
    }
}
