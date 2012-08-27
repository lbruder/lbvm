namespace org.lb.lbvm
{
    internal struct Call
    {
        public Call(int ip, int numberOfParameters)
        {
            Ip = ip;
            NumberOfParameters = numberOfParameters;
        }

        public readonly int Ip;
        public readonly int NumberOfParameters;
    }
}
