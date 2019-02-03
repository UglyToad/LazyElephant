namespace LazyElephant.TextGenerators
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class GeneratedSql
    {
        public Table Table { get; }

        public string Sql { get; }

        public IReadOnlyList<Table> Dependencies { get; }

        public GeneratedSql(Table table, string sql, IReadOnlyList<Table> dependencies)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            Sql = sql ?? throw new ArgumentNullException(nameof(sql));
            Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public string GetSelectAll()
        {
            var builder = new StringBuilder();

            GetSelectAllColumns(builder);
                
            builder.Append(";");

            return builder.ToString();
        }

        public string GetSelectByPk()
        {
            var builder = new StringBuilder();

            GetSelectAllColumns(builder);

            var pkCol = Table.GetPrimaryKey();
            var pk = Column.GetPostgresName(Table.GetPrimaryKey().Name);

            builder.AppendLine().Append("WHERE ")
                .Append(pk)
                .Append(" = @").Append(Column.GetCSharpName(pkCol.Name, true))
                .Append(";");

            return builder.ToString();
        }

        public string GetDeleteByPk()
        {
            var pkCol = Table.GetPrimaryKey();
            var pk = Column.GetPostgresName(Table.GetPrimaryKey().Name);

            var builder = new StringBuilder("DELETE FROM ");

            AddQualifiedTableName(builder);
                
            builder.Append(" WHERE ")
                .Append(pk).Append(" = @").Append(Column.GetCSharpName(pkCol.Name, true)).Append(';');

            return builder.ToString();
        }

        private void GetSelectAllColumns(StringBuilder builder)
        {
            builder.Append("SELECT ");

            for (var i = 0; i < Table.Columns.Count; i++)
            {
                var column = Table.Columns[i];
                builder.Append(Column.GetPostgresName(column.Name));

                if (i < Table.Columns.Count - 1)
                {
                    builder.AppendLine(",");
                }
                else
                {
                    builder.AppendLine();
                }
            }

            builder.Append("FROM ");

            AddQualifiedTableName(builder);
        }

        public (string sql, IReadOnlyList<string> parameters) Create(GeneratorOptions options)
        {
            var builder = new StringBuilder("INSERT INTO ");
            AddQualifiedTableName(builder);
            builder.Append("(");

            var parameters = new List<string>();
            var columns = new List<string>();

            for (var i = 0; i < Table.Columns.Count; i++)
            {
                var column = Table.Columns[i];

                var par = Column.GetPostgresName(column.Name);

                columns.Add(par);

                if (column.AutogeneratePrimaryKey)
                {
                    continue;
                }

                builder.Append(par);
                parameters.Add(Column.GetCSharpName(column.Name, true));

                if (i < Table.Columns.Count - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.AppendLine(")").Append("VALUES (");

            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                builder.Append('@').Append(parameter);

                if (i < parameters.Count - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.AppendLine(")");
            builder.Append("RETURNING ");

            for (var i = 0; i < columns.Count; i++)
            {
                builder.Append(columns[i]);

                if (i < columns.Count - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.Append(';');

            return (builder.ToString(), parameters);
        }

        private StringBuilder AddQualifiedTableName(StringBuilder builder)
        {
            builder.Append(Table.Schema ?? "public").Append('.').Append(Table.Name);

            return builder;
        }

        public (string sql, IReadOnlyList<string> parameters) Update(GeneratorOptions options)
        {
            var builder = new StringBuilder("UPDATE ");
            AddQualifiedTableName(builder).AppendLine();

            var parameters = new List<string>();
            var columns = new List<string>();

            builder.Append("SET ");

            for (var i = 0; i < Table.Columns.Count; i++)
            {
                var column = Table.Columns[i];

                var col = Column.GetPostgresName(column.Name);
                columns.Add(col);

                if (column.IsPrimaryKey)
                {
                    continue;
                }

                var parameter = Column.GetCSharpName(column.Name, true);
                parameters.Add(parameter);

                builder.Append(col).Append(" = @").Append(parameter);

                if (i < Table.Columns.Count - 1)
                {
                    builder.AppendLine(",");
                }
            }

            var pk = Table.GetPrimaryKey();
            var pkName = Column.GetPostgresName(pk.Name);
            var pkParam = Column.GetCSharpName(pk.Name, true);
            parameters.Insert(0, pkParam);

            builder.AppendLine().Append("WHERE ").Append(pkName).Append(" = @").AppendLine(pkParam);

            builder.Append("RETURNING ");

            for (var i = 0; i < columns.Count; i++)
            {
                builder.Append(columns[i]);

                if (i < columns.Count - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.Append(';');

            return (builder.ToString(), parameters);
        }
    }

    internal class GeneratedRepository
    {
        public Table Table { get; }

        public string Repository { get; }

        public string Namespace { get; }

        public GeneratedRepository(Table table, string repository, string ns)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Namespace = ns ?? throw new ArgumentNullException(nameof(ns));
        }
    }
}