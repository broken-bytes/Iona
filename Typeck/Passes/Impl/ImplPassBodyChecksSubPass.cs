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
    IStructVisitor,
    IVariableVisitor
{
    private readonly IErrorCollector _errorCollector;
    private readonly ExpressionResolver _expressionResolver;
    private SymbolTable _symbolTable;
    private ISymbol? _currentScope;
    private ISymbol? _currentCallableScope;

    internal ImplPassBodyChecksSubPass(
        IErrorCollector errorCollector,
        ExpressionResolver expressionResolver
    )
    {
        _errorCollector = errorCollector;
        _expressionResolver = expressionResolver;
        _symbolTable = new SymbolTable();
    }
    
    public void Run(List<FileNode> files, SymbolTable table, string assemblyName)
    {
        _symbolTable = table;
        
        foreach (var file in files)
        {
            file.Accept(this);
        }
    }
    
    public void Visit(BlockNode node)
    {
        node.Status = INode.ResolutionStatus.Resolving;

        foreach (var child in node.Children)
        {
            switch (child)
            {
                case IfNode ifNode:
                    ifNode.Body?.Accept(this);
                    foreach (var clause in ifNode.ElseClauses)
                    {
                        clause.Body?.Accept(this);
                    }
                    break;
                case WhileNode whileNode:
                    whileNode.Body?.Accept(this);
                    break;
                case ForNode forNode:
                    // Register the iterator in the expression resolver's loop scope
                    if (forNode.IteratorName != "_")
                    {
                        TypeSymbol? iteratorType = null;

                        if (forNode.Iterable is RangeExpressionNode range)
                        {
                            _expressionResolver.ResolveExpressionType(range.Start, _symbolTable);
                            if (range.Start.ResultType != null)
                            {
                                var typeResult = _symbolTable.FindType(forNode.Root, range.Start.ResultType.FullyQualifiedName);
                                if (typeResult.IsSuccess)
                                {
                                    iteratorType = typeResult.Unwrapped();
                                }
                            }
                        }

                        iteratorType ??= new TypeSymbol("Unknown", TypeKind.Unknown);
                        _expressionResolver.EnterLoopScope(forNode.IteratorName, iteratorType);
                    }
                    forNode.Body?.Accept(this);
                    if (forNode.IteratorName != "_")
                    {
                        _expressionResolver.ExitLoopScope();
                    }
                    break;
                case FuncNode funcNode:
                    funcNode.Accept(this);
                    break;
                case InitNode initNode:
                    initNode.Accept(this);
                    break;
                case OperatorNode opNode:
                    opNode.Accept(this);
                    break;
                case VariableNode varNode:
                    varNode.Accept(this);
                    break;
                default:
                    break;
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

        _currentScope = classSymbol;
            
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
        
        _currentScope = null;
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

        // Track the function scope so variables can be registered under it
        var previousCallableScope = _currentCallableScope;
        if (_currentScope != null)
        {
            _currentCallableScope = _currentScope.LookupAllSymbols(node.Name)
                .OfType<FuncSymbol>().FirstOrDefault();
        }

        var isResolved = node.Parameters.TrueForAll(p => p.TypeNode.Status == INode.ResolutionStatus.Resolved);

        if (node.Body != null)
        {
            node.Body.Accept(this);
            isResolved &= node.Body.Status == INode.ResolutionStatus.Resolved;
        }

        _currentCallableScope = previousCallableScope;

        if (isResolved)
        {
            node.Status = INode.ResolutionStatus.Resolved;
        }
    }

    public void Visit(InitNode node)
    {
        var previousCallableScope = _currentCallableScope;
        if (_currentScope is TypeSymbol typeSymbol)
        {
            _currentCallableScope = typeSymbol.LookupAllSymbols("init").OfType<InitSymbol>().FirstOrDefault();
        }

        node.Body?.Accept(this);

        _currentCallableScope = previousCallableScope;
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

        var previousCallableScope = _currentCallableScope;
        if (_currentScope != null)
        {
            _currentCallableScope = _currentScope.Symbols.OfType<OperatorSymbol>()
                .FirstOrDefault(o => o.Operator == node.Op);
        }

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

        _currentCallableScope = previousCallableScope;
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
        
        _currentScope = structSymbol;
        
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

        _currentScope = null;
    }

    public void Visit(VariableNode node)
    {
        node.Status = INode.ResolutionStatus.Resolving;
        var variableSymbol = new VariableSymbol(node.Name, new TypeSymbol("Unknown", TypeKind.Unknown));

        if (_currentScope is ModuleSymbol or null)
        {
            var error = CompilerErrorFactory.VariableNotAllowedInTopLevel(node.Meta);

            _errorCollector.Collect(error);

            node.Status = INode.ResolutionStatus.Failed;
            
            return;
        }
        
        // Type inference
        if (node.Value != null)
        {
            // Resolve the value
            _expressionResolver.ResolveExpressionType(node.Value, _symbolTable);

            node.TypeNode = node.Value.ResultType;
        }

        if (node.TypeNode is null && node.Value is null)
        {
            node.Status = INode.ResolutionStatus.Failed;

            var error = CompilerErrorFactory.MissingTypeAnnotation(node.Name, node.Meta);
            _errorCollector.Collect(error);
                
            return;
        }
        
        if (node.TypeNode is not null)
        {
            var type = _symbolTable.FindType(node.Root, node.TypeNode.FullyQualifiedName);

            if (type.IsSuccess)
            {
                variableSymbol.Type = type.Unwrapped();
            }
            else
            {
                var error = CompilerErrorFactory.TopLevelDefinitionError(node.TypeNode.FullyQualifiedName, node.TypeNode.Meta);
                
                _errorCollector.Collect(error);
                
                node.Status = INode.ResolutionStatus.Failed;
                
                return;
            }
        }
        else
        {
            if (node.Value != null && node.Value.Status == INode.ResolutionStatus.Failed)
            {
                var error = CompilerErrorFactory.CannotInferType(node.Name, node.Meta);
                    
                _errorCollector.Collect(error);
                    
                return;
            }
                
            var resultType = _symbolTable.FindType(node.Root, node.Value.ResultType.FullyQualifiedName);

            if (resultType.IsSuccess)
            {
                variableSymbol.Type = resultType.Unwrapped();
            }
            else
            {
                var error = CompilerErrorFactory.CannotInferType(node.Name, node.Meta);
                    
                _errorCollector.Collect(error);
                    
                return;
            }
        }

        // Register the variable in the symbol table under the current callable scope
        if (_currentCallableScope != null)
        {
            _currentCallableScope.AddSymbol(variableSymbol);
        }

        node.Status = INode.ResolutionStatus.Resolved;
    }
}