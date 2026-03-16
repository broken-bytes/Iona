using AST.Nodes;
using AST.Types;
using Lexer;
using Parser;
using Shared;
using Symbols;
using Symbols.Symbols;
using Typeck;

namespace Tests.Helpers;

/// <summary>
/// Provides utilities for building test ASTs and symbol tables without needing
/// the real Iona.Builtins assembly.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a SymbolTable pre-populated with all Iona builtin types and operators.
    /// </summary>
    public static SymbolTable CreateBuiltinsSymbolTable()
    {
        var table = new SymbolTable();
        BuiltinTypeRegistrar.RegisterBuiltins(table);
        return table;
    }

    /// <summary>
    /// Lexes and parses Iona source code, returning the FileNode AST.
    /// Automatically injects `use Iona.Builtins` import.
    /// </summary>
    public static FileNode ParseSource(string source, string assemblyName = "TestAssembly")
    {
        var errorCollector = ErrorCollectorFactory.Create();
        var warningCollector = WarningCollectorFactory.Create();
        var fixItCollector = FixItCollectorFactory.Create();

        var lexer = LexerFactory.Create(errorCollector, warningCollector, fixItCollector);
        var parser = ParserFactory.Create(errorCollector, warningCollector, fixItCollector);

        var tokens = lexer.Tokenize(source, "test.iona");
        var ast = parser.Parse(tokens, assemblyName);

        var fileNode = (FileNode)ast;

        // Auto-inject builtins import (same as Compiler does)
        var hasBuiltinsImport = fileNode.Children
            .OfType<ImportNode>()
            .Any(i => i.Name == "Iona.Builtins");

        if (!hasBuiltinsImport)
        {
            fileNode.Children.Insert(0, new ImportNode("Iona.Builtins", fileNode));
        }

        return fileNode;
    }

    /// <summary>
    /// Creates a full type-checking pipeline and runs semantic analysis.
    /// Returns (errorCollector, symbolTable) so tests can inspect results.
    /// </summary>
    public static (IErrorCollector errors, SymbolTable table) RunTypeck(
        string source,
        string assemblyName = "TestAssembly")
    {
        var errorCollector = ErrorCollectorFactory.Create();
        var warningCollector = WarningCollectorFactory.Create();
        var fixItCollector = FixItCollectorFactory.Create();

        var typeck = TypeckFactory.Create(errorCollector, warningCollector, fixItCollector);

        var fileNode = ParseSource(source, assemblyName);
        var table = CreateBuiltinsSymbolTable();

        typeck.DoSemanticAnalysis([fileNode], assemblyName, table);

        return (errorCollector, table);
    }

    /// <summary>
    /// Runs typeck and also returns the parsed FileNode for AST inspection.
    /// </summary>
    public static (IErrorCollector errors, SymbolTable table, FileNode ast) RunTypeckWithAst(
        string source,
        string assemblyName = "TestAssembly")
    {
        var errorCollector = ErrorCollectorFactory.Create();
        var warningCollector = WarningCollectorFactory.Create();
        var fixItCollector = FixItCollectorFactory.Create();

        var typeck = TypeckFactory.Create(errorCollector, warningCollector, fixItCollector);

        var fileNode = ParseSource(source, assemblyName);
        var table = CreateBuiltinsSymbolTable();

        typeck.DoSemanticAnalysis([fileNode], assemblyName, table);

        return (errorCollector, table, fileNode);
    }
}
