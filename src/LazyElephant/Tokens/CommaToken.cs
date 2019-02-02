namespace LazyElephant.Tokens
{
    internal class CommaToken : IElephantToken
    {
        public static CommaToken Value { get; } = new CommaToken();

        private CommaToken() { }
    }
}