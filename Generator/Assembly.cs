﻿using AST.Nodes;
using Generator.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            unit = WithFileHeader(node.ToString(), unit);
            freeFunctionsUnit = WithFileHeader("GENERATED", freeFunctionsUnit);

            var builtins = Environment.GetEnvironmentVariable("IONA_SDK_DIR") + "/Iona.Builtins.dll";

            var runtime = System.Reflection.Assembly.Load("System.Private.CoreLib");
            
            CSharpCompilation compilation = CSharpCompilation.Create(Name)
                .AddSyntaxTrees(unit.SyntaxTree,freeFunctionsUnit.SyntaxTree)
                .AddReferences(MetadataReference.CreateFromFile(runtime.Location))
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

        private CompilationUnitSyntax WithFileHeader(string file, CompilationUnitSyntax unit)
        {
             var line1 = "Generated by the Iona Compiler";
                var line2 = "Version: v0.0.1";
                var line3 = $"Source: {file}";
                var line4 = $"Date: {DateTime.Now}";
                var line5 = "Caution: Do not manually edit this file!";
                
                var neededWidth = Int32.Max(line1.Count(), line2.Count());
                neededWidth = Int32.Max(neededWidth, line3.Count());
                neededWidth = Int32.Max(neededWidth, line4.Count());
                neededWidth = Int32.Max(neededWidth, line5.Count());

                // Padding
                var paddingLeft = 2;
                var paddingRight = 4;
                
                var headerComment1 = SyntaxFactory.Comment($"// ┌{new string('─', neededWidth + paddingLeft + paddingRight)}┐");
                var headerComment2 = SyntaxFactory.Comment($"// │{new string(' ', paddingLeft)}{line1}{new string(' ', neededWidth - line1.Count() + paddingRight - 1)} │");
                var headerComment3 = SyntaxFactory.Comment($"// │{new string(' ', paddingLeft)}{line2}{new string(' ', neededWidth - line2.Count() + paddingRight - 1)} │");
                var headerComment4 = SyntaxFactory.Comment($"// │{new string(' ', paddingLeft)}{line3}{new string(' ', neededWidth - line3.Count() + paddingRight - 1)} │");
                var headerComment5 = SyntaxFactory.Comment($"// │{new string(' ', paddingLeft)}{line4}{new string(' ', neededWidth - line4.Count() + paddingRight - 1)} │");
                var headerComment6 = SyntaxFactory.Comment($"// │{new string(' ', paddingLeft)}{line5}{new string(' ', neededWidth - line5.Count() + paddingRight - 1)} │");
                var headerComment7 = SyntaxFactory.Comment($"// └{new string('─', neededWidth + paddingLeft + paddingRight)}┘");

                var newLine = SyntaxFactory.CarriageReturnLineFeed;

                // Create a SyntaxTriviaList with the comments and new lines
                var leadingTrivia = SyntaxFactory.TriviaList(
                    headerComment1,
                    newLine,
                    headerComment2,
                    newLine,
                    headerComment3,
                    newLine,
                    headerComment4,
                    newLine,
                    headerComment5,
                    newLine,
                    headerComment6,
                    newLine,
                    headerComment7,
                    newLine,
                    newLine
                );
                
                return unit.WithLeadingTrivia(leadingTrivia);
        }
    }
}
