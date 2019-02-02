namespace LazyElephant.Tokens
{
    using System;

    internal class NameToken : IElephantToken
    {
        public string Name { get; }

        public NameToken(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString()
        {
            return $"Name: {Name}";
        }
    }
}