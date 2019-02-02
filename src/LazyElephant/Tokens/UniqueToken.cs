namespace LazyElephant.Tokens
{
    internal class UniqueToken : IElephantToken
    {
        public static UniqueToken Value { get; } = new UniqueToken();

        private UniqueToken() { }

        public override string ToString()
        {
            return "uq";
        }
    }
}