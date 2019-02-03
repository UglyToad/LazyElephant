namespace LazyElephant.TextGenerators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    internal class ClassGenerator
    {
        public IReadOnlyDictionary<Table, GeneratedClass> GetClasses(IReadOnlyList<GeneratedSql> tables, GeneratorOptions options)
        {
            var builder = new StringBuilder();

            var result = new Dictionary<Table, GeneratedClass>();

            for (var i = 0; i < tables.Count; i++)
            {
                var table = tables[i].Table;
                var usingSystem = table.Columns.Any(x => x.DataType.Type == typeof(Guid) 
                                                         || x.DataType.Type == typeof(DateTime) 
                                                         || x.DataType.Type == typeof(TimeSpan));

                if (usingSystem)
                {
                    builder.Append("using System;").AppendLine().AppendLine();
                }

                var className = Column.GetCSharpName(table.Name);

                builder.Append("namespace ").Append(options.ClassNamespace).AppendLine()
                    .Append('{').AppendLine()
                    .AddTab(options).Append("public class ").Append(className).AppendLine()
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

                result[table] = new GeneratedClass(table, className, options.ClassNamespace, builder.ToString(), tables[i]);

                builder.Clear();
            }

            return result;
        }
    }
}
