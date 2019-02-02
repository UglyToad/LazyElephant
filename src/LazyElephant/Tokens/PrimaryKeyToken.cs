namespace LazyElephant.Tokens
{
    internal class PrimaryKeyToken : IElephantToken
    {
        public static PrimaryKeyToken Value { get; } = new PrimaryKeyToken();

        private PrimaryKeyToken() { }
    }
}