namespace org.lb.lbvm.runtime
{
    internal sealed class Variable
    {
        private object value;
        private bool unassigned;

        public Variable()
        {
            value = null;
            unassigned = true;
        }

        public Variable(object o)
        {
            SetValue(o);
        }

        internal void SetValue(object o)
        {
            value = o;
            unassigned = false;
        }

        internal object GetValue()
        {
            if (unassigned) throw new RuntimeException("Read access to unassigned variable");
            return value;
        }

        internal bool IsUnassigned()
        {
            return unassigned;
        }
    }
}