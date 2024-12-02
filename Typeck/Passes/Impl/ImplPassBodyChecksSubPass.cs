using AST.Nodes;
using AST.Visitors;
using Shared;
using Symbols;
using Symbols.Symbols;

namespace Typeck.Passes.Impl;

/// <summary>
/// This pass checks all implementations of bodies (class, function, etc)
/// </summary>
public class ImplPassBodyChecksSubPass :
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
    IStructVisitor
{
    private SymbolTable _symbolTable;

    internal ImplPassBodyChecksSubPass()
    {
        _symbolTable = new SymbolTable();
    }
    
    public void Run(FileNode root, SymbolTable table, string assemblyName)
    {
        _symbolTable = table;
        root.Accept(this);
    }
    
    public void Visit(BlockNode node)
    {
        node.Status = INode.ResolutionStatus.Resolving;

        foreach (var child in node.Children)
        {
            if (child is FuncNode func)
            {
                func.Body?.Accept(this);
            }
        }
    }

    public void Visit(ClassNode node)
    {
        TypeSymbol? classSymbol = null;

        var classResult = _symbolTable.FindType(node.Root, node.FullyQualifiedName);

        if (classResult.IsSuccess)
        {
            classSymbol = classResult.Unwrapped();
        }
        else
        {
            return;
        }
        
        // For each contract this class conforms to, check if one of them is in fact a class and make it the base type
        foreach (var contract in node.Contracts)
        {
            TypeSymbol? typeSymbol = null;

            var result = _symbolTable.FindType(node.Root, contract.Name);

            if (result.IsSuccess)
            {
                typeSymbol = result.Unwrapped();
            }
            else
            {
                return;
            }

            contract.FullyQualifiedName = typeSymbol.FullyQualifiedName;
            contract.TypeKind = Utils.SymbolKindToASTKind(typeSymbol.TypeKind);
            contract.Assembly = typeSymbol.Assembly;

            var contractSymbol = _symbolTable.FindType(node.Root, contract.FullyQualifiedName);

            if (contractSymbol.IsSuccess)
            {
                if (typeSymbol.TypeKind == TypeKind.Class)
                {
                    node.BaseType = contract;
                    classSymbol!.BaseType = typeSymbol;
                }
                else
                {
                    classSymbol!.Contracts.Add(typeSymbol!);
                }
            }
        
        }

        if (node.BaseType != null)
        {
            node.Contracts.Remove(node.BaseType);
        }

        node.Body?.Accept(this);
    }


    public void Visit(ContractNode node)
    {
        throw new NotImplementedException();
    }

    public void Visit(EnumNode node)
    {
        throw new NotImplementedException();
    }

    public void Visit(FileNode node)
    {
        node.Status = INode.ResolutionStatus.Resolving;

        foreach (var child in node.Children)
        {
            if (child is ModuleNode module)
            {
                module.Accept(this);
            }
        }

        node.Status = INode.ResolutionStatus.Resolved;
    }

    public void Visit(FuncNode node)
    {
        node.Status = INode.ResolutionStatus.Resolving;
        
        if (node.ReturnType is not null)
        {
            var symbol = _symbolTable.FindType(node.Root, node.ReturnType.FullyQualifiedName);

            if (symbol.IsSuccess)
            {
                node.ReturnType.FullyQualifiedName = symbol.Success!.FullyQualifiedName;
                node.ReturnType.TypeKind = Utils.SymbolKindToASTKind(symbol.Success!.TypeKind);
            }
            else
            {
                // TODO: Show error
            }
        }

        var isResolved = node.Parameters.TrueForAll(p => p.TypeNode.Status == INode.ResolutionStatus.Resolved);

        if (node.Body != null)
        {
            node.Body.Accept(this);
            isResolved &= node.Body.Status == INode.ResolutionStatus.Resolved;
        }

        if (isResolved)
        {
            node.Status = INode.ResolutionStatus.Resolved;
        }
    }

    public void Visit(InitNode node)
    {
        node.Body?.Accept(this);
    }
    
    public void Visit(ModuleNode node)
    {
        node.Status = INode.ResolutionStatus.Resolving;

        foreach (var child in node.Children)
        {
            switch (child)
            {
                case ClassNode classNode:
                    classNode.Accept(this);
                    break;
                case ContractNode contractNode:
                    contractNode.Accept(this);
                    break;
                case EnumNode enumNode:
                    enumNode.Accept(this);
                    break;
                case FuncNode funcNode:
                    funcNode.Accept(this);
                    break;
                case StructNode structNode:
                    structNode.Accept(this);
                    break;
            }
        }
    }

    public void Visit(OperatorNode node)
    {
        node.Status = INode.ResolutionStatus.Resolving;
        

        if (node.ReturnType is not null)
        {
            var symbol = _symbolTable.FindType(node.Root, node.ReturnType.FullyQualifiedName);

            if (symbol.IsSuccess)
            {
                node.ReturnType.FullyQualifiedName = symbol.Success!.FullyQualifiedName;
                node.ReturnType.TypeKind = Utils.SymbolKindToASTKind(symbol.Success!.TypeKind);
            }
            else
            {
                // TODO: Show error
            }
        }
        
        node.Body?.Accept(this);
    }

    public void Visit(StructNode node)
    {
        TypeSymbol? structSymbol = null;

        var structResult = _symbolTable.FindType(node.Root, node.FullyQualifiedName);

        if (structResult.IsSuccess)
        {
            structSymbol = structResult.Unwrapped();
        }
        else
        {
            return;
        }
        
        // For each contract this class conforms to, check if one of them is in fact a class and make it the base type
        foreach (var contract in node.Contracts)
        {
            TypeSymbol? typeSymbol = null;

            var result = _symbolTable.FindType(node.Root, contract.Name);

            if (result.IsSuccess)
            {
                typeSymbol = result.Unwrapped();
            }
            else
            {
                return;
            }

            contract.FullyQualifiedName = typeSymbol.FullyQualifiedName;
            contract.TypeKind = Utils.SymbolKindToASTKind(typeSymbol.TypeKind);
            contract.Assembly = typeSymbol.Assembly;

            var contractSymbol = _symbolTable.FindType(node.Root, contract.FullyQualifiedName);

            if (contractSymbol.IsSuccess)
            {
                if (typeSymbol.TypeKind == TypeKind.Class)
                {
                    // TODO: Show error because struct cannot inherit from classes
                }
                else
                {
                    structSymbol!.Contracts.Add(typeSymbol!);
                }
            }
        
        }

        node.Body?.Accept(this);
    }
}