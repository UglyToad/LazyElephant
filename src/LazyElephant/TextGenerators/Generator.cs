namespace LazyElephant.TextGenerators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tokens;

    public class GeneratorOptions
    {
        public int TabSize { get; set; } = 4;

        public bool UseTabs { get; set; }

        public string RepositorySuffix { get; set; } = "Repository";

        public bool UseUnderscore { get; set; }

        public string ClassNamespace { get; }

        public string RepositoryNamespace { get; }

        public string DefaultSchema { get; set; } = "public";

        public GeneratorOptions(string classNamespace, string repositoryNamespace = null)
        {
            ClassNamespace = classNamespace ?? throw new ArgumentNullException(nameof(classNamespace));
            RepositoryNamespace = repositoryNamespace ?? ClassNamespace;
        }
    }

    public class GeneratedResult
    {
        public string ObjectName { get; }

        public string Sql { get; }

        public string CSharp { get; }

        public string Repository { get; }

        public GeneratedResult(string objectName, string sql, string cSharp, string repository)
        {
            ObjectName = objectName ?? throw new ArgumentNullException(nameof(objectName));
            Sql = sql ?? throw new ArgumentNullException(nameof(sql));
            CSharp = cSharp ?? throw new ArgumentNullException(nameof(cSharp));
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }
    }

    public static class Generator
    {
        private static readonly Tokenizer Tokenizer = new Tokenizer();
        private static readonly SqlGenerator SqlGenerator = new SqlGenerator();
        private static readonly ClassGenerator ClassGenerator = new ClassGenerator();
        private static readonly RepositoryClassGenerator RepositoryClassGenerator = new RepositoryClassGenerator();

        public static IReadOnlyList<GeneratedResult> Generate(IReadOnlyList<string> inputs, GeneratorOptions options)
        {
            var tables = new List<Table>();
            foreach (var input in inputs)
            {
                var templateTables = Parse(input, options);
                tables.AddRange(templateTables);
            }
            
            var sql = SqlGenerator.GetSql(tables, options);
            var classes = ClassGenerator.GetClasses(sql, options);
            var repos = RepositoryClassGenerator.GetRepositories(classes, options);

            var result = new List<GeneratedResult>();

            for (int i = 0; i < sql.Count; i++)
            {
                result.Add(new GeneratedResult(tables[i].CSharpStyleName, sql[i].Sql, classes[i].Class, repos[i].Repository));
            }
            
            return result;
        }

        private static IReadOnlyList<Table> Parse(string input, GeneratorOptions options)
        {
            var expectsTable = true;
            var nextMustOpenTable = false;
            var careAboutSplitter = false;
            var precedingToken = default(IElephantToken);

            var tables = new List<Table>();
            var columns = new List<ColumnPlaceholder>();

            var currentTable = default(TablePlaceholder);
            var currentColumn = default(ColumnPlaceholder);
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

                    if (token is ForeignKeyDetailsToken foreignTable)
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
                            currentTable = new TablePlaceholder(name.Name);
                            expectsTable = false;
                            nextMustOpenTable = true;
                            break;
                        }

                        if (currentColumn == null)
                        {
                            currentColumn = new ColumnPlaceholder
                            {
                                Name = name.Name
                            };
                            careAboutSplitter = true;
                        }
                        break;
                    case ColumnTableReferenceToken columnTableReferenceToken:
                        if (expectsTable)
                        {
                            currentTable = new TablePlaceholder(columnTableReferenceToken.Column, columnTableReferenceToken.Table);

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
                            currentColumn = null;
                        }

                        var qualifiedName = new SchemaQualifiedName(currentTable.Schema ?? options.DefaultSchema ?? "public", currentTable.Name);
                        var actualColumns = columns.Select(x => new Column(x.Name, x.DataType, x.DefaultValue, x.IsPrimaryKey, x.AutogeneratePrimaryKey,
                            x.IsNullable, x.ForeignKey, x.MaxLength, x.IsUnique));
                        tables.Add(new Table(qualifiedName, actualColumns));
                        currentTable = null;
                        columns.Clear();
                        expectsTable = true;
                        break;
                }

                precedingToken = token;
            }

            return tables;
        }
    }
}