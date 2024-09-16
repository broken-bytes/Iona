using ASTLogger;
using Lexer;
using Parser;
using Typeck;
using Typeck.Symbols;
using AST.Nodes;
using Generator;
using System.Collections.Concurrent;

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

        public void Compile(List<CompilationUnit> files)
        {
            SymbolTable globalTable = new SymbolTable();

            ConcurrentBag<INode> asts = new ConcurrentBag<INode>();
            ConcurrentBag<SymbolTable> symbols = new ConcurrentBag<SymbolTable>();

            var logger = ASTLoggerFactory.Create();

            Parallel.ForEach(files, file =>
            {
                var tokens = lexer.Tokenize(file.Source, file.Name);
                var ast = parser.Parse(tokens);
                asts.Add(ast);

                var fileTable = typeck.BuildSymbolTable(ast);
                symbols.Add(fileTable);
            });

            globalTable = typeck.MergeTables(symbols.ToList());

            Parallel.ForEach(asts, ast => typeck.TypeCheck(ast, globalTable));

            Parallel.ForEach(asts, ast => logger.Log(ast));

            Parallel.ForEach(asts, ast => generator.GenerateCIL(ast));

            // Generate code via the final AST
        }
    }
}
