using System.Text;
using Jef4Net.CodeGen;

if (args.Length is < 2 or > 3) throw new ArgumentException("Usage: Jef4Net.CodeGen <data directory> <output.cs> [fujitsu|hitachi]");
string target = args.Length == 3 ? args[2] : "fujitsu";
string output = target.Equals("hitachi", StringComparison.OrdinalIgnoreCase)
    ? HitachiMappingGenerator.Generate(args[0])
    : target.Equals("fujitsu", StringComparison.OrdinalIgnoreCase)
        ? MappingGenerator.Generate(args[0])
        : throw new ArgumentException("Target must be fujitsu or hitachi.");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
File.WriteAllText(args[1], output, new UTF8Encoding(false));
Console.WriteLine($"Generated {args[1]}");
