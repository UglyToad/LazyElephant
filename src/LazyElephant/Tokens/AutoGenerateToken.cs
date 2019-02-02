namespace LazyElephant.Tokens
{
    internal class AutoGenerateToken : IElephantToken
    {
        public static AutoGenerateToken Value { get; } = new AutoGenerateToken();

        private AutoGenerateToken() { }
    }
}