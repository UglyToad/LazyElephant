namespace LazyElephant
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TextGenerators;
    using Tokens;

    public class GeneratorOptions
    {
        public int TabSize { get; set; } = 4;

        public bool UseTabs { get; set; }

        public string RepositorySuffix { get; set; } = "Repository";

        public bool UseUnderscore { get; set; }

        public string ClassNamespace { get; }

        public string RepositoryNamespace { get; }

        public GeneratorOptions(string classNamespace, string repositoryNamespace = null)
        {
            ClassNamespace = classNamespace ?? throw new ArgumentNullException(nameof(classNamespace));
            RepositoryNamespace = repositoryNamespace ?? ClassNamespace;
        }
    }

    public static class Generator
    {
        private static readonly Tokenizer Tokenizer = new Tokenizer();
        private static readonly SqlGenerator SqlGenerator = new SqlGenerator();
        private static readonly ClassGenerator ClassGenerator = new ClassGenerator();
        private static readonly RepositoryClassGenerator RepositoryClassGenerator = new RepositoryClassGenerator();

        public static Result Generate(string input, GeneratorOptions options)
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
                            currentColumn = null;
                        }
                        currentTable.Columns = new List<Column>(columns);
                        tables.Add(currentTable);
                        currentTable = null;
                        expectsTable = true;
                        break;
                }

                precedingToken = token;
            }

            var sql = SqlGenerator.GetSql(tables, options);
            var classes = ClassGenerator.GetClasses(sql.Values.ToList(), options);
            var repos = RepositoryClassGenerator.GetRepositories(classes.Values.ToList(), options);

            return new Result
            {
                Sql = sql[tables[0]].Sql,
                Class = classes[tables[0]].Class,
                Repository = repos[tables[0]].Repository
            };
        }
    }
}