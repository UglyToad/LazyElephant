namespace LazyElephant.TextGenerators
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class SqlGenerator
    {
        public IReadOnlyList<GeneratedSql> GetSql(IReadOnlyList<Table> tables, GeneratorOptions options)
        {
            var builder = new StringBuilder();

            var result = new List<GeneratedSql>();

            foreach (var table in tables)
            {
                builder.Clear();

                var dependencies = new List<Table>();
                
                var schema = table.Name.Schema;
                builder.Append($"CREATE TABLE {schema}.{table.Name.Table}").Append(" (").AppendLine();

                for (var c = 0; c < table.Columns.Count; c++)
                {
                    var column = table.Columns[c];
                    builder.AddTab(options).Append(column.Name.ToPostgresStyle())
                        .Append(' ');

                    if (column.AutogeneratePrimaryKey && column.DataType.Type == typeof(int))
                    {
                        builder.Append("SERIAL");
                    }
                    else
                    {
                        builder.Append(column.DataType.PostgresType.ToUpperInvariant());
                    }

                    if (column.IsPrimaryKey)
                    {
                        builder.Append(" PRIMARY KEY");
                    }

                    if (column.AutogeneratePrimaryKey)
                    {
                        if (column.DataType.Type == typeof(Guid))
                        {
                            builder.Append(' ')
                                .Append("DEFAULT uuid_generate_v4()");
                        }
                        else if (column.DataType.Type != typeof(int))
                        {
                            throw new NotSupportedException($"Generating default primary key values is unsupported for data type {column.DataType.Type.Name} in table: {column}.");
                        }
                    }

                    // primary key is implicitly not null
                    if (!column.IsNullable && !column.IsPrimaryKey)
                    {
                        builder.Append(" NOT NULL");
                    }

                    if (column.IsForeignKey)
                    {
                        builder.Append(" REFERENCES ")
                            .Append($"{column.ForeignKey.Schema.ToLowerInvariant()}.{column.ForeignKey.Table.ToLowerInvariant()}(").Append(column.ForeignKey.Column.ToPostgresStyle())
                            .Append(')');

                        // TODO: add dependency
                    }

                    if (column.HasDefault)
                    {
                        builder.Append(" DEFAULT ")
                            .Append(column.GetDefaultValueExpression());
                    }

                    if (column.IsUnique && !column.IsPrimaryKey)
                    {
                        builder.Append(" UNIQUE");
                    }

                    if (c < table.Columns.Count - 1)
                    {
                        builder.Append(',');
                    }

                    builder.Append("\r\n");
                }

                builder.Append(')').Append(';');

                result.Add(new GeneratedSql(table, builder.ToString(), dependencies));
            }

            return result;
        }
    }
}
