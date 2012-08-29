namespace org.lb.lbvm
{
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
