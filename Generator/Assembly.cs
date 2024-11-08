﻿using System.Reflection;
using System.Runtime.Versioning;
using AST.Nodes;
using Basic.Reference.Assemblies;
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

        public Assembly Generate(INode node, bool intermediate, List<string> assemblies, List<string> assemblyRefs)
        {
            var unit = builder.Build(node);

            unit = WithFileHeader(node.ToString(), unit)
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("UnityEngine")));
            
            var references = new List<MetadataReference>();
            foreach (var reference in assemblyRefs)
            {
                var ass = System.Reflection.Assembly.Load(reference);

                if (ass != null)
                {
                    references.Add(MetadataReference.CreateFromFile(ass.Location));
                }
            }
            
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithPlatform(Platform.AnyCpu)
                .WithNullableContextOptions(NullableContextOptions.Enable);

            CSharpCompilation compilation = CSharpCompilation.Create(Name)
                .WithOptions(options)
                .AddSyntaxTrees(unit.SyntaxTree, AssemblyInfo().SyntaxTree)
                .AddReferences(ReferenceAssemblies.Net80)
                .AddReferences(references);

            if (intermediate)
            {
                File.WriteAllText(node.ToString() + ".cs", unit.NormalizeWhitespace().ToFullString());
                File.WriteAllText(node.ToString() + "__assembly.cs", AssemblyInfo().NormalizeWhitespace().ToFullString());

                return null;
            }

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

        private CompilationUnitSyntax AssemblyInfo()
        {

            
            var assemblyInfo = SyntaxFactory.CompilationUnit()
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.Versioning")))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Reflection")))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.CompilerServices")))
                .NormalizeWhitespace();

            return assemblyInfo;
        }
    }
}
