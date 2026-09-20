using System.Text;
using Jef4Net.CodeGen;

if (args.Length != 2) throw new ArgumentException("Usage: Jef4Net.CodeGen <data directory> <output.cs>");
string output = MappingGenerator.Generate(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
File.WriteAllText(args[1], output, new UTF8Encoding(false));
Console.WriteLine($"Generated {args[1]}");
