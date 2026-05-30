//|--- DeclPassMemberReferenceResolveSubPass.cs ----------------|
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
using Shared;
using Symbols;
using Symbols.Symbols;

namespace Typeck.Passes.Decl;

public class DeclPassMemberReferenceResolveSubPass : 
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
    private readonly IErrorCollector _errorCollector;
    private readonly ExpressionResolver _expressionResolver;
    private SymbolTable _symbolTable = new();
    private ISymbol? _currentSymbol;
    private string _assemblyName = "";

    internal DeclPassMemberReferenceResolveSubPass(IErrorCollector errorCollector, ExpressionResolver expressionResolver)
    {
        _errorCollector = errorCollector;
        _expressionResolver = expressionResolver;
    }
    
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

        var symbol = _symbolTable.FindTypeByFQN(node.FullyQualifiedName);
        if (symbol != null) { ResolveGenericConstraints(symbol, node.GenericArguments, node.Root); }

        var previous = _currentSymbol;
        _currentSymbol = symbol;

        node.Body?.Accept(this);

        _currentSymbol = previous;
    }

    // Walk the symbol's GenericParameterSymbol children and bind each to its declared
    // constraint types. The constraint AST nodes were left as bare TypeReferenceNodes during
    // parsing; resolution to a TypeSymbol happens here, once the symbol table is populated.
    // A constraint whose name matches another sibling generic parameter (`where T: U`) is
    // stored as a placeholder TypeSymbol with TypeKind.Generic so call-site validation can
    // look up the supplied type for the sibling and route the check through it.
    private void ResolveGenericConstraints(ISymbol owner, List<GenericArgument> astArgs, FileNode rootFile)
    {
        var siblingNames = astArgs.Select(a => a.Name).ToHashSet();
        foreach (var ga in astArgs)
        {
            if (ga.Constraints.Count == 0) { continue; }
            var paramSym = owner.Symbols.OfType<GenericParameterSymbol>().FirstOrDefault(p => p.Name == ga.Name);
            if (paramSym == null) { continue; }
            foreach (var constraintRef in ga.Constraints)
            {
                if (siblingNames.Contains(constraintRef.Name))
                {
                    // Sibling-generic reference — the supplied type at the matching position
                    // will be looked up at call sites.
                    paramSym.Constraints.Add(new TypeSymbol(constraintRef.Name, TypeKind.Generic));
                    continue;
                }
                var resolved = _symbolTable.FindTypeBy(rootFile, constraintRef, null);
                if (resolved != null)
                {
                    paramSym.Constraints.Add(resolved);
                }
            }
        }
    }

    public void Visit(ContractNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = _symbolTable.FindTypeByFQN(node.FullyQualifiedName);
        if (symbol != null) { ResolveGenericConstraints(symbol, node.GenericArguments, node.Root); }

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

        var symbol = _symbolTable.FindTypeByFQN(node.FullyQualifiedName);

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
        
        var symbol = _currentSymbol.Symbols.OfType<FuncSymbol>().FirstOrDefault(func =>
        {
            if (func.Name != node.Name)
            {
                return false;
            }

            if (func.Symbols.OfType<ParameterSymbol>().Count() != node.Parameters.Count)
            {
                return false;
            }

            foreach (var (nParam, sParam) in node.Parameters.Zip(func.Symbols.OfType<ParameterSymbol>()))
            {
                if (sParam.Name != nParam.Name)
                {
                    return false;
                }
            }

            return true;
        });

        if (symbol == null) return;

        ResolveGenericConstraints(symbol, node.GenericArguments, node.Root);

        foreach (var old in symbol.Symbols.OfType<ParameterSymbol>().ToList())
        {
            symbol.Symbols.Remove(old);
            if (symbol.SymbolsByName.TryGetValue(old.Name, out var byName))
            {
                byName.Remove(old);
            }
        }

        foreach (var param in node.Parameters)
        {
            ParameterSymbol parameter;
            if (param.TypeNode is FunctionTypeNode)
            {
                // Function-typed params don't sit in the symbol table — synthesise a TypeSymbol
                // whose Name doubles as the FullyQualifiedName for delegate dispatch.
                var fnTypeSym = new TypeSymbol(param.TypeNode.FullyQualifiedName, TypeKind.Class);
                parameter = new ParameterSymbol(param.Name, fnTypeSym, symbol)
                {
                    IsOptional = param.TypeNode.IsOptional || param.TypeNode.IsImplicitlyUnwrapped
                };
                symbol.AddSymbol(parameter);
                continue;
            }

            if (TryResolveGenericParam(param.TypeNode, out var genericSym))
            {
                parameter = new ParameterSymbol(param.Name, genericSym!, symbol)
                {
                    IsOptional = param.TypeNode.IsOptional || param.TypeNode.IsImplicitlyUnwrapped
                };
                symbol.AddSymbol(parameter);
                continue;
            }

            var paramType = _symbolTable.FindType(node.Root, param.TypeNode.FullyQualifiedName);

            if (paramType.IsSuccess)
            {
                parameter = new ParameterSymbol(param.Name, paramType.Unwrapped(), symbol)
                {
                    IsOptional = param.TypeNode.IsOptional || param.TypeNode.IsImplicitlyUnwrapped
                };
                symbol.AddSymbol(parameter);
            }
            else
            {
                var typeError =
                    CompilerErrorFactory.TopLevelDefinitionError(param.TypeNode.FullyQualifiedName, param.TypeNode.Meta);

                _errorCollector.Collect(typeError);

                node.Status = INode.ResolutionStatus.Failed;

                break;
            }
        }

        if (node.ReturnType is not null)
        {
            if (TryResolveGenericParam(node.ReturnType, out var genReturn))
            {
                symbol!.ReturnType = genReturn!;
            }
            else
            {
                var returnType = _symbolTable.FindType(node.Root, node.ReturnType.FullyQualifiedName);

                if (returnType.IsSuccess)
                {
                    symbol!.ReturnType = returnType.Unwrapped();
                    var retIsOptional = node.ReturnType.IsOptional;
                    var retIsIUO = node.ReturnType.IsImplicitlyUnwrapped;
                    node.ReturnType = new TypeReferenceNode(returnType.Unwrapped().Name, node)
                    {
                        FullyQualifiedName = returnType.Unwrapped().FullyQualifiedName,
                        Assembly = returnType.Unwrapped().Assembly,
                        IsOptional = retIsOptional,
                        IsImplicitlyUnwrapped = retIsIUO,
                    };
                }
                else
                {
                    node.Status = INode.ResolutionStatus.Failed;

                    var error = CompilerErrorFactory.TopLevelDefinitionError(node.ReturnType.FullyQualifiedName, node.ReturnType.Meta);

                    _errorCollector.Collect(error);

                    return;
                }
            }
        }
    }

    // If `typeNode` is a bare identifier matching a generic parameter on any enclosing
    // class/record/struct/contract/fn, synthesise a TypeSymbol that codegen can later turn
    // into the right Cecil generic-parameter reference.
    private static bool TryResolveGenericParam(TypeReferenceNode typeNode, out TypeSymbol? sym)
    {
        sym = null;
        if (typeNode.Name.Contains('.') || typeNode.GenericArguments.Count > 0) { return false; }
        INode? cursor = ((INode)typeNode).Parent;
        while (cursor != null)
        {
            var generics = cursor switch
            {
                ClassNode cn => cn.GenericArguments,
                RecordNode rn => rn.GenericArguments,
                StructNode sn => sn.GenericArguments,
                ContractNode con => con.GenericArguments,
                FuncNode fn => fn.GenericArguments,
                _ => null
            };
            if (generics != null && generics.Any(g => g.Name == typeNode.Name))
            {
                sym = new TypeSymbol(typeNode.Name, TypeKind.Generic);
                typeNode.TypeKind = AST.Types.Kind.Generic;
                typeNode.ReferenceKind = TypeReferenceKind.Generic;
                typeNode.FullyQualifiedName = typeNode.Name;
                typeNode.Status = INode.ResolutionStatus.Resolved;
                return true;
            }
            cursor = cursor.Parent;
        }
        return false;
    }

    public void Visit(InitNode node)
    {
        if (_currentSymbol is not TypeSymbol)
        {
            return;
        }

        var symbol = _currentSymbol.Symbols.OfType<InitSymbol>().FirstOrDefault(init =>
        {
            if (init.Symbols.OfType<ParameterSymbol>().Count() != node.Parameters.Count)
            {
                return false;
            }

            foreach (var (nParam, sParam) in node.Parameters.Zip(init.Symbols.OfType<ParameterSymbol>()))
            {
                if (sParam.Name != nParam.Name)
                {
                    return false;
                }
            }

            return true;
        });

        if (symbol == null)
        {
            return;
        }

        // The register sub-pass already added placeholder parameters; replace them with the
        // resolved ones rather than appending (mirrors the FuncSymbol dedup).
        foreach (var old in symbol.Symbols.OfType<ParameterSymbol>().ToList())
        {
            symbol.Symbols.Remove(old);
            if (symbol.SymbolsByName.TryGetValue(old.Name, out var byName))
            {
                byName.Remove(old);
            }
        }

        foreach (var param in node.Parameters)
        {
            if (param.TypeNode is FunctionTypeNode)
            {
                var fnTypeSym = new TypeSymbol(param.TypeNode.FullyQualifiedName, TypeKind.Class);
                var fnParam = new ParameterSymbol(param.Name, fnTypeSym, symbol)
                {
                    IsOptional = param.TypeNode.IsOptional || param.TypeNode.IsImplicitlyUnwrapped
                };
                symbol!.AddSymbol(fnParam);
                continue;
            }
            if (TryResolveGenericParam(param.TypeNode, out var genParam))
            {
                var p = new ParameterSymbol(param.Name, genParam!, symbol)
                {
                    IsOptional = param.TypeNode.IsOptional || param.TypeNode.IsImplicitlyUnwrapped
                };
                symbol!.AddSymbol(p);
                continue;
            }
            var paramType = _symbolTable.FindType(node.Root, param.TypeNode.FullyQualifiedName);

            if (paramType.IsSuccess)
            {
                var parameter = new ParameterSymbol(param.Name, paramType.Unwrapped(), symbol)
                {
                    IsOptional = param.TypeNode.IsOptional || param.TypeNode.IsImplicitlyUnwrapped
                };
                symbol!.AddSymbol(parameter);
            }
            else
            {
                var typeError =
                    CompilerErrorFactory.TopLevelDefinitionError(param.TypeNode.FullyQualifiedName, param.TypeNode.Meta);
                
                _errorCollector.Collect(typeError);

                node.Status = INode.ResolutionStatus.Failed;
                
                break;
            }
        }
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

    public void Visit(OperatorNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = _currentSymbol.Symbols.OfType<OperatorSymbol>().FirstOrDefault(op =>
        {
            if (op.Symbols.OfType<ParameterSymbol>().Count() != node.Parameters.Count)
            {
                return false;
            }

            foreach (var (nParam, sParam) in node.Parameters.Zip(op.Symbols.OfType<ParameterSymbol>()))
            {
                if (sParam.Name != nParam.Name)
                {
                    return false;
                }
            }

            return true;
        });        
        foreach (var param in node.Parameters)
        {
            if (param.TypeNode is FunctionTypeNode)
            {
                var fnTypeSym = new TypeSymbol(param.TypeNode.FullyQualifiedName, TypeKind.Class);
                var fnParam = new ParameterSymbol(param.Name, fnTypeSym, symbol)
                {
                    IsOptional = param.TypeNode.IsOptional || param.TypeNode.IsImplicitlyUnwrapped
                };
                symbol.AddSymbol(fnParam);
                continue;
            }
            if (TryResolveGenericParam(param.TypeNode, out var genP))
            {
                var p = new ParameterSymbol(param.Name, genP!, symbol)
                {
                    IsOptional = param.TypeNode.IsOptional || param.TypeNode.IsImplicitlyUnwrapped
                };
                symbol!.AddSymbol(p);
                continue;
            }
            var paramType = _symbolTable.FindType(node.Root, param.TypeNode.FullyQualifiedName);

            if (paramType.IsSuccess)
            {
                var parameter = new ParameterSymbol(param.Name, paramType.Unwrapped(), symbol)
                {
                    IsOptional = param.TypeNode.IsOptional || param.TypeNode.IsImplicitlyUnwrapped
                };
                symbol!.AddSymbol(parameter);
            }
            else
            {
                var typeError =
                    CompilerErrorFactory.TopLevelDefinitionError(param.TypeNode.FullyQualifiedName, param.TypeNode.Meta);

                _errorCollector.Collect(typeError);

                node.Status = INode.ResolutionStatus.Failed;

                break;
            }
        }

        if (node.ReturnType is not null)
        {
            if (TryResolveGenericParam(node.ReturnType, out var genR))
            {
                if (symbol != null) { symbol.ReturnType = genR!; }
                return;
            }
            var returnType = _symbolTable.FindType(node.Root, node.ReturnType.FullyQualifiedName);

            if (returnType.IsSuccess)
            {
                symbol!.ReturnType = returnType.Unwrapped();
                var retIsOptional = node.ReturnType.IsOptional;
                var retIsIUO = node.ReturnType.IsImplicitlyUnwrapped;
                node.ReturnType = new TypeReferenceNode(returnType.Unwrapped().Name, node)
                {
                    FullyQualifiedName = returnType.Unwrapped().FullyQualifiedName,
                    Assembly = returnType.Unwrapped().Assembly,
                    IsOptional = retIsOptional,
                    IsImplicitlyUnwrapped = retIsIUO,
                };
            }
            else
            {
                node.Status = INode.ResolutionStatus.Failed;

                var error = CompilerErrorFactory.TopLevelDefinitionError(node.ReturnType.FullyQualifiedName, node.ReturnType.Meta);

                _errorCollector.Collect(error);

                return;
            }
        }

    }

    public void Visit(PropertyNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = _currentSymbol.LookupAllSymbols(node.Name).OfType<PropertySymbol>().FirstOrDefault();

        if (node.TypeNode is not null)
        {
            if (TryResolveGenericParam(node.TypeNode, out var genPropSym))
            {
                if (symbol != null) { symbol.Type = genPropSym!; }
                return;
            }
            var type = _symbolTable.FindType(node.Root, node!.TypeNode!.FullyQualifiedName);

            if (type.IsSuccess)
            {
                symbol!.Type = type.Unwrapped();

                var isOptional = node.TypeNode.IsOptional;
                var isIUO = node.TypeNode.IsImplicitlyUnwrapped;

                node.TypeNode = new TypeReferenceNode(type.Unwrapped().Name, node)
                {
                    FullyQualifiedName = type.Unwrapped().FullyQualifiedName,
                    Assembly = type.Unwrapped().Assembly,
                    IsOptional = isOptional,
                    IsImplicitlyUnwrapped = isIUO,
                };

                symbol!.IsOptional = isOptional;
                symbol!.IsImplicitlyUnwrapped = isIUO;
            }
            else
            {
                node.Status = INode.ResolutionStatus.Failed;

                var error = CompilerErrorFactory.TopLevelDefinitionError(node.TypeNode.FullyQualifiedName,
                    node.TypeNode.Meta);

                _errorCollector.Collect(error);

                return;
            }
        }
        else
        {
            _expressionResolver.ResolveExpressionType(node, _symbolTable);
        }
    }
    
    public void Visit(RecordNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = _symbolTable.FindTypeByFQN(node.FullyQualifiedName);
        if (symbol != null) { ResolveGenericConstraints(symbol, node.GenericArguments, node.Root); }

        var previous = _currentSymbol;
        _currentSymbol = symbol;

        node.Body?.Accept(this);

        SynthesizeMemberwiseInit(node.Body, symbol);

        _currentSymbol = previous;
    }

    public void Visit(StructNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = _symbolTable.FindTypeByFQN(node.FullyQualifiedName);
        if (symbol != null) { ResolveGenericConstraints(symbol, node.GenericArguments, node.Root); }

        var previous = _currentSymbol;
        _currentSymbol = symbol;

        node.Body?.Accept(this);

        SynthesizeMemberwiseInit(node.Body, symbol);

        _currentSymbol = previous;
    }

    private void SynthesizeMemberwiseInit(BlockNode? body, TypeSymbol? typeSymbol)
    {
        if (body == null || typeSymbol == null)
        {
            return;
        }

        if (typeSymbol.Symbols.OfType<InitSymbol>().Any())
        {
            return;
        }

        var initSymbol = new InitSymbol { ReturnType = typeSymbol };

        foreach (var prop in body.Children.OfType<PropertyNode>())
        {
            if (prop.Get != null || prop.Set != null)
            {
                continue;
            }

            var propSymbol = typeSymbol.LookupAllSymbols(prop.Name).OfType<PropertySymbol>().FirstOrDefault();
            if (propSymbol == null)
            {
                continue;
            }

            var paramSymbol = new ParameterSymbol(prop.Name, propSymbol.Type, initSymbol)
            {
                IsOptional = propSymbol.IsOptional
            };
            initSymbol.AddSymbol(paramSymbol);
        }

        typeSymbol.AddSymbol(initSymbol);
    }
}