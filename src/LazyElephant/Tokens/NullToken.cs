namespace LazyElephant.Tokens
{
    internal class NullToken : IElephantToken
    {
        public static NullToken Value { get; } = new NullToken();

        private NullToken() { }
    }
}