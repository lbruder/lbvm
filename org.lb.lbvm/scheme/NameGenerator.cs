namespace org.lb.lbvm.scheme
{
    internal sealed class NameGenerator
    {
        private int nextLambdaNumber;
        private int nextLabelNumber;

        public runtime.Symbol NextLambdaName()
        {
            return runtime.Symbol.fromString("##compiler__lambda##" + nextLambdaNumber++);
        }

        public string NextLabel()
        {
            return "##compiler__label##" + nextLabelNumber++;
        }
    }
}