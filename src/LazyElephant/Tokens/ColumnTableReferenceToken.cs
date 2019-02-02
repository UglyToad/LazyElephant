namespace LazyElephant.Tokens
{
    using System;

    internal class ColumnTableReferenceToken : IElephantToken
    {
        public string Table { get; }

        public string Column { get; }

        public ColumnTableReferenceToken(string table, string column)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            Column = column ?? throw new ArgumentNullException(nameof(column));
        }

        public override string ToString()
        {
            return $"{Table}.{Column}";
        }
    }
}