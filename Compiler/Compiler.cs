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
using Shared;
using Assembly = System.Reflection.Assembly;

namespace Compiler
{
    public class Compiler : ICompiler
    {
        private readonly ILexer lexer;
        private readonly IParser parser;
        private readonly ITypeck typeck;
        private readonly IGenerator generator;
        private readonly IErrorCollector errorCollector;
        private readonly IWarningCollector warningCollector;
        private readonly IFixItCollector fixItCollector;

        internal Compiler(
            ILexer lexer, 
            IParser parser, 
            ITypeck typeck, 
            IGenerator generator, 
            IErrorCollector errorCollector,
            IWarningCollector warningCollector,
            IFixItCollector fixItCollector
        )
        {
            this.lexer = lexer;
            this.parser = parser;
            this.typeck = typeck;
            this.generator = generator;
            this.errorCollector = errorCollector;
            this.warningCollector = warningCollector;
            this.fixItCollector = fixItCollector;
        }

        public void Compile(string assemblyName, List<CompilationUnit> files, bool intermediate, List<string> assemblyPaths, List<string> assemblyRefs)
        {
            // Add IONA SDK to the references
            assemblyRefs.Add("Iona.Builtins");
            assemblyPaths.Add(Environment.GetEnvironmentVariable("IONA_SDK_DIR"));
            // The compiler is made up of several passes:
            // - Lexing
            // - Parsing
            // - AST construction
            // - Symbol table construction
            // - Scope checking (will be done twice, before and after type checking)
            // - Type checking
            // - Code generation
            // - Assembly building
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                // Get the assembly name that failed to load
                string assemblyName = new AssemblyName(args.Name).Name + ".dll";

                // Construct the full path to the assembly in the specified folder
                // First, check if the file is directly contained in some path
                foreach (var path in assemblyPaths)
                {
                    foreach (var file in Directory.GetFiles(path, "*.dll"))
                    {
                        if (file.EndsWith(assemblyName))
                        {
                            return Assembly.LoadFile(file);
                        }
                    }
                }
                
                // Check the IONA_SDK_DIR
                var ionaSdkDir = Environment.GetEnvironmentVariable("IONA_SDK_DIR");

                foreach (var file in Directory.GetFiles(ionaSdkDir, "*.dll"))
                {
                    if (file.EndsWith(assemblyName))
                    {
                        return Assembly.LoadFile(file);
                    }
                }
                
                // If the assembly was not found in the custom path, return null (continue searching)
                return null;
            };

            SymbolTable globalTable = new SymbolTable();

            ConcurrentBag<INode> asts = new ConcurrentBag<INode>();
            ConcurrentBag<SymbolTable> symbols = new ConcurrentBag<SymbolTable>();

            var logger = ASTLoggerFactory.Create();
            var visualizer = ASTVisualizerFactory.Create();

            Parallel.ForEach(files, file =>
            {
                var tokens = lexer.Tokenize(file.Source, file.Name);
                var ast = parser.Parse(tokens, assemblyName);
                asts.Add(ast);

                var fileTable = typeck.BuildSymbolTable(ast, assemblyName);
                symbols.Add(fileTable);
            });
            
            globalTable = typeck.MergeTables(symbols.ToList(), assemblyRefs);

            Parallel.ForEach(asts, ast => typeck.CheckTopLevelScopes(ast, globalTable));
            Parallel.ForEach(asts, ast => typeck.TypeCheck(ast, globalTable));
            Parallel.ForEach(asts, ast => typeck.CheckExpressions(ast, globalTable));
            Parallel.ForEach(asts, ast => {
                logger.Log(ast);
                File.WriteAllText(((FileNode)ast.Root).Name + ".ast", visualizer.Visualize(ast));
            });
            
            if (errorCollector.Errors.Any())
            {
                foreach(var error in errorCollector.Errors)
                {
                    error.Log();
                }
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Compilation failed. See above errors for details.");
                
                Console.ResetColor();
                
                return;
            }
            
            Console.ForegroundColor = ConsoleColor.Green;
            GenerateCode(assemblyName, asts.ToList(), globalTable, intermediate, assemblyPaths, assemblyRefs);
        }

        private void GenerateCode(
            string assemblyName, 
            List<INode> asts, 
            SymbolTable globalTable, 
            bool intermediate, 
            List<string> assemblyPaths, 
            List<string> assemblyRefs
            )
        {
            var assembly = generator.CreateAssembly(assemblyName, globalTable);
            Parallel.ForEach(asts, ast => assembly.Generate(ast, intermediate, assemblyPaths, assemblyRefs));
        }
    }
}
