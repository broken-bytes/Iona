//|--- TestHelpers.cs ------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using AST.Types;
using Lexer;
using Parser;
using Shared;
using Symbols;
using Symbols.Symbols;
using Typeck;

namespace Tests.Helpers;

public static class TestHelpers
{
    public static SymbolTable CreateBuiltinsSymbolTable()
    {
        var table = new SymbolTable();
        BuiltinTypeRegistrar.RegisterBuiltins(table);
        return table;
    }

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
