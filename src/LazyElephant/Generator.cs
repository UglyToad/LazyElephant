namespace LazyElephant
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Tokens;

    public class GeneratorOptions
    {
        public int TabSize { get; set; } = 4;
        public bool UseTabs { get; set; }
    }

    public static class Generator
    {
        private static readonly Tokenizer Tokenizer = new Tokenizer();

        public static Result Generate(string input, string @namespace, GeneratorOptions options)
        {
            var expectsTable = true;
            var nextMustOpenTable = false;
            var careAboutSplitter = false;
            var precedingToken = default(IElephantToken);

            var tables = new List<Table>();
            var columns = new List<Column>();
            
            var currentTable = default(Table);
            var currentColumn = default(Column);
            foreach (var token in Tokenizer.Tokenize(input))
            {
                if (nextMustOpenTable)
                {
                    if (token == OpenTableToken.Value)
                    {
                        nextMustOpenTable = false;
                    }
                    else if (token != NewLineToken.Value)
                    {
                        throw new InvalidOperationException($"Expected open table token '{{' after table name: {currentTable.Name}.");
                    }

                    continue;
                }

                if (precedingToken == ForeignKeyToken.Value)
                {
                    if (currentColumn == null)
                    {
                        throw new InvalidOperationException("Protected keyword 'fk' used outside column context.");
                    }

                    if (token is ColumnTableReferenceToken foreignTable)
                    {
                        currentColumn.ForeignKey = foreignTable;
                    }
                    else
                    {
                        throw new InvalidOperationException($"No table-column of the format 'table.column' provided for foreign key. Instead found: {token}.");
                    }

                    precedingToken = token;
                    continue;
                }

                if (precedingToken == DefaultToken.Value)
                {
                    if (currentColumn == null)
                    {
                        throw new InvalidOperationException("Protected keyword 'df' used outside column context.");
                    }

                    if (token is ValueToken valueToken)
                    {
                        currentColumn.DefaultValue = valueToken;
                    }
                    else
                    {
                        throw new InvalidOperationException($"No default value of the format '[ some_value ]' provided for default. Instead found: {token}.");
                    }

                    precedingToken = token;
                    continue;
                }

                switch (token)
                {
                    case NewLineToken _:
                    case CommaToken _:
                        if (!careAboutSplitter || currentTable == null)
                        {
                            continue;
                        }
                        columns.Add(currentColumn);
                        if (currentColumn.Name == null || currentColumn.DataType == null)
                        {
                            throw new InvalidOperationException("Column did not contain at least a name and a data type.");
                        }
                        currentColumn = null;
                        careAboutSplitter = false;
                        break;
                    case NameToken name:
                        if (expectsTable)
                        {
                            currentTable = new Table(name.Name);
                            expectsTable = false;
                            nextMustOpenTable = true;
                            break;
                        }

                        if (currentColumn == null)
                        {
                            currentColumn = new Column
                            {
                                Name = name.Name
                            };
                            careAboutSplitter = true;
                        }
                        break;
                    case ColumnTableReferenceToken columnTableReferenceToken:
                        if (expectsTable)
                        {
                            currentTable = new Table(columnTableReferenceToken.Column, columnTableReferenceToken.Table);

                            expectsTable = false;
                            nextMustOpenTable = true;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Table-column name {columnTableReferenceToken} found outside the context of a foreign key in table: '{currentTable.Name}'.");
                        }
                        break;
                    case DataTypeToken dataTypeToken:
                        if (currentColumn == null)
                        {
                            throw new InvalidOperationException($"Unexpected data type declaration outside column: {dataTypeToken.Type.Name}.");
                        }

                        currentColumn.DataType = dataTypeToken;
                        break;
                    case PrimaryKeyToken _:
                        if (currentColumn == null)
                        {
                            throw new InvalidOperationException($"Unexpected primary key 'pk' declaration outside column in table: '{currentTable.Name}'.");
                        }

                        currentColumn.IsPrimaryKey = true;
                        break;
                    case ForeignKeyToken _:
                        if (currentColumn == null)
                        {
                            throw new InvalidOperationException($"Unexpected foreign key 'fk' declaration outside column in table: '{currentTable.Name}'.");
                        }

                        if (currentColumn.IsPrimaryKey)
                        {
                            throw new InvalidOperationException($"The column '{currentColumn.Name}' is already a primary key, it cannot also be a foreign key in table: '{currentTable.Name}'.");
                        }
                        break;
                    case NullToken _:
                        if (currentColumn == null)
                        {
                            throw new InvalidOperationException($"Unexpected not null 'nn' declaration outside column in table: '{currentTable.Name}'.");
                        }

                        if (currentColumn.IsPrimaryKey)
                        {
                            throw new InvalidOperationException($"The column '{currentColumn.Name}' is a primary key, it cannot be nullable.");
                        }

                        currentColumn.IsNullable = true;
                        break;
                    case ValueToken innerValueToken:
                        if (currentColumn == null)
                        {
                            throw new InvalidOperationException($"Unexpected value declaration '{innerValueToken.Value}' outside column.");
                        }

                        if (currentColumn.DataType.Type == typeof(string) && int.TryParse(innerValueToken.Value, out var val))
                        {
                            currentColumn.MaxLength = val;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unexpected value declaration '{innerValueToken.Value}' for non string column and not following default in table: '{currentTable.Name}'.");
                        }

                        break;
                    case UniqueToken _:
                        if (currentColumn == null)
                        {
                            throw new InvalidOperationException($"Unexpected unique 'uq' declaration outside column in table: '{currentTable.Name}'.");
                        }

                        currentColumn.IsUnique = true;
                        break;
                    case AutoGenerateToken _:
                        if (currentColumn == null)
                        {
                            throw new InvalidOperationException($"Unexpected auto generate 'ag' declaration outside column in table: '{currentTable.Name}'.");
                        }

                        if (!currentColumn.IsPrimaryKey)
                        {
                            throw new InvalidOperationException($"The column '{currentColumn.Name}' is not a primary key, it cannot be autogenerated.");
                        }

                        currentColumn.AutogeneratePrimaryKey = true;
                        break;
                    case CloseTableToken _:
                        if (currentTable == null)
                        {
                            throw new InvalidOperationException("Found end of table marker '}' outside of a table.");
                        }

                        if (currentColumn != null)
                        {
                            columns.Add(currentColumn);
                        }
                        currentTable.Columns = new List<Column>(columns);
                        tables.Add(currentTable);
                        currentTable = null;
                        break;
                }

                precedingToken = token;
            }

            var sql = GetSql(tables, options);

            return new Result
            {
                Sql = sql,
                Class = GetCSharpClass(tables, @namespace, options)
            };
        }

        private static string GetSql(IReadOnlyList<Table> tables, GeneratorOptions options)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < tables.Count; i++)
            {
                var table = tables[i];
                if (i < tables.Count - 1)
                {
                    builder.AppendLine().AppendLine();
                }

                var schema = table.Schema ?? "public";
                builder.Append($"CREATE TABLE {schema.ToLowerInvariant()}.{table.Name.ToLowerInvariant()}").Append(" (").AppendLine();

                for (var c = 0; c < table.Columns.Count; c++)
                {
                    var column = table.Columns[c];
                    builder.AddTab(options).Append(Column.GetPostgresName(column.Name))
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
                            // TODO: real schema here please
                            .Append($"public.{column.ForeignKey.Table.ToLowerInvariant()}(").Append(Column.GetPostgresName(column.ForeignKey.Column))
                            .Append(')');
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
            }

            return builder.ToString();
        }

        private static string GetCSharpClass(IReadOnlyList<Table> tables, string @namespace, GeneratorOptions options)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < tables.Count; i++)
            {
                var table = tables[i];
                var usingSystem = table.Columns.Any(x => x.DataType.Type == typeof(Guid) || x.DataType.Type == typeof(DateTime) || x.DataType.Type == typeof(TimeSpan));

                if (usingSystem)
                {
                    builder.Append("using System;").AppendLine().AppendLine();
                }

                builder.Append("namespace ").Append(@namespace).AppendLine()
                        .Append('{').AppendLine()
                        .AddTab(options).Append("public class ").Append(Column.GetCSharpName(table.Name)).AppendLine()
                        .AddTab(options)
                    .Append('{').AppendLine();

                for (var j = 0; j < table.Columns.Count; j++)
                {
                    var col = table.Columns[j];

                    builder.AddTab(options, 2).Append("public ").Append(col.DataType.CSharpTypeWithNullable(col))
                        .Append(" ")
                        .Append(Column.GetCSharpName(col.Name)).Append(" { get; set; }").AppendLine();

                    if (j < table.Columns.Count - 1)
                    {
                        builder.AppendLine();
                    }
                }

                builder.AddTab(options).Append('}').AppendLine().Append('}');
            }

            return builder.ToString();
        }
    }

    internal static class BuilderExtensions
    {
        public static StringBuilder AddTab(this StringBuilder builder, GeneratorOptions opts, int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                if (opts.UseTabs)
                {
                    builder.Append('\t');
                }
                else
                {
                    builder.Append(' ', opts.TabSize);
                }
            }

            return builder;
        }
    }
}