namespace LazyElephant.Console
{
    using System;
    using System.IO;
    using System.Linq;
    using TextGenerators;

    public class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// LazyElephant.exe generate file-or-folder [-o output-directory -proj csproj -osql sql-output -n namespace -c config-file] 
        /// </remarks>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            if (args.Length == 0 || !string.Equals(args[0], "generate", StringComparison.OrdinalIgnoreCase))
            {
                // print help

                return;
            }

            FileOrDirectory input = null;
            FileOrDirectory output = null;

            if (args.Length == 1)
            {
                input = new FileOrDirectory(false, AppDomain.CurrentDomain.BaseDirectory);
                output = new FileOrDirectory(false, AppDomain.CurrentDomain.BaseDirectory);
            }
            else if (args.Length >= 2)
            {
                if (!TryResolve(args[1], out input))
                {
                    Console.WriteLine($"Invalid input file or directory provided: {args[1]}.");
                    return;
                }

                if (args.Length >= 3)
                {
                    if (!TryResolve(args[2], out output))
                    {
                        Console.WriteLine($"Invalid output file or directory provided: {args[2]}.");
                        return;
                    }
                }
                else
                {
                    output = new FileOrDirectory(false, AppDomain.CurrentDomain.BaseDirectory);
                }
            }

            Console.WriteLine($"Generating output to {output.Path} from input {input.Path}");

            var inputs = input.IsFile ? new[] { File.ReadAllText(input.Path) } : Directory.GetFiles(input.Path, "*.ele")
                .Select(File.ReadAllText).ToArray();

            var outputs = Generator.Generate(inputs, new GeneratorOptions("LazyElephant"));

            foreach (var result in outputs)
            {
                var path = Path.Combine(output.Path, result.ObjectName);
                File.WriteAllText(path + ".sql", result.Sql);
                File.WriteAllText(path + ".cs", result.CSharp);
                File.WriteAllText(path + "Repository.cs", result.Repository);
            }
        }

        private static bool TryResolve(string input, out FileOrDirectory result)
        {
            result = null;

            try
            {
                var directory = Path.GetDirectoryName(input);

                if (directory == null)
                {
                    return false;
                }

                try
                {
                    var file = Path.GetFileName(input);
                    result = new FileOrDirectory(true, input);
                }
                catch
                {
                    result = new FileOrDirectory(false, input);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    internal class FileOrDirectory
    {
        public bool IsFile { get; }

        public string Path { get; }

        public FileOrDirectory(bool isFile, string path)
        {
            IsFile = isFile;
            Path = path;
        }
    }

    internal enum Actions
    {
        Help,
        Generate
    }
}
