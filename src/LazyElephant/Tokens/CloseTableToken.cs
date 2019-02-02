namespace LazyElephant.Tokens
{
    internal class CloseTableToken : IElephantToken
    {
        public static CloseTableToken Value { get; } = new CloseTableToken();

        private CloseTableToken() { }
    }
}