namespace LazyElephant
{
    using System;
    using System.Collections.Generic;

    internal class Table
    {
        public string Schema { get; }

        public string Name { get; }

        public IReadOnlyList<Column> Columns { get; set; }

        public Table(string name, string schema = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Schema = schema;
        }
    }
}