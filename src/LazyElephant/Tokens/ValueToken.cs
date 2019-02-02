namespace LazyElephant.Tokens
{
    using System;

    internal class ValueToken : IElephantToken
    {
        public string Value { get; }

        public ValueToken(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Value provided with empty content", nameof(value));
            }

            Value = value;
        }
    }
}