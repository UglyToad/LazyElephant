namespace LazyElephant
{
    using System.Text;
    using TextGenerators;

    internal static class BuilderExtensions
    {
        public static StringBuilder AddTab(this StringBuilder builder, GeneratorOptions opts, int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                if (opts.UseTabs)
                {
                    builder.Append('\t');
                }
                else
                {
                    builder.Append(' ', opts.TabSize);
                }
            }

            return builder;
        }
    }
}