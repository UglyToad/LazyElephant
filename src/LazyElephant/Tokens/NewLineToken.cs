namespace LazyElephant.Tokens
{
    internal class NewLineToken : IElephantToken
    {
        public static NewLineToken Value { get; } = new NewLineToken();

        private NewLineToken() { }
    }
}