namespace LazyElephant.Tokens
{
    internal class ForeignKeyToken : IElephantToken
    {
        public static ForeignKeyToken Value { get; } = new ForeignKeyToken();

        private ForeignKeyToken() { }
    }
}