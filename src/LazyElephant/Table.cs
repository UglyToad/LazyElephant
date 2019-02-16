namespace LazyElephant
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class TablePlaceholder
    {
        public string Schema { get; }

        public string Name { get; }

        public TablePlaceholder(string name, string schema = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Schema = schema;
        }
    }

    internal struct SchemaQualifiedName
    {
        /// <summary>
        /// Postgres Standard Style Schema name.
        /// </summary>
        public string Schema { get; }

        /// <summary>
        /// Postgres Standard Style Table name.
        /// </summary>
        public string Table { get; }

        public SchemaQualifiedName(string schema, string table)
        {
            Schema = schema?.ToPostgresStyle() ?? throw new ArgumentNullException(nameof(schema));
            Table = table?.ToPostgresStyle() ?? throw new ArgumentNullException(nameof(table));
        }
    }

    internal class Table
    {
        public SchemaQualifiedName Name { get; }

        public string CSharpStyleName { get; }

        public string PrimaryKeyName { get; }

        public IReadOnlyList<Column> Columns { get; }

        public Table(SchemaQualifiedName name, IEnumerable<Column> columns)
        {
            Name = name;
            Columns = columns?.ToList() ?? throw new ArgumentNullException(nameof(columns));
            
            CSharpStyleName = ColumnPlaceholder.GetCSharpName(name.Table);
            PrimaryKeyName = Columns.FirstOrDefault(x => x.IsPrimaryKey)?.Name;
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