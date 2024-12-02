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
        
        _currentSymbol.Symbols.Add(symbol!);
        symbol!.Parent = _currentSymbol;
        
        _currentSymbol = symbol;

        node.Body?.Accept(this);
        
        _currentSymbol = null;
    }
    
    public void Visit(ContractNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = _symbolTable.FindTypeByFQN(node.FullyQualifiedName);
        
        _currentSymbol.Symbols.Add(symbol!);
        symbol!.Parent = _currentSymbol;
        
        _currentSymbol = symbol;

        node.Body?.Accept(this);
    }

    public void Visit(EnumNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = _symbolTable.FindTypeByFQN(node.FullyQualifiedName);
        
        _currentSymbol.Symbols.Add(symbol!);
        symbol!.Parent = _currentSymbol;
        
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
        
        var symbol = _currentSymbol.Symbols.OfType<FuncSymbol>().FirstOrDefault(func =>
        {
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
        
        foreach (var param in node.Parameters)
        {
            var paramType = _symbolTable.FindType(node.Root, param.TypeNode.FullyQualifiedName);
            
            if (paramType.IsSuccess)
            {
                var parameter = new ParameterSymbol(param.Name, paramType.Unwrapped(), symbol);
                symbol!.Symbols.Add(parameter);
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
            var returnType = _symbolTable.FindType(node.Root, node.ReturnType.FullyQualifiedName);

            if (returnType.IsSuccess)
            {
                symbol!.ReturnType = returnType.Unwrapped();
                node.ReturnType = new TypeReferenceNode(returnType.Unwrapped().Name, node)
                {
                    FullyQualifiedName = returnType.Unwrapped().FullyQualifiedName,
                    Assembly = returnType.Unwrapped().Assembly
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

        _currentSymbol.Symbols.Add(symbol!);
        symbol!.Parent = _currentSymbol;
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
        
        foreach (var param in node.Parameters)
        {
            var paramType = _symbolTable.FindType(node.Root, param.TypeNode.FullyQualifiedName);
            
            if (paramType.IsSuccess)
            {
                var parameter = new ParameterSymbol(param.Name, paramType.Unwrapped(), symbol);
                symbol!.Symbols.Add(parameter);
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

        _currentSymbol.Symbols.Add(symbol!);
        symbol!.Parent = _currentSymbol;
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
            var paramType = _symbolTable.FindType(node.Root, param.TypeNode.FullyQualifiedName);
            
            if (paramType.IsSuccess)
            {
                var parameter = new ParameterSymbol(param.Name, paramType.Unwrapped(), symbol);
                symbol!.Symbols.Add(parameter);
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
            var returnType = _symbolTable.FindType(node.Root, node.ReturnType.FullyQualifiedName);

            if (returnType.IsSuccess)
            {
                symbol!.ReturnType = returnType.Unwrapped();
                node.ReturnType = new TypeReferenceNode(returnType.Unwrapped().Name, node)
                {
                    FullyQualifiedName = returnType.Unwrapped().FullyQualifiedName,
                    Assembly = returnType.Unwrapped().Assembly
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

        _currentSymbol.Symbols.Add(symbol!);
        symbol!.Parent = _currentSymbol;
    }

    public void Visit(PropertyNode node)
    {
        if (_currentSymbol == null)
        {
            return;
        }

        var symbol = _currentSymbol.Symbols.OfType<PropertySymbol>().FirstOrDefault(prop => prop.Name == node.Name);

        if (node.TypeNode is not null)
        {
            var type = _symbolTable.FindType(node.Root, node!.TypeNode!.FullyQualifiedName);

            if (type.IsSuccess)
            {
                symbol!.Type = type.Unwrapped();

                node.TypeNode = new TypeReferenceNode(type.Unwrapped().Name, node)
                {
                    FullyQualifiedName = type.Unwrapped().FullyQualifiedName,
                    Assembly = type.Unwrapped().Assembly
                };
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