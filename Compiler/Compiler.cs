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

        public bool Compile(
            string assemblyName,
            List<CompilationUnit> files,
            bool intermediate,
            bool debug,
            bool emitIr,
            List<string> assemblyPaths,
            List<string> assemblyRefs,
            string targetFramework
            )
        {
            // Add IONA SDK to the references
            assemblyRefs.Add("Iona.Builtins");
            assemblyPaths.Add(Environment.GetEnvironmentVariable("IONA_SDK_DIR") ?? "");
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
            
            var logger = ASTLoggerFactory.Create();
            var visualizer = ASTVisualizerFactory.Create();

            Parallel.ForEach(files, file =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Compiling {file.Name}");
                Console.ForegroundColor = ConsoleColor.White;
                var tokens = lexer.Tokenize(file.Source, file.Name);
                var ast = parser.Parse(tokens, assemblyName);

                // Auto-inject `use Iona.Builtins` so builtin types resolve without an explicit import
                if (ast is FileNode fileNode)
                {
                    var hasBuiltinsImport = fileNode.Children
                        .OfType<ImportNode>()
                        .Any(i => i.Name == "Iona.Builtins");

                    if (!hasBuiltinsImport)
                    {
                        fileNode.Children.Insert(0, new ImportNode("Iona.Builtins", fileNode));
                    }
                }

                asts.Add(ast);
            });

            typeck.AddImportedAssemblySymbols(globalTable, assemblyRefs);
            typeck.DoSemanticAnalysis(asts.OfType<FileNode>().ToList(), assemblyName, globalTable);

            if (debug)
            {
                Parallel.ForEach(asts, ast => logger.Log(ast));
            }
            
            if (errorCollector.Errors.Any())
            {
                foreach(var error in errorCollector.Errors)
                {
                    error.Log();
                }
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Compilation failed. See above errors for details.");
                
                Console.ResetColor();
                
                return false;
            }
            
            if (emitIr)
            {
                var lowering = new IR.IrLowering();
                var printer = new IR.IrPrinter();

                foreach (var ast in asts.OfType<FileNode>())
                {
                    var irModule = lowering.Build(ast);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(printer.Print(irModule));
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            GenerateCode(assemblyName, asts.ToList(), globalTable, intermediate, assemblyPaths, assemblyRefs, targetFramework);

            return true;
        }

        private void GenerateCode(
            string assemblyName, 
            List<INode> asts, 
            SymbolTable globalTable, 
            bool intermediate, 
            List<string> assemblyPaths, 
            List<string> assemblyRefs,
            string targetFramework
            )
        {
            var assembly = generator.CreateAssembly(assemblyName, globalTable);
            assembly.Generate(asts, intermediate, assemblyRefs, targetFramework);
        }
    }
}
