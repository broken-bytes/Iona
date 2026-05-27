//|--- DeclPassMemberRegisterSubPass.cs ------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

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
    IRecordVisitor,
    IStructVisitor
{
    private SymbolTable _symbolTable = new();
    private ISymbol? _currentSymbol;
    private string _assemblyName = "";
    
    internal DeclPassMemberRegisterSubPass() {}
    
    public void Run(List<FileNode> files, SymbolTable table, string assemblyName)
    {
        _symbolTable = table;
        _assemblyName = assemblyName;

        foreach (var file in files)
        {
            file.Accept(this);
        }
    }

    public void Visit(BlockNode node)
    {
        foreach(var func in node.Children.OfType<FuncNode>())
        {
            func.Accept(this);
        }

        foreach (var init in node.Children.OfType<InitNode>())
        {
            init.Accept(this);
        }

        foreach (var op in node.Children.OfType<OperatorNode>())
        {
            op.Accept(this);
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

        _currentSymbol.AddSymbol(symbol);

        var previous = _currentSymbol;
        _currentSymbol = symbol;

        node.Body?.Accept(this);

        _currentSymbol = previous;
    }
    
    public void Visit(ContractNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = new TypeSymbol(node.Name, TypeKind.Contract);

        _currentSymbol.AddSymbol(symbol);

        var previous = _currentSymbol;
        _currentSymbol = symbol;

        node.Body?.Accept(this);

        _currentSymbol = previous;
    }

    public void Visit(EnumNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = new TypeSymbol(node.Name, TypeKind.Enum);

        _currentSymbol.AddSymbol(symbol);

        var previous = _currentSymbol;
        _currentSymbol = symbol;

        node.Body?.Accept(this);

        _currentSymbol = previous;
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
        symbol.AccessLevel = node.AccessLevel;
        symbol.IsMutating = node.IsMutable;
        symbol.IsAsync = node.IsAsync;

        RegisterParameters(symbol, node.Parameters);

        foreach (var generic in node.GenericArguments)
        {
            var genericSymbol = new GenericParameterSymbol(generic.Name);
            symbol.AddSymbol(genericSymbol);
        }
        
        _currentSymbol.AddSymbol(symbol);
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
        
        RegisterParameters(symbol, node.Parameters);
        
        _currentSymbol.AddSymbol(symbol);
    }

    public void Visit(ModuleNode node)
    {
        _symbolTable.ModulesByName.TryGetValue(node.Name, out var symbol);

        if (symbol == null)
        {
            symbol = new ModuleSymbol(node.Name, _assemblyName);
            _symbolTable.AddModule(symbol);
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
                case RecordNode recordNode:
                    recordNode.Accept(this);
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

    public void Visit(RecordNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = new TypeSymbol(node.Name, TypeKind.Record);

        _currentSymbol.AddSymbol(symbol);

        var previous = _currentSymbol;
        _currentSymbol = symbol;

        node.Body?.Accept(this);

        _currentSymbol = previous;
    }
    
    public void Visit(OperatorNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = new OperatorSymbol(node.Op);

        RegisterParameters(symbol, node.Parameters);
        
        _currentSymbol.AddSymbol(symbol);
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

        _currentSymbol.AddSymbol(symbol);
    }
    
    public void Visit(StructNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = new TypeSymbol(node.Name, TypeKind.Struct);

        _currentSymbol.AddSymbol(symbol);

        var previous = _currentSymbol;
        _currentSymbol = symbol;

        node.Body?.Accept(this);

        _currentSymbol = previous;
    }

    private void RegisterParameters(ISymbol symbol, List<ParameterNode> parameters)
    {
        foreach (var param in parameters)
        {
            var paramType = new TypeSymbol(param.TypeNode.Name, TypeKind.Unknown);
            var paramSymbol = new ParameterSymbol(param.Name, paramType, symbol)
            {
                IsOptional = param.TypeNode.IsOptional || param.TypeNode.IsImplicitlyUnwrapped
            };

            symbol.AddSymbol(paramSymbol);
        }
    }
}