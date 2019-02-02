namespace LazyElephant.Tokens
{
    internal class DefaultToken : IElephantToken
    {
        public static DefaultToken Value { get; } = new DefaultToken();

        private DefaultToken() { }
    }
}