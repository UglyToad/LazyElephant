namespace LazyElephant.Tokens
{
    internal class OpenTableToken : IElephantToken
    {
        public static OpenTableToken Value { get; } = new OpenTableToken();

        private OpenTableToken() { }
    }
}