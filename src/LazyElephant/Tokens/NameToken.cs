namespace LazyElephant.Tokens
{
    using System;

    internal class NameToken : IElephantToken
    {
        public string Name { get; }

        public NameToken(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString()
        {
            return $"Name: {Name}";
        }
    }

    internal class ForeignKeyDetailsToken : IElephantToken
    {
        public string Table { get; }
        public string Column { get; }
        public string Schema { get; }

        public ForeignKeyDetailsToken(string table, string column, string schema = "public")
        {
            Column = column;
            Schema = schema;
            Table = table;
        }
    }
}