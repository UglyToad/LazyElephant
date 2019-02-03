namespace LazyElephant.TextGenerators
{
    using System;

    internal class GeneratedClass
    {
        public Table Table { get; }

        public string Name { get; }

        public string Namespace { get; }

        public string Class { get; }

        public GeneratedSql Sql { get; }

        public GeneratedClass(Table table, string name, string ns, string @class, GeneratedSql sql)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Namespace = ns ?? throw new ArgumentNullException(nameof(ns));
            Class = @class ?? throw new ArgumentNullException(nameof(@class));
            Sql = sql ?? throw new ArgumentNullException(nameof(sql));
        }
    }
}