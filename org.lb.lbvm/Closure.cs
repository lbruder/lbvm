namespace org.lb.lbvm
{
    internal sealed class Closure
    {
        public readonly int Target;
        public readonly Environment closureEnv;

        public Closure(int target, Environment closureEnv)
        {
            this.Target = target;
            this.closureEnv = closureEnv;
        }
    }
}
