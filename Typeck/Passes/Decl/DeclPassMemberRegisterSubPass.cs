using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Symbols;
using Symbols.Symbols;

namespace Typeck.Passes.Decl;

public class DeclPassMemberRegisterSubPass : 
    ISemanticAnalysisPass, 
    IBlockVisitor,
    IClassVisitor, 
    IContractVisitor,
    IEnumVisitor, 
    IFileVisitor, 
    IFuncVisitor, 
    IInitVisitor,
    IModuleVisitor,
    IOperatorVisitor,
    IPropertyVisitor,
    IStructVisitor
{
    private SymbolTable _symbolTable = new();
    private ISymbol? _currentSymbol;
    private string _assemblyName = "";
    
    internal DeclPassMemberRegisterSubPass() {}
    
    public void Run(FileNode root, SymbolTable table, string assemblyName)
    {
        _symbolTable = table;
        _assemblyName = assemblyName;
        
        root.Accept(this);
    }

    public void Visit(BlockNode node)
    {
        foreach(var func in node.Children.OfType<FuncNode>())
        {
            func.Accept(this);
        }

        foreach (var prop in node.Children.OfType<PropertyNode>())
        {
            prop.Accept(this);
        }
    }

    public void Visit(ClassNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = new TypeSymbol(node.Name, TypeKind.Class);
        
        _currentSymbol.Symbols.Add(symbol);
        symbol.Parent = _currentSymbol;
        
        _currentSymbol = symbol;

        node.Body?.Accept(this);
    }
    
    public void Visit(ContractNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = new TypeSymbol(node.Name, TypeKind.Contract);
        
        _currentSymbol.Symbols.Add(symbol);
        symbol.Parent = _currentSymbol;
        
        _currentSymbol = symbol;

        node.Body?.Accept(this);
    }

    public void Visit(EnumNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = new TypeSymbol(node.Name, TypeKind.Enum);
        
        _currentSymbol.Symbols.Add(symbol);
        symbol.Parent = _currentSymbol;
        
        _currentSymbol = symbol;

        node.Body?.Accept(this);
    }

    public void Visit(FileNode node)
    {
        foreach (var module in node.Children.OfType<ModuleNode>())
        {
            module.Accept(this);
        }
    }

    public void Visit(FuncNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }
        
        var csharpName = Shared.Utils.IonaToCSharpName(node.Name);
        var symbol = new FuncSymbol(node.Name, csharpName);
        _currentSymbol.Symbols.Add(symbol);
        symbol.Parent = _currentSymbol;
    }
    
    public void Visit(InitNode node)
    {
        if (_currentSymbol is not TypeSymbol typeSymbol)
        {
            return;
        }

        var symbol = new InitSymbol
        {
            ReturnType = typeSymbol
        };
        
        _currentSymbol.Symbols.Add(symbol);
        symbol.Parent = _currentSymbol;
    }

    public void Visit(ModuleNode node)
    {
        var symbol = _symbolTable.Modules.Find(module => module.Name == node.Name);

        if (symbol == null)
        {
            symbol = new ModuleSymbol(node.Name, _assemblyName);
            _symbolTable.Modules.Add(symbol);
        }

        _currentSymbol = symbol;
        
        foreach (var type in node.Children.OfType<ITypeNode>())
        {
            switch (type)
            {
                case ClassNode classNode:
                    classNode.Accept(this);
                    break;
                case ContractNode contractNode:
                    contractNode.Accept(this);
                    break;
                case StructNode structNode:
                    structNode.Accept(this);
                    break;
            }
        }

        foreach (var func in node.Children.OfType<FuncNode>())
        {
            func.Accept(this);
        }
    }
    
    public void Visit(OperatorNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = new OperatorSymbol(node.Op);
        _currentSymbol.Symbols.Add(symbol);
        symbol.Parent = _currentSymbol;
    }

    public void Visit(PropertyNode node)
    {
        string cSharpName = Shared.Utils.IonaToCSharpName(node.Name);
        if (node.AccessLevel == AccessLevel.Private)
        {
            cSharpName = $"_{node.Name}";
        }
        // TODO: Add actual get set access levels instead of public per default
        var symbol = new PropertySymbol(
            node.Name,
            cSharpName, 
            new TypeSymbol("Unknown", TypeKind.Unknown),
            false,
            true,
            AccessLevel.Public,
            AccessLevel.Public
        );

        if (_currentSymbol == null)
        {
            return;
        }

        if (node.TypeNode is { } type)
        {
            symbol.Type = new TypeSymbol(type.Name, TypeKind.Unknown);
        }

        _currentSymbol.Symbols.Add(symbol);
        symbol.Parent = _currentSymbol;
    }
    
    public void Visit(StructNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = new TypeSymbol(node.Name, TypeKind.Struct);
        
        _currentSymbol.Symbols.Add(symbol);
        symbol.Parent = _currentSymbol;
        
        _currentSymbol = symbol;

        node.Body?.Accept(this);
    }
}