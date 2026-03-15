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
    /// Creates a SymbolTable pre-populated with Iona.Builtins types (Int32, String, Bool, Double, Float)
    /// with arithmetic operators on Int32.
    /// </summary>
    public static SymbolTable CreateBuiltinsSymbolTable()
    {
        var table = new SymbolTable();

        // Create module hierarchy: Iona -> Builtins
        var ionaModule = new ModuleSymbol("Iona", "Iona.Builtins");
        table.AddModule(ionaModule);

        var builtinsModule = new ModuleSymbol("Builtins", "Iona.Builtins");
        ionaModule.AddSymbol(builtinsModule);

        // Int32
        var int32Type = new TypeSymbol("Int32", TypeKind.Struct);
        builtinsModule.AddSymbol(int32Type);

        // Add arithmetic operators to Int32: +, -, *, /
        AddBinaryOperator(int32Type, OperatorType.Add, int32Type);
        AddBinaryOperator(int32Type, OperatorType.Subtract, int32Type);
        AddBinaryOperator(int32Type, OperatorType.Multiply, int32Type);
        AddBinaryOperator(int32Type, OperatorType.Divide, int32Type);

        // Add comparison operators to Int32
        var boolType = new TypeSymbol("Bool", TypeKind.Struct);
        builtinsModule.AddSymbol(boolType);

        AddBinaryOperator(int32Type, OperatorType.Equal, boolType);
        AddBinaryOperator(int32Type, OperatorType.LessThan, boolType);
        AddBinaryOperator(int32Type, OperatorType.GreaterThan, boolType);

        // String
        var stringType = new TypeSymbol("String", TypeKind.Class);
        builtinsModule.AddSymbol(stringType);
        AddBinaryOperator(stringType, OperatorType.Add, stringType);

        // Double
        var doubleType = new TypeSymbol("Double", TypeKind.Struct);
        builtinsModule.AddSymbol(doubleType);
        AddBinaryOperator(doubleType, OperatorType.Add, doubleType);

        // Float
        var floatType = new TypeSymbol("Float", TypeKind.Struct);
        builtinsModule.AddSymbol(floatType);
        AddBinaryOperator(floatType, OperatorType.Add, floatType);

        return table;
    }

    private static void AddBinaryOperator(TypeSymbol ownerType, OperatorType opType, TypeSymbol returnType)
    {
        var op = new OperatorSymbol(opType);
        op.ReturnType = returnType;
        op.Parent = ownerType;

        var leftParam = new ParameterSymbol("left", ownerType, op);
        var rightParam = new ParameterSymbol("right", ownerType, op);
        op.AddSymbol(leftParam);
        op.AddSymbol(rightParam);

        ownerType.AddSymbol(op);
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
