namespace LazyElephant
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Tokens;

    internal class Tokenizer
    {
        private static readonly IReadOnlyDictionary<string, Func<IElephantToken>> Tokens = new Dictionary<string, Func<IElephantToken>>(StringComparer.OrdinalIgnoreCase)
        {
            { "pk", () => PrimaryKeyToken.Value },
            { "null", () => NullToken.Value },
            { "fk", () => ForeignKeyToken.Value },
            { "df", () => DefaultToken.Value },
            { "uq", () => UniqueToken.Value },
            { "ag", () => AutoGenerateToken.Value },
        };

        public IEnumerable<IElephantToken> Tokenize(string input)
        {
            var lastWasNewline = false;
            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];

                if (IsNewline(c))
                {
                    if (lastWasNewline)
                    {
                        continue;
                    }

                    lastWasNewline = true;
                    yield return NewLineToken.Value;
                    continue;
                }

                lastWasNewline = false;

                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                switch (c)
                {
                    case ',':
                        yield return CommaToken.Value;
                        break;
                    case '[':
                        yield return ReadValue(input, ref i);
                        break;
                    case '{':
                        yield return OpenTableToken.Value;
                        break;
                    case '}':
                        yield return CloseTableToken.Value;
                        break;
                    default:
                        var result = ReadTillEnd(input, ref i);
                        if (Tokens.TryGetValue(result, out var factory))
                        {
                            yield return factory();
                            break;
                        }

                        var dotIndex = result.IndexOf('.');

                        if (dotIndex >= 0)
                        {
                            yield return new ColumnTableReferenceToken(result.Substring(0, dotIndex), result.Substring(dotIndex + 1));
                            break;
                        }

                        if (DataTypeToken.TryParse(result, out var dataTypeToken))
                        {
                            yield return dataTypeToken;
                            break;
                        }

                        yield return new NameToken(result);
                        break;
                }
            }
        }

        private static bool IsNewline(char c)
        {
            return c == '\n' || c == '\r';
        }

        private static string ReadTillEnd(string input, ref int i)
        {
            if (i == input.Length - 1)
            {
                return input[i].ToString();
            }

            var c = input[i];
            var precedingDots = 0;
            var builder = new StringBuilder().Append(input[i]);

            while (!char.IsWhiteSpace(c) && i < input.Length - 1)
            {
                c = input[i + 1];
                switch (c)
                {
                    case '[':
                    case ']':
                    case '{':
                    case '}':
                    case ',':
                        return builder.ToString();
                    case '_':
                    case '-':
                        builder.Append(c);
                        break;
                    case '.':
                        if (precedingDots < 1)
                        {
                            builder.Append(c);
                            precedingDots++;
                        }
                        else
                        {
                            throw new InvalidOperationException($"String contained multiple periods, value so far was '{builder}' at position: {i + 1}.");
                        }
                        break;
                    default:
                        if (!char.IsLetterOrDigit(c))
                        {
                            if (char.IsWhiteSpace(c))
                            {
                                return builder.ToString();
                            }

                            throw new InvalidOperationException($"Invalid character '{c}' at position {i + 1}.");
                        }

                        builder.Append(c);
                        break;
                }

                i++;
            }

            return builder.ToString();
        }

        private static ValueToken ReadValue(string input, ref int i)
        {
            var builder = new StringBuilder();
            var c = input[i];

            while (c != ']')
            {
                if (i == input.Length - 1)
                {
                    throw new InvalidOperationException("String ended before the end of the value was found.");
                }

                c = input[i + 1];

                if (c == ']')
                {
                    break;
                }

                if (IsNewline(c))
                {
                    throw new InvalidOperationException("Newline occurred in value.");
                }

                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                builder.Append(c);

                i++;
            }

            return new ValueToken(builder.ToString());
        }
    }
}
