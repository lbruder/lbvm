namespace org.lb.lbvm
{
    internal sealed class Variable
    {
        private object value;

        public Variable(object o)
        {
            value = o;
        }

        internal void SetValue(object o)
        {
            value = o;
        }

        internal object GetValue()
        {
            return value;
        }
    }
}