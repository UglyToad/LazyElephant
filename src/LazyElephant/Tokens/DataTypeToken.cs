namespace LazyElephant.Tokens
{
    using System;
    using System.Collections.Generic;

    internal class DataTypeToken : IElephantToken
    {
        private static readonly IReadOnlyDictionary<string, DataTypeToken> KnownTokens = new Dictionary<string, DataTypeToken>(StringComparer.OrdinalIgnoreCase)
        {
            { "guid", new DataTypeToken(typeof(Guid), "uuid", "Guid") },
            { "string", new DataTypeToken(typeof(string), "text", "string") },
            { "datetime", new DataTypeToken(typeof(DateTime), "timestamp", "DateTime") },
            { "bool", new DataTypeToken(typeof(bool), "bool", "bool") },
            { "short", new DataTypeToken(typeof(short), "int2", "short") },
            { "int", new DataTypeToken(typeof(int), "int4", "int") },
            { "long", new DataTypeToken(typeof(long), "int8", "long") },
            { "decimal", new DataTypeToken(typeof(decimal), "numeric", "decimal") },
            { "float", new DataTypeToken(typeof(float), "float4", "float") },
            { "single", new DataTypeToken(typeof(float), "float4", "float") },
            { "double", new DataTypeToken(typeof(double), "float8", "long") },
            { "timespan", new DataTypeToken(typeof(TimeSpan), "interval", "TimeSpan") },
            { "byte[]", new DataTypeToken(typeof(byte[]), "bytea", "byte[]") },
        };

        public Type Type { get; }

        public string PostgresType { get; }

        public string CSharpType { get; }

        public DataTypeToken(Type type, string postgresType, string cSharpType)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            PostgresType = postgresType ?? throw new ArgumentNullException(nameof(postgresType));
            CSharpType = cSharpType ?? throw new ArgumentNullException(nameof(cSharpType));
        }

        public static bool TryParse(string input, out DataTypeToken result)
        {
            return KnownTokens.TryGetValue(input, out result);
        }

        public override string ToString()
        {
            return $"psql: {PostgresType} <-> {Type.Name} :c#";
        }

        public string CSharpTypeWithNullable(Column col)
        {
            if (col.DataType.Type.IsValueType && col.IsNullable)
            {
                return CSharpType + "?";
            }

            return CSharpType;
        }
    }
}