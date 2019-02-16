namespace LazyElephant.TextGenerators
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class RepositoryClassGenerator
    {
        private const string GetAll = "GetAll";
        private const string Get = "Get";
        private const string Delete = "Delete";
        private const string Create = "Create";
        private const string Update = "Update";

        private static readonly IReadOnlyList<string> RequiredNamespaces = new[]
        {
            "System",
            "System.Collections.Generic",
            "System.Data.Common",
            "System.Threading.Tasks",
            "Npgsql"
        };

        public IReadOnlyList<GeneratedRepository> GetRepositories(IReadOnlyList<GeneratedClass> classes, GeneratorOptions options)
        {
            var builder = new StringBuilder();

            var result = new List<GeneratedRepository>();

            var connectionUsing = "using (var connection = new NpgsqlConnection(" + (options.UseUnderscore ? "_connectionString" : "connectionString") + "))";

            var namespaceRequired = options.ClassNamespace != options.RepositoryNamespace;

            foreach (var generatedClass in classes)
            {
                var name = GetQualifiedClassName(generatedClass);

                foreach (var requiredNamespace in RequiredNamespaces)
                {
                    builder.Append("using ").Append(requiredNamespace).AppendLine(";");
                }

                if (namespaceRequired)
                {
                    builder.Append("using ").Append(options.ClassNamespace).AppendLine(";");
                }

                builder.AppendLine();

                builder.Append("namespace ").AppendLine(options.RepositoryNamespace).AppendLine("{");
                // Now inside the namespace
                {
                    builder.AddTab(options).Append("public class ").Append(generatedClass.Name).Append(options.RepositorySuffix).AppendLine();
                    builder.AddTab(options).AppendLine("{");
                    // Now inside the class
                    {
                        AddConstructorAndField(generatedClass, options, builder);
                        builder.AppendLine();

                        // ReSharper disable UnusedVariable
                        using (var getAll = new GeneratedMethod($"IEnumerable<{name}>", GetAll, string.Empty, builder, options))
                        {
                            using (var conn = new GeneratedUsing(connectionUsing, builder, options))
                            {
                                builder.AddTab(options, 4)
                                    .Append("var command = new NpgsqlCommand(@\"");

                                var statement = generatedClass.Sql.GetSelectAll();

                                AddWithAlignedIndentation(statement, 4, options, builder);

                                builder.AppendLine("\", connection);").AppendLine();

                                using (var reader = new GeneratedUsing("using (var reader = await command.ExecuteReaderAsync())", builder, options, 4))
                                {
                                    builder.AddTab(options, 5).AppendLine("return Enumerate(reader);");
                                }
                            }
                        }

                        builder.AppendLine();

                        var pkCol = generatedClass.Table.GetPrimaryKey();
                        var pkParamName = pkCol.CSharpStyleNameLower;
                        var argByPk = pkCol.DataType.CSharpType + " " + pkParamName;

                        using (var get = new GeneratedMethod(name, Get, argByPk, builder, options))
                        {
                            using (var conn = new GeneratedUsing(connectionUsing, builder, options))
                            {
                                builder.AddTab(options, 4)
                                    .Append("var command = new NpgsqlCommand(@\"");

                                var statement = generatedClass.Sql.GetSelectByPk();

                                AddWithAlignedIndentation(statement, 4, options, builder);

                                builder.AppendLine("\", connection);").AppendLine();

                                builder.AddTab(options, 4).Append("command.Parameters.AddWithValue(\"")
                                    .Append(pkParamName)
                                    .Append("\", ").Append(pkParamName).AppendLine(");").AppendLine();

                                using (var reader = new GeneratedUsing("using (var reader = await command.ExecuteReaderAsync())", builder, options, 4))
                                {
                                    using (var mvNext = new GeneratedUsing("while (await reader.ReadAsync())", builder, options, 5))
                                    {
                                        builder.AddTab(options, 6).AppendLine("return GetCurrent(reader);");
                                    }
                                }

                                builder.AppendLine();
                                builder.AddTab(options, 4).AppendLine("return null;");
                            }
                        }

                        builder.AppendLine();

                        using (var del = new GeneratedMethod("bool", Delete, argByPk, builder, options))
                        {
                            using (var conn = new GeneratedUsing(connectionUsing, builder, options))
                            {
                                builder.AddTab(options, 4)
                                    .Append("var command = new NpgsqlCommand(@\"");

                                var statement = generatedClass.Sql.GetDeleteByPk();

                                AddWithAlignedIndentation(statement, 4, options, builder);

                                builder.AppendLine("\", connection);").AppendLine();

                                builder.AddTab(options, 4).Append("command.Parameters.AddWithValue(\"")
                                    .Append(pkParamName)
                                    .Append("\", ").Append(pkParamName).AppendLine(");");

                                builder.AppendLine().AddTab(options, 4).AppendLine("var result = await command.ExecuteNonQueryAsync();");

                                builder.AppendLine().AddTab(options, 4).AppendLine("return result > 0;");
                            }
                        }

                        builder.AppendLine();
                        var paramName = ColumnPlaceholder.GetCSharpName(generatedClass.Name, true);

                        using (var create = new GeneratedMethod(name, Create, $"{name} {paramName}", builder, options))
                        {
                            using (var conn = new GeneratedUsing(connectionUsing, builder, options))
                            {
                                builder.AddTab(options, 4)
                                    .Append("var command = new NpgsqlCommand(@\"");

                                var statement = generatedClass.Sql.Create(options);

                                AddWithAlignedIndentation(statement.sql, 4, options, builder);

                                builder.AppendLine("\", connection);").AppendLine();

                                foreach (var param in statement.parameters)
                                {
                                    builder.AddTab(options, 4).Append("command.Parameters.AddWithValue(\"")
                                    .Append(param)
                                    .Append("\", ").Append($"{paramName}.{ColumnPlaceholder.GetCSharpName(param)}").AppendLine(");");
                                }

                                builder.AppendLine();

                                using (var reader = new GeneratedUsing("using (var reader = await command.ExecuteReaderAsync())", builder, options, 4))
                                {
                                    using (var mvNext = new GeneratedUsing("while (await reader.ReadAsync())", builder, options, 5))
                                    {
                                        builder.AddTab(options, 6).AppendLine("return GetCurrent(reader);");
                                    }
                                }

                                builder.AppendLine();
                                builder.AddTab(options, 4).AppendLine("return null;");
                            }
                        }

                        builder.AppendLine();

                        using (var update = new GeneratedMethod(name, Update, $"{name} {paramName}", builder, options))
                        {
                            using (var conn = new GeneratedUsing(connectionUsing, builder, options))
                            {
                                builder.AddTab(options, 4)
                                    .Append("var command = new NpgsqlCommand(@\"");

                                var statement = generatedClass.Sql.Update(options);

                                AddWithAlignedIndentation(statement.sql, 4, options, builder);

                                builder.AppendLine("\", connection);").AppendLine();

                                foreach (var param in statement.parameters)
                                {
                                    builder.AddTab(options, 4).Append("command.Parameters.AddWithValue(\"")
                                        .Append(param)
                                        .Append("\", ").Append($"{paramName}.{ColumnPlaceholder.GetCSharpName(param)}").AppendLine(");");
                                }

                                builder.AppendLine();

                                using (var reader = new GeneratedUsing("using (var reader = await command.ExecuteReaderAsync())", builder, options, 4))
                                {
                                    using (var mvNext = new GeneratedUsing("while (await reader.ReadAsync())", builder, options, 5))
                                    {
                                        builder.AddTab(options, 6).AppendLine("return GetCurrent(reader);");
                                    }
                                }

                                builder.AppendLine();
                                builder.AddTab(options, 4).AppendLine("return null;");
                            }
                        }

                        builder.AppendLine();

                        using (var enumer = new GeneratedMethod($"IEnumerable<{name}>", "Enumerate", "DbDataReader reader", builder, options, "private", false, true))
                        {
                            using (var reader = new GeneratedUsing("while (reader.Read())", builder, options))
                            {
                                builder.AddTab(options, 4).AppendLine("yield return GetCurrent(reader);");
                            }
                        }

                        builder.AppendLine();
                        
                        using (var enumer = new GeneratedMethod(name, "GetCurrent", "DbDataReader reader", builder, options, "private", false, true))
                        {
                            builder.AddTab(options, 3).Append("return new ").Append(name).AppendLine()
                                .AddTab(options, 3).AppendLine("{");

                            for (var i = 0; i < generatedClass.Table.Columns.Count; i++)
                            {
                                var col = generatedClass.Table.Columns[i];

                                var colName = ColumnPlaceholder.GetCSharpName(col.Name);

                                builder.AddTab(options, 4).Append(colName).Append(" = ")
                                    .Append(GetReaderQueryForColumn(col, i));

                                if (i < generatedClass.Table.Columns.Count - 1)
                                {
                                    builder.AppendLine(",");
                                }
                                else
                                {
                                    builder.AppendLine();
                                }
                            }

                            builder.AddTab(options, 3).AppendLine("};");
                        }
                    }
                    // ReSharper restore UnusedVariable

                    builder.AddTab(options).AppendLine("}");
                }
                builder.Append('}');

                var repoCode = builder.ToString();

                result.Add(new GeneratedRepository(generatedClass.Table, repoCode, options.RepositoryNamespace));

                builder.Clear();
            }

            return result;
        }

        private static void AddConstructorAndField(GeneratedClass generatedClass, GeneratorOptions options, StringBuilder builder)
        {
            var fieldName = options.UseUnderscore ? "_connectionString" : "connectionString";

            builder.AddTab(options, 2).Append("private readonly string ").Append(fieldName).AppendLine(";").AppendLine();

            builder.AddTab(options, 2).Append("public ").Append(generatedClass.Name).Append(options.RepositorySuffix)
                .AppendLine("(string connectionString)")
                .AddTab(options, 2).AppendLine("{");

            builder.AddTab(options, 3).Append(options.UseUnderscore ? string.Empty : "this.").Append(fieldName).AppendLine(" = connectionString;");

            builder.AddTab(options, 2).AppendLine("}");
        }

        private static string GetQualifiedClassName(GeneratedClass generatedClass)
        {
            // TODO: check class name for conflict with each required namespace
            if (generatedClass.Name == "Task")
            {
                return $"{generatedClass.Namespace}.{generatedClass.Name}";
            }

            return generatedClass.Name;
        }

        private static void AddWithAlignedIndentation(string text, int depth, GeneratorOptions opts, StringBuilder builder)
        {
            var newLine = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '\r' || c == '\n')
                {
                    newLine = true;
                    continue;
                }

                if (newLine)
                {
                    builder.AppendLine().AddTab(opts, depth);
                    newLine = false;
                }

                builder.Append(c);
            }
        }

        private static string GetReaderQueryForColumn(Column column, int index)
        {
            var str = string.Empty;
            if (column.IsNullable)
            {
                var dfExpress = column.DataType.Type.IsValueType ? $"{column.DataType.CSharpType}?" : column.DataType.CSharpType;
                str += $"reader.IsDBNull({index}) ? default({dfExpress}) : ";
            }

            str += $"reader.Get{column.DataType.Type.Name}({index})";

            return str;
        }
    }

    internal class GeneratedMethod : IDisposable
    {
        private readonly StringBuilder builder;
        private readonly GeneratorOptions options;

        public GeneratedMethod(string returnType, string name, string arguments, StringBuilder builder,
            GeneratorOptions options, string accessModifier = "public", bool isAsync = true, bool isStatic = false)
        {
            this.builder = builder;
            this.options = options;
            builder.AddTab(options, 2).Append(accessModifier).Append(" ");

            if (isStatic)
            {
                builder.Append("static ");
            }

            if (isAsync)
            {
                builder.Append("async Task<").Append(returnType).Append("> ");
            }
            else
            {
                builder.Append(returnType).Append(" ");
            }
                
            builder.Append(name)
                .Append("(").Append(arguments).AppendLine(")");
            builder.AddTab(options, 2).AppendLine("{");
        }

        public void Dispose()
        {
            builder.AddTab(options, 2).AppendLine("}");
        }
    }

    internal class GeneratedUsing : IDisposable
    {
        private readonly StringBuilder builder;
        private readonly GeneratorOptions options;
        private readonly int depth;

        public GeneratedUsing(string statement, StringBuilder builder, GeneratorOptions options, int depth = 3)
        {
            this.builder = builder;
            this.options = options;
            this.depth = depth;

            builder.AddTab(options, depth).AppendLine(statement)
                .AddTab(options, depth).AppendLine("{");
        }

        public void Dispose()
        {
            builder.AddTab(options, depth).AppendLine("}");
        }
    }
}
