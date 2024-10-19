using ASTLogger;
using ASTVisualizer;
using Lexer;
using Parser;
using Typeck;
using Symbols;
using Symbols.Symbols;
using AST.Nodes;
using Generator;
using System.Collections.Concurrent;
using System.Reflection;

namespace Compiler
{
    public class Compiler : ICompiler
    {
        private readonly ILexer lexer;
        private readonly IParser parser;
        private readonly ITypeck typeck;
        private readonly IGenerator generator;

        internal Compiler(ILexer lexer, IParser parser, ITypeck typeck, IGenerator generator)
        {
            this.lexer = lexer;
            this.parser = parser;
            this.typeck = typeck;
            this.generator = generator;
        }

        public void Compile(string assemblyName, List<CompilationUnit> files)
        {
            // The compiler is made up of several passes:
            // - Lexing
            // - Parsing
            // - AST construction
            // - Symbol table construction
            // - Scope checking (will be done twice, before and after type checking)
            // - Type checking
            // - Code generation
            // - Assembly building

            SymbolTable globalTable = new SymbolTable();

            ConcurrentBag<INode> asts = new ConcurrentBag<INode>();
            ConcurrentBag<SymbolTable> symbols = new ConcurrentBag<SymbolTable>();

            var logger = ASTLoggerFactory.Create();
            var visualizer = ASTVisualizerFactory.Create();

            Parallel.ForEach(files, file =>
            {
                var tokens = lexer.Tokenize(file.Source, file.Name);
                var ast = parser.Parse(tokens);
                asts.Add(ast);

                var fileTable = typeck.BuildSymbolTable(ast);
                symbols.Add(fileTable);
            });

            globalTable = typeck.MergeTables(symbols.ToList());

            Parallel.ForEach(asts, ast => typeck.CheckTopLevelScopes(ast, globalTable));
            Parallel.ForEach(asts, ast => typeck.TypeCheck(ast, globalTable));
            Parallel.ForEach(asts, ast => typeck.CheckExpressionScopes(ast, globalTable));
            Parallel.ForEach(asts, ast => logger.Log(ast));
            Parallel.ForEach(asts, ast => {
                File.WriteAllText(((FileNode)ast.Root).Name + ".ast", visualizer.Visualize(ast));
            });


            //GenerateCode(assemblyName, asts.ToList(), globalTable);
        }

        private void GenerateCode(string assemblyName, List<INode> asts, SymbolTable globalTable)
        {
            var assembly = generator.CreateAssembly(assemblyName, globalTable);
            Parallel.ForEach(asts, ast => assembly.Generate(ast));
            assembly.Build();
        }
    }
}
