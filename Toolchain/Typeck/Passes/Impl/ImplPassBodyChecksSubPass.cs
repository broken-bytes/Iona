//|--- ImplPassBodyChecksSubPass.cs ----------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using AST.Visitors;
using Shared;
using Symbols;
using Symbols.Symbols;

namespace Typeck.Passes.Impl;

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
    IRecordVisitor,
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
                case GuardNode guardNode:
                    // For binding guards, resolve the expression and register the variable
                    if (guardNode.BindingExpression != null && guardNode.BindingName != null)
                    {
                        _expressionResolver.ResolveExpressionType(guardNode.BindingExpression, _symbolTable);

                        if (guardNode.BindingExpression is IExpressionNode bindExpr && bindExpr.ResultType != null)
                        {
                            // Resolve the type — for optionals, unwrap the inner type
                            var typeResult = _symbolTable.FindType(guardNode.Root, bindExpr.ResultType.FullyQualifiedName);
                            TypeSymbol bindingType;

                            if (typeResult.IsSuccess && typeResult.Unwrapped().IsOptional && typeResult.Unwrapped().InnerType != null)
                            {
                                bindingType = typeResult.Unwrapped().InnerType!;
                            }
                            else if (typeResult.IsSuccess)
                            {
                                bindingType = typeResult.Unwrapped();
                            }
                            else
                            {
                                bindingType = new TypeSymbol("Unknown", TypeKind.Unknown);
                            }

                            guardNode.BindingTypeNode = bindExpr.ResultType;

                            var variableSymbol = new VariableSymbol(guardNode.BindingName, bindingType)
                            {
                                IsMutable = guardNode.BindingIsMutable
                            };

                            if (_currentCallableScope != null)
                            {
                                _currentCallableScope.AddSymbol(variableSymbol);
                            }
                        }
                    }
                    guardNode.Body?.Accept(this);
                    break;
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
                case AwaitExpressionNode awaitExpr:
                    _expressionResolver.ResolveExpressionType(awaitExpr, _symbolTable);
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

        CheckContractConformance(classSymbol!, node.Name, node.Meta);

        node.Body?.Accept(this);

        _currentScope = null;
    }


    public void Visit(ContractNode node)
    {
        TypeSymbol? contractSymbol = null;

        var contractResult = _symbolTable.FindType(node.Root, node.FullyQualifiedName);

        if (contractResult.IsSuccess)
        {
            contractSymbol = contractResult.Unwrapped();
        }
        else
        {
            return;
        }

        _currentScope = contractSymbol;

        // Resolve refinements (contract inheritance)
        foreach (var refinementNode in node.Refinements)
        {
            if (refinementNode is TypeReferenceNode refinementRef)
            {
                var result = _symbolTable.FindType(node.Root, refinementRef.Name);

                if (result.IsSuccess)
                {
                    var refinedContract = result.Unwrapped();
                    refinementRef.FullyQualifiedName = refinedContract.FullyQualifiedName;
                    refinementRef.TypeKind = Utils.SymbolKindToASTKind(refinedContract.TypeKind);
                    refinementRef.Assembly = refinedContract.Assembly;

                    contractSymbol.Contracts.Add(refinedContract);
                }
            }
        }

        node.Body?.Accept(this);

        _currentScope = null;
    }

    public void Visit(EnumNode node)
    {
        // Enums don't have body checks yet
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
        else if (node.Parent is ModuleNode moduleNode)
        {
            // Free function: look up in the module symbol
            var moduleSymbol = _symbolTable.FindModuleByFQN(node.Root, moduleNode.Name);
            if (moduleSymbol != null)
            {
                _currentCallableScope = moduleSymbol.LookupAllSymbols(node.Name)
                    .OfType<FuncSymbol>().FirstOrDefault();
            }
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

        // Visit contracts first so refinements are resolved before conforming types check against them
        foreach (var child in node.Children.OfType<ContractNode>())
        {
            child.Accept(this);
        }

        foreach (var child in node.Children)
        {
            switch (child)
            {
                case ClassNode classNode:
                    classNode.Accept(this);
                    break;
                case ContractNode:
                    // Already visited above
                    break;
                case EnumNode enumNode:
                    enumNode.Accept(this);
                    break;
                case FuncNode funcNode:
                    funcNode.Accept(this);
                    break;
                case RecordNode recordNode:
                    recordNode.Accept(this);
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

    public void Visit(RecordNode node)
    {
        TypeSymbol? recordSymbol = null;

        var recordResult = _symbolTable.FindType(node.Root, node.FullyQualifiedName);

        if (recordResult.IsSuccess)
        {
            recordSymbol = recordResult.Unwrapped();
        }
        else
        {
            return;
        }

        _currentScope = recordSymbol;

        // Enforce record immutability: no var properties, no mut fn
        if (node.Body != null)
        {
            foreach (var prop in node.Body.Children.OfType<PropertyNode>())
            {
                if (prop.IsMutable)
                {
                    var error = CompilerErrorFactory.MutablePropertyInRecord(prop.Name, node.Name, prop.Meta);
                    _errorCollector.Collect(error);
                }
            }

            foreach (var func in node.Body.Children.OfType<FuncNode>())
            {
                if (func.IsMutable)
                {
                    var error = CompilerErrorFactory.MutatingFuncInRecord(func.Name, node.Name, func.Meta);
                    _errorCollector.Collect(error);
                }
            }
        }

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
                    var error = CompilerErrorFactory.ValueTypeCannotInheritClass(node.Name, typeSymbol.Name, node.Meta);
                    _errorCollector.Collect(error);
                }
                else
                {
                    recordSymbol!.Contracts.Add(typeSymbol!);
                }
            }
        }

        CheckContractConformance(recordSymbol!, node.Name, node.Meta);

        node.Body?.Accept(this);

        _currentScope = null;
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
                    var error = CompilerErrorFactory.ValueTypeCannotInheritClass(node.Name, typeSymbol.Name, node.Meta);
                    _errorCollector.Collect(error);
                }
                else
                {
                    structSymbol!.Contracts.Add(typeSymbol!);
                }
            }

        }

        CheckContractConformance(structSymbol!, node.Name, node.Meta);

        node.Body?.Accept(this);

        _currentScope = null;
    }

    public void Visit(VariableNode node)
    {
        node.Status = INode.ResolutionStatus.Resolving;
        var variableSymbol = new VariableSymbol(node.Name, new TypeSymbol("Unknown", TypeKind.Unknown));

        if (_currentScope is ModuleSymbol or null && _currentCallableScope == null)
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

            if (node.TypeNode is null)
            {
                node.TypeNode = node.Value.ResultType;
            }
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
            if (node.Value != null && (node.Value.Status == INode.ResolutionStatus.Failed || node.Value.ResultType == null))
            {
                var error = CompilerErrorFactory.CannotInferType(node.Name, node.Meta);

                _errorCollector.Collect(error);

                return;
            }

            var resultType = _symbolTable.FindType(node.Root, node.Value!.ResultType!.FullyQualifiedName);

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

        variableSymbol.IsMutable = node.IsMutable;

        // Propagate optional/weak flags from type annotation
        if (node.TypeNode is TypeReferenceNode varTypeRef)
        {
            variableSymbol.IsOptional = varTypeRef.IsOptional;
            variableSymbol.IsImplicitlyUnwrapped = varTypeRef.IsImplicitlyUnwrapped;
        }

        // Register the variable in the symbol table under the current callable scope
        if (_currentCallableScope != null)
        {
            _currentCallableScope.AddSymbol(variableSymbol);
        }

        node.Status = INode.ResolutionStatus.Resolved;
    }

    // -------------------------------------------------------------------
    //  Contract conformance checking
    // -------------------------------------------------------------------

    private void CheckContractConformance(TypeSymbol typeSymbol, string typeName, Metadata typeMeta)
    {
        var allContracts = GetAllContracts(typeSymbol);

        foreach (var contract in allContracts)
        {
            CheckContractMembers(typeSymbol, contract, typeName, typeMeta);
        }
    }

    private List<TypeSymbol> GetAllContracts(TypeSymbol typeSymbol)
    {
        var result = new HashSet<TypeSymbol>();
        var queue = new Queue<TypeSymbol>(typeSymbol.Contracts);

        while (queue.Count > 0)
        {
            var contract = queue.Dequeue();
            if (result.Add(contract))
            {
                foreach (var refined in contract.Contracts)
                {
                    queue.Enqueue(refined);
                }
            }
        }

        return result.ToList();
    }

    private void CheckContractMembers(
        TypeSymbol typeSymbol,
        TypeSymbol contractSymbol,
        string typeName,
        Metadata typeMeta)
    {
        var contractName = contractSymbol.Name;

        foreach (var requiredFunc in contractSymbol.Symbols.OfType<FuncSymbol>())
        {
            if (!TypeHasMatchingFunc(typeSymbol, requiredFunc))
            {
                var error = CompilerErrorFactory.ContractConformanceMissingFunc(
                    typeName, contractName, requiredFunc.Name, typeMeta);
                _errorCollector.Collect(error);
            }
        }

        foreach (var requiredProp in contractSymbol.Symbols.OfType<PropertySymbol>())
        {
            if (!TypeHasMatchingProperty(typeSymbol, requiredProp))
            {
                var error = CompilerErrorFactory.ContractConformanceMissingProperty(
                    typeName, contractName, requiredProp.Name, typeMeta);
                _errorCollector.Collect(error);
            }
        }

        foreach (var requiredOp in contractSymbol.Symbols.OfType<OperatorSymbol>())
        {
            if (!TypeHasMatchingOperator(typeSymbol, requiredOp))
            {
                var error = CompilerErrorFactory.ContractConformanceMissingOperator(
                    typeName, contractName, requiredOp.Name, typeMeta);
                _errorCollector.Collect(error);
            }
        }

        foreach (var requiredInit in contractSymbol.Symbols.OfType<InitSymbol>())
        {
            if (!TypeHasMatchingInit(typeSymbol, requiredInit))
            {
                var paramParts = requiredInit.Symbols.OfType<ParameterSymbol>()
                    .Select(p => $"{p.Name}: {p.Type.Name}");
                var initSig = $"init({string.Join(", ", paramParts)})";
                var error = CompilerErrorFactory.ContractConformanceMissingInit(
                    typeName, contractName, initSig, typeMeta);
                _errorCollector.Collect(error);
            }
        }
    }

    private bool TypeHasMatchingFunc(TypeSymbol type, FuncSymbol required)
    {
        var candidates = type.LookupAllSymbols(required.Name).OfType<FuncSymbol>();

        foreach (var candidate in candidates)
        {
            if (FuncSignaturesMatch(candidate, required))
                return true;
        }

        if (type.BaseType != null)
            return TypeHasMatchingFunc(type.BaseType, required);

        return false;
    }

    private static bool FuncSignaturesMatch(FuncSymbol candidate, FuncSymbol required)
    {
        var candidateParams = candidate.Symbols.OfType<ParameterSymbol>().ToList();
        var requiredParams = required.Symbols.OfType<ParameterSymbol>().ToList();

        if (candidateParams.Count != requiredParams.Count)
            return false;

        for (int i = 0; i < candidateParams.Count; i++)
        {
            if (candidateParams[i].Name != requiredParams[i].Name)
                return false;

            // Skip unresolved types to avoid cascading errors
            if (candidateParams[i].Type.TypeKind == TypeKind.Unknown ||
                requiredParams[i].Type.TypeKind == TypeKind.Unknown)
                continue;

            if (candidateParams[i].Type.FullyQualifiedName != requiredParams[i].Type.FullyQualifiedName)
                return false;
        }

        // Check return type (skip if either is unresolved)
        if (candidate.ReturnType.TypeKind != TypeKind.Unknown &&
            required.ReturnType.TypeKind != TypeKind.Unknown &&
            candidate.ReturnType.FullyQualifiedName != required.ReturnType.FullyQualifiedName)
            return false;

        return true;
    }

    private bool TypeHasMatchingProperty(TypeSymbol type, PropertySymbol required)
    {
        var candidates = type.LookupAllSymbols(required.Name).OfType<PropertySymbol>();

        foreach (var candidate in candidates)
        {
            // Skip unresolved types
            if (candidate.Type.TypeKind == TypeKind.Unknown || required.Type.TypeKind == TypeKind.Unknown)
                return true;

            if (candidate.Type.FullyQualifiedName == required.Type.FullyQualifiedName)
                return true;
        }

        if (type.BaseType != null)
            return TypeHasMatchingProperty(type.BaseType, required);

        return false;
    }

    private bool TypeHasMatchingOperator(TypeSymbol type, OperatorSymbol required)
    {
        var candidates = type.Symbols.OfType<OperatorSymbol>()
            .Where(o => o.Operator == required.Operator);

        foreach (var candidate in candidates)
        {
            var candidateParams = candidate.Symbols.OfType<ParameterSymbol>().ToList();
            var requiredParams = required.Symbols.OfType<ParameterSymbol>().ToList();

            if (candidateParams.Count != requiredParams.Count)
                continue;

            bool match = true;
            for (int i = 0; i < candidateParams.Count; i++)
            {
                if (candidateParams[i].Type.TypeKind != TypeKind.Unknown &&
                    requiredParams[i].Type.TypeKind != TypeKind.Unknown &&
                    candidateParams[i].Type.FullyQualifiedName != requiredParams[i].Type.FullyQualifiedName)
                {
                    match = false;
                    break;
                }
            }

            if (match &&
                (candidate.ReturnType.TypeKind == TypeKind.Unknown ||
                 required.ReturnType.TypeKind == TypeKind.Unknown ||
                 candidate.ReturnType.FullyQualifiedName == required.ReturnType.FullyQualifiedName))
                return true;
        }

        if (type.BaseType != null)
            return TypeHasMatchingOperator(type.BaseType, required);

        return false;
    }

    private static bool TypeHasMatchingInit(TypeSymbol type, InitSymbol required)
    {
        var candidates = type.Symbols.OfType<InitSymbol>();

        foreach (var candidate in candidates)
        {
            var candidateParams = candidate.Symbols.OfType<ParameterSymbol>().ToList();
            var requiredParams = required.Symbols.OfType<ParameterSymbol>().ToList();

            if (candidateParams.Count != requiredParams.Count)
                continue;

            bool match = true;
            for (int i = 0; i < candidateParams.Count; i++)
            {
                if (candidateParams[i].Name != requiredParams[i].Name)
                {
                    match = false;
                    break;
                }

                if (candidateParams[i].Type.TypeKind != TypeKind.Unknown &&
                    requiredParams[i].Type.TypeKind != TypeKind.Unknown &&
                    candidateParams[i].Type.FullyQualifiedName != requiredParams[i].Type.FullyQualifiedName)
                {
                    match = false;
                    break;
                }
            }

            if (match) return true;
        }

        return false;
    }
}