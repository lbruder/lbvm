namespace org.lb.lbvm.scheme
{
    public sealed class Symbol
    {
        public readonly string Name;

        public Symbol(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return "<Scheme symbol " + Name + ">";
        }

        public override bool Equals(object obj)
        {
            Symbol other = obj as Symbol;
            return other != null && other.Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}