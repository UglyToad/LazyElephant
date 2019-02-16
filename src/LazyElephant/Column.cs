namespace LazyElephant
{
    using System;
    using System.Text;
    using Tokens;

    internal static class StringHandler
    {
        public static string ToPostgresStyle(this string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var builder = new StringBuilder();
            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        builder.Append('_');
                    }

                    builder.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }

    internal class ColumnPlaceholder
    {
        public string Name { get; set; }

        public DataTypeToken DataType { get; set; }

        public ValueToken DefaultValue { get; set; }

        public bool IsForeignKey => ForeignKey != null;

        public bool IsPrimaryKey { get; set; }

        public bool AutogeneratePrimaryKey { get; set; }

        public bool HasDefault => DefaultValue != null;

        public bool IsNullable { get; set; }

        private ForeignKeyDetailsToken foreignKey;
        public ForeignKeyDetailsToken ForeignKey
        {
            get => foreignKey;
            set
            {
                if (IsPrimaryKey)
                {
                    throw new InvalidOperationException($"The column '{Name}' is already a primary key, it may not also be a foreign key.");
                }

                foreignKey = value;
            }
        }

        public int? MaxLength { get; set; }
        public bool IsUnique { get; set; }

        public override string ToString()
        {
            var extra = IsPrimaryKey ? "PK" : IsForeignKey ? "FK" : IsNullable ? "NN" : string.Empty;
            return $"{Name} {DataType.Type.Name}{extra}";
        }

        public static string GetCSharpName(string name, bool firstLower = false)
        {
            var builder = new StringBuilder();

            var usedFirst = false;
            var precedingWasUnderscore = false;
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (!usedFirst && char.IsLetter(c))
                {
                    builder.Append(firstLower ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c));
                    usedFirst = true;
                    continue;
                }

                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(precedingWasUnderscore ? char.ToUpperInvariant(c) : c);
                }

                if (c == '_')
                {
                    precedingWasUnderscore = true;
                }
                else
                {
                    precedingWasUnderscore = false;
                }
            }

            return builder.ToString();
        }
    }

    internal class Column
    {
        public string Name { get; }

        public string CSharpStyleName { get;}

        public string CSharpStyleNameLower { get; }

        public DataTypeToken DataType { get; }

        public ValueToken DefaultValue { get; }

        public bool IsForeignKey => ForeignKey != null;

        public bool IsPrimaryKey { get; }

        public bool AutogeneratePrimaryKey { get; }

        public bool HasDefault => DefaultValue != null;

        public bool IsNullable { get; }

        public ForeignKeyDetailsToken ForeignKey { get; }
        
        public int? MaxLength { get; }

        public bool IsUnique { get; }

        public Column(string name, DataTypeToken dataType, ValueToken defaultValue, bool isPrimaryKey, bool autogeneratePrimaryKey, 
            bool isNullable, 
            ForeignKeyDetailsToken foreignKey, 
            int? maxLength, 
            bool isUnique)
        {
            if (foreignKey != null && isPrimaryKey)
            {
                throw new ArgumentException($"A column cannot be both a primary and foreign key. Column: {Name}.");   
            }

            Name = name?.ToPostgresStyle() ?? throw new ArgumentNullException(nameof(name));
            CSharpStyleName = ColumnPlaceholder.GetCSharpName(name);
            CSharpStyleNameLower = ColumnPlaceholder.GetCSharpName(name, true);
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            DefaultValue = defaultValue;
            IsPrimaryKey = isPrimaryKey;
            AutogeneratePrimaryKey = autogeneratePrimaryKey;
            IsNullable = isNullable;
            ForeignKey = foreignKey;
            MaxLength = maxLength;
            IsUnique = isUnique;
        }

        public string GetDefaultValueExpression()
        {
            if (!HasDefault)
            {
                throw new InvalidOperationException("The column has no default value.");
            }

            if (DataType.Type == typeof(DateTime))
            {
                if (string.Equals(DefaultValue.Value, "now", StringComparison.OrdinalIgnoreCase))
                {
                    return "NOW()";
                }

                if (string.Equals(DefaultValue.Value, "utcnow", StringComparison.OrdinalIgnoreCase))
                {
                    return "(NOW() at time zone 'utc')";
                }

                throw new InvalidOperationException($"Unrecognized default value for DateTime column: {DefaultValue.Value}.");
            }

            if (DataType.Type == typeof(short))
            {
                if (!short.TryParse(DefaultValue.Value, out var shortResult))
                {
                    throw new InvalidOperationException($"The default value for a short field was an invalid format: {DefaultValue.Value}.");
                }

                return shortResult.ToString();
            }

            if (DataType.Type == typeof(int))
            {
                if (!int.TryParse(DefaultValue.Value, out var intResult))
                {
                    throw new InvalidOperationException($"The default value for an int field was an invalid format: {DefaultValue.Value}.");
                }

                return intResult.ToString();
            }

            if (DataType.Type == typeof(long))
            {
                if (!long.TryParse(DefaultValue.Value, out var longResult))
                {
                    throw new InvalidOperationException($"The default value for a long field was an invalid format: {DefaultValue.Value}.");
                }

                return longResult.ToString();
            }

            if (DataType.Type == typeof(float))
            {
                if (!float.TryParse(DefaultValue.Value, out var _))
                {
                    throw new InvalidOperationException($"The default value for a float field was an invalid format: {DefaultValue.Value}.");
                }

                return DefaultValue.Value;
            }

            if (DataType.Type == typeof(double))
            {
                if (!double.TryParse(DefaultValue.Value, out var _))
                {
                    throw new InvalidOperationException($"The default value for a double field was an invalid format: {DefaultValue.Value}.");
                }

                return DefaultValue.Value;
            }

            if (DataType.Type == typeof(decimal))
            {
                if (!decimal.TryParse(DefaultValue.Value, out var decimalResult))
                {
                    throw new InvalidOperationException($"The default value for a decimal field was an invalid format: {DefaultValue.Value}.");
                }

                return decimalResult.ToString("G");
            }

            throw new NotImplementedException($"Not implemented for value '{DefaultValue.Value}' on type '{DataType.Type.Name}'.");
        }
    }
}