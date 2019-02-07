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

        public Column GetPrimaryKey()
        {
            var col = default(Column);

            foreach (var column in Columns)
            {
                if (!column.IsPrimaryKey)
                {
                    continue;
                }

                if (col != null)
                {
                    throw new InvalidOperationException($"Both {col.Name} and {column.Name} were primary keys in the table {Name}.");
                }

                col = column;
            }

            return col;
        }
    }
}