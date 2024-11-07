using AST.Nodes;
using Generator.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Symbols;

namespace Generator
{
    public class Assembly
    {
        public string Name { get; set; }
        private readonly AssemblyBuilder builder;
        CSharpSyntaxTree syntaxTree;

        public Assembly(string name, SymbolTable table)
        {
            Name = name;
            builder = new AssemblyBuilder(table);
        }

        public Assembly Generate(INode node)
        {
            var freeFunctionsUnit = SyntaxFactory.CompilationUnit();
            
            var unit = builder.Build(node, ref freeFunctionsUnit);

            var builtins = Environment.GetEnvironmentVariable("IONA_SDK_DIR") + "/Iona.Builtins.dll";

            CSharpCompilation compilation = CSharpCompilation.Create(Name)
                .AddSyntaxTrees(unit.SyntaxTree, freeFunctionsUnit.SyntaxTree)
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(builtins))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            
            Console.WriteLine(unit.NormalizeWhitespace().ToFullString());
            Console.WriteLine(freeFunctionsUnit.NormalizeWhitespace().ToFullString());

            using (var stream = new MemoryStream())
            {
                EmitResult result = compilation.Emit(stream);
                if (result.Success)
                {
                    File.WriteAllBytes($"{Name}.dll", stream.ToArray());
                    Console.WriteLine($"Assembly written to {Name}.dll");
                }
                else
                {
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        Console.Error.WriteLine(diagnostic);
                    }
                }
            }

            return this;
        }

        public void Build()
        {
        }
    }
}
