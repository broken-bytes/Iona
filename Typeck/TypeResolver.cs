using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Shared;
using Symbols;
using Symbols.Symbols;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using static AST.Nodes.INode;

namespace Typeck
{
    internal class TypeResolver :
        IAssignmentVisitor,
        IBlockVisitor,
        IBinaryExpressionVisitor,
        IClassVisitor,
        IContractVisitor,
        IFileVisitor,
        IFuncCallVisitor,
        IFuncVisitor,
        IIdentifierVisitor,
        IImportVisitor,
        IInitCallVisitor,
        IInitVisitor,
        ILiteralVisitor,
        IModuleVisitor,
        IObjectLiteralVisitor,
        IOperatorVisitor,
        IPropAccessVisitor,
        IPropertyVisitor,
        IReturnVisitor,
        IScopeResolutionVisitor,
        IStructVisitor,
        ITypeReferenceVisitor,
        IUnaryExpressionVisitor,
        IVariableVisitor
    {
        private SymbolTable _symbolTable;
        private readonly IErrorCollector _errorCollector;
        private readonly IWarningCollector _warningCollector;
        private readonly IFixItCollector _fixItCollector;

        internal TypeResolver(
            IErrorCollector errorCollector,
            IWarningCollector warningCollector,
            IFixItCollector fixItCollector
        )
        {
            // Dummy symbol table so it doesn't need to be nullable
            _symbolTable = new SymbolTable();
            _errorCollector = errorCollector;
            _warningCollector = warningCollector;
            _fixItCollector = fixItCollector;
        }

        internal void TypeCheckAST(FileNode file, SymbolTable table)
        {
            _symbolTable = table;

            file.Accept(this);
        }

        public void Visit(AssignmentNode node)
        {
            // Two things to check:
            // - Check the types of the target and the value
            // - Check that the target and value have the same type (else add an error node)
            CheckNode(node.Target);
            CheckNode(node.Value);
        }

        public void Visit(BlockNode node)
        {
            foreach (var child in node.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(BinaryExpressionNode node)
        {
            CheckNode(node.Left);
            CheckNode(node.Right);
        }

        public void Visit(ClassNode node)
        {
            
            // For each contract this class conforms to, check if one of them is in fact a class and make it the base type
            foreach (var contract in node.Contracts)
            {
                var symbol = _symbolTable.FindTypeByFQN(contract.Name);

                if (symbol == null)
                {
                    symbol = _symbolTable.FindTypeBySimpleName(contract.Name);
                }

                if (symbol != null)
                {
                    contract.FullyQualifiedName = symbol.FullyQualifiedName;
                    contract.TypeKind = Utils.SymbolKindToASTKind(symbol.TypeKind);
                    contract.Assembly = symbol.Assembly;
                    
                    if (symbol.TypeKind == TypeKind.Class)
                    {
                        node.BaseType = contract;
                    }
                }
            }

            if (node.BaseType != null)
            {
                node.Contracts.Remove(node.BaseType);
            }
            
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(ContractNode node)
        {
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(FileNode node)
        {
            // We ignore the file itself, but we visit its children and add the module to the symbol table
            // (a file can only have one module)
            foreach (var child in node.Children)
            {
                if (child is ModuleNode module)
                {
                    module.Accept(this);
                }
            }
        }

        public void Visit(FuncCallNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(FuncNode node)
        {
            // Check the parameters if they have a (known) type
            foreach (var param in node.Parameters)
            {
                var actualType = CheckNodeType(param.TypeNode);

                if (actualType != null)
                {
                    param.TypeNode = actualType;
                }
            }

            // Check the return type
            if (node.ReturnType is TypeReferenceNode returnType)
            {
                var actualType = CheckNodeType(returnType);

                if (actualType != null)
                {
                    node.ReturnType = actualType;
                }
            }

            // Check the body
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(IdentifierNode node)
        {
            node.Status = ResolutionStatus.Resolving;
            var symbol = _symbolTable.FindBy(node);

            if (symbol == null)
            {
                node.Status = ResolutionStatus.Failed;
                return;
            }

            TypeSymbol? typeSymbol = null;
            if (symbol is TypeSymbol type)
            {
                typeSymbol = type;
            }
            else if (symbol is VariableSymbol variable)
            {
                typeSymbol = variable.Type;
            }
            else if (symbol is PropertySymbol property)
            {
                typeSymbol = property.Type;
            }
            else if (symbol is ParameterSymbol parameter)
            {
                typeSymbol = parameter.Type;
            }

            if (typeSymbol != null)
            {
                node.Status = ResolutionStatus.Resolved;
                var typeRef = new TypeReferenceNode(typeSymbol.Name, node);
                node.ResultType = typeRef;
                typeRef.FullyQualifiedName = typeSymbol.FullyQualifiedName;
                typeRef.TypeKind = Utils.SymbolKindToASTKind(typeSymbol.TypeKind);
                typeRef.Status = ResolutionStatus.Resolved;
            }
            else
            {
                node.Status = ResolutionStatus.Failed;
            }
        }

        public void Visit(ImportNode import)
        {
            throw new NotImplementedException();
        }

        public void Visit(InitCallNode initCall)
        {
            // Find the type for the init call
            var type = _symbolTable.FindTypeByFQN(initCall.TypeFullName);

            if (type == null)
            {
                initCall.Status = ResolutionStatus.Failed;
                return;
            }

            initCall.ResultType = new TypeReferenceNode(type.Name, initCall)
            {
                FullyQualifiedName = type.FullyQualifiedName,
                TypeKind = Utils.SymbolKindToASTKind(type.TypeKind),
                Status = ResolutionStatus.Resolved,
                Meta = initCall.Meta
            };

            // Ensure that the type is imported
            if (!CheckFileImportsModule(initCall.ResultType))
            {
                EmitImportMissing(initCall, type);

                initCall.Status = ResolutionStatus.Failed;

                return;
            }

            initCall.Status = ResolutionStatus.Resolved;
        }

        public void Visit(InitNode node)
        {
            // Check the parameters if they have a (known) type
            foreach (var param in node.Parameters)
            {
                if (param.TypeNode is TypeReferenceNode type)
                {
                    var actualType = CheckNodeType(type);

                    if (actualType != null)
                    {
                        param.TypeNode = actualType;
                    }
                    else
                    {
                        node.Status = ResolutionStatus.Failed;
                    }
                }
            }

            // Check the body
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(LiteralNode node)
        {
            // There are six different types of literals:
            // - Bool
            // - Int
            // - Float
            // - String
            // - Char
            // - Null

            // We can determine the type of the literal by looking at the value (except for null and array `[]`)

            if (node.LiteralType == LiteralType.Boolean)
            {
                node.ResultType = new TypeReferenceNode("Bool", node)
                {
                    FullyQualifiedName = "Builtins.Bool",
                    TypeKind = AST.Types.Kind.Struct,
                    Status = ResolutionStatus.Resolved
                };
            }
            if (node.LiteralType == LiteralType.Integer)
            {
                node.ResultType = new TypeReferenceNode("Int", node)
                {
                    FullyQualifiedName = "Builtins.Int",
                    TypeKind = AST.Types.Kind.Struct,
                    Status = ResolutionStatus.Resolved
                };
            }
            if (node.LiteralType == LiteralType.Float)
            {
                node.ResultType = new TypeReferenceNode("Float", node)
                {
                    FullyQualifiedName = "Builtins.Float",
                    TypeKind = AST.Types.Kind.Struct,
                    Status = ResolutionStatus.Resolved
                };
            }
            if (node.LiteralType == LiteralType.String)
            {
                node.ResultType = new TypeReferenceNode("String", node)
                {
                    FullyQualifiedName = "Builtins.String",
                    TypeKind = AST.Types.Kind.Struct,
                    Status = ResolutionStatus.Resolved
                };
            }
            if (node.LiteralType == LiteralType.Char)
            {
                node.ResultType = new TypeReferenceNode("Char", node)
                {
                    FullyQualifiedName = "Builtins.Char",
                    TypeKind = AST.Types.Kind.Struct,
                    Status = ResolutionStatus.Resolved
                };
            }
        }

        public void Visit(ModuleNode node)
        {
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
                    case FuncNode funcNode:
                        funcNode.Accept(this);
                        break;
                    case StructNode structNode:
                        structNode.Accept(this);
                        break;
                    case VariableNode variableNode:
                        variableNode.Accept(this);
                        break;
                    default:
                        break;
                }
            }
        }

        public void Visit(ObjectLiteralNode node)
        {
           
        }

        public void Visit(OperatorNode node)
        {
            // Check the parameters if they have a (known) type
            foreach (var param in node.Parameters)
            {
                ResolveParameter(param);
            }

            // Check the return type
            if (node.ReturnType is TypeReferenceNode returnType)
            {
                var actualType = CheckNodeType(returnType);

                if (actualType != null)
                {
                    node.ReturnType = actualType;
                }
            }

            // Check the body
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(PropAccessNode node)
        {
            if (node.Status is not ResolutionStatus.Resolving)
            {
                return;
            }

            GetTypeOfPropAccess(node);
        }

        public void Visit(PropertyNode node)
        {
            if (node.Status is not ResolutionStatus.Resolving)
            {
                return;
            }
            
            // If the value is a literal we can parse it right away. Otherwise, we need to check the name of the TypeNode

            var actualType = CheckNodeType(node.TypeNode);
            node.TypeNode = actualType;

            // Update the symbol in the symbol table
            var symbol = _symbolTable.FindBy(node);
            var typeSymbol = _symbolTable.FindTypeByFQN(node.TypeNode.FullyQualifiedName);

            if (symbol is PropertySymbol prop && typeSymbol is not null)
            {
                prop.Type = typeSymbol;
            }
            
        }

        public void Visit(ReturnNode node)
        {
            node.Status = ResolutionStatus.Resolving;

            if (node.Value is null)
            {
                node.Status = ResolutionStatus.Resolved;
            }
            else
            {
                CheckNodeType(node.Value);
            }
        }

        public void Visit(ScopeResolutionNode node)
        {
            // Find the first symbol
            var symbol = _symbolTable.FindBy(node.Scope);
            
            Console.Write(symbol);
        }

        public void Visit(StructNode node)
        {
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(TypeReferenceNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(UnaryExpressionNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(VariableNode node)
        {
            if (node.TypeNode is TypeReferenceNode type)
            {
                var actualType = CheckNodeType(type);
                node.TypeNode = actualType;
            }

            if (node.Value != null)
            {
                CheckNode(node.Value);

                if (node.Value.ResultType == null)
                {
                    node.Status = ResolutionStatus.Failed;
                    return;
                }

                if (node.Value.Status == ResolutionStatus.Resolved)
                {
                    node.TypeNode = node.Value.ResultType;

                    // Update the symbol in the symbol table
                    var symbol = _symbolTable.FindBy(node);
                    var typeSymbol = _symbolTable.FindTypeByFQN(node.Value.ResultType.FullyQualifiedName);
                    
                    if (symbol is VariableSymbol var && typeSymbol is not null)
                    {
                        var.Type = typeSymbol;
                    }
                }
            }
        }

        private TypeReferenceNode? CheckNodeType(INode node)
        {
            if (node is TypeReferenceNode typeNode)
            {
                return CheckTypeReferenceNode(typeNode);
            }
            
            if (node is PropAccessNode propAccess)
            {
                return CheckPropAccessNode(propAccess);
            }

            return null;
        }

        // ---- Helper methods ----
        private TypeReferenceNode? CheckTypeReferenceNode(TypeReferenceNode typeNode)
        {
            TypeReferenceNode typeRef;

            // We first need to find the scope this type reference is in(to find nested types, or the module)
            List<INode> nodeOrder = ((INode)typeNode).Hierarchy();

            // Now get the first module in the list (each file can only have one module)
            var file = nodeOrder.OfType<FileNode>().FirstOrDefault();

            var module = file.Children.OfType<ModuleNode>().FirstOrDefault();

            if (module == null)
            {
                return null;
            }

            // Now we know the model and the scopes in correct order, we can traverse both the ast and the symbol table to find the type
            var type = _symbolTable.FindTypeBy(typeNode, null);
            
            if (type != null)
            {
                typeNode.FullyQualifiedName = type.FullyQualifiedName;
                typeNode.Status = ResolutionStatus.Resolved;
                typeNode.TypeKind = Utils.SymbolKindToASTKind(type.TypeKind);

                return typeNode;
            }

            var error = CompilerErrorFactory.TopLevelDefinitionError(
                typeNode.Name,
                typeNode.Meta
            );

            _errorCollector.Collect(error);

            return null;
        }

        private TypeReferenceNode? CheckPropAccessNode(PropAccessNode propAccess)
        {
            // We first need to find the scope this type reference is in(to find nested types, or the module)
            // Step 1: Check if the leftmost node is a module
            if (propAccess.Object is not IdentifierNode target)
            {

                var error = CompilerErrorFactory.SyntaxError(
                    "Invalid member access in type reference",
                    propAccess.Meta
                );

                _errorCollector.Collect(error);

                return null;
            }

            // Step 2: Find the module
            var isModule = _symbolTable.Modules.Any(mod => mod.Name == target.Value);

            // Edge case: If the target is indeed a module, we still need to check if the module has a type with the same name
            // To do so we need to do the following:
            // - Check if the the member node is an identifier (if not we cannot mean the type within the module but rather the module)
            // - If it does, we need to further check that the next node is not the type of that very same name
            if (isModule)
            {
                if (propAccess.Property is not IdentifierNode member)
                {
                    // Not an identifier, so we cannot mean the type within the module
                }
            }

            return null;
        }

        private void CheckNode(INode? node)
        {
            if (node == null)
            {
                return;
            }

            switch (node)
            {
                case AssignmentNode assignmentNode:
                    assignmentNode.Accept(this);
                    break;
                case BinaryExpressionNode binaryExpressionNode:
                    binaryExpressionNode.Accept(this);
                    break;
                case BlockNode blockNode:
                    blockNode.Accept(this);
                    break;
                case ClassNode classNode:
                    classNode.Accept(this);
                    break;
                case ContractNode contractNode:
                    contractNode.Accept(this);
                    break;
                case FileNode fileNode:
                    fileNode.Accept(this);
                    break;
                case FuncCallNode funcCallNode:
                    funcCallNode.Accept(this);
                    break;
                case FuncNode funcNode:
                    funcNode.Accept(this);
                    break;
                case IdentifierNode identifierNode:
                    identifierNode.Accept(this);
                    break;
                case ImportNode importNode:
                    importNode.Accept(this);
                    break;
                case InitCallNode initCallNode:
                    initCallNode.Accept(this);
                    break;
                case InitNode initNode:
                    initNode.Accept(this);
                    break;
                case LiteralNode literalNode:
                    literalNode.Accept(this);
                    break;
                case ModuleNode moduleNode:
                    moduleNode.Accept(this);
                    break;
                case ObjectLiteralNode objectLiteralNode:
                    objectLiteralNode.Accept(this);
                    break;
                case OperatorNode operatorNode:
                    operatorNode.Accept(this);
                    break;
                case PropAccessNode propAccessNode:
                    propAccessNode.Accept(this);
                    break;
                case PropertyNode propertyNode:
                    propertyNode.Accept(this);
                    break;
                case ReturnNode returnNode:
                    returnNode.Accept(this);
                    break;
                case ScopeResolutionNode scopeNode:
                    scopeNode.Accept(this);
                    break;
                case StructNode structNode:
                    structNode.Accept(this);
                    break;
                case TypeReferenceNode typeReferenceNode:
                    typeReferenceNode.Accept(this);
                    break;
                case UnaryExpressionNode unaryExpressionNode:
                    unaryExpressionNode.Accept(this);
                    break;
                case VariableNode variableNode:
                    variableNode.Accept(this);
                    break;
            }
        }

        private TypeReferenceNode? GetTypeOf(INode node)
        {
            if (node is IdentifierNode identifier)
            {
                return identifier.ResultType as TypeReferenceNode;
            }
            else if (node is PropAccessNode propAccess)
            {
                return GetTypeOfPropAccess(propAccess);
            }
            else if (node is LiteralNode literal)
            {
                return literal.ResultType as TypeReferenceNode;
            }

            return null;
        }

        private TypeReferenceNode? GetTypeOfSelf(SelfNode self)
        {
            // Self means we must be within a TypeNode. Find it by going up the parents until one was found, or we hit a module node (invalid self then)
            INode? current = self;

            while (current != null)
            {
                if (current is ITypeNode type)
                {
                    var typeRef = new TypeReferenceNode(type.Name, self);

                    var module = type.Parent;

                    if (module is ModuleNode moduleNode)
                    {
                        typeRef.FullyQualifiedName = type.FullyQualifiedName;

                        if (type is ClassNode)
                        {
                            typeRef.TypeKind = AST.Types.Kind.Class;
                        }
                        else if (type is StructNode)
                        {
                            typeRef.TypeKind = AST.Types.Kind.Struct;
                        }
                        else if (type is ContractNode)
                        {
                            typeRef.TypeKind = AST.Types.Kind.Contract;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }

                    return typeRef;
                }

                current = current.Parent;
            }

            return null;
        }

        private TypeReferenceNode? GetTypeOfPropAccess(PropAccessNode propAccess, TypeSymbol? parent = null)
        {
            TypeSymbol? typeSymbol = null;
            
            if (propAccess.Object is IdentifierNode target)
            {
                ISymbol? symbol = null;

                if (parent == null)
                {
                    symbol = _symbolTable.FindBy(target);
                }
                else
                {
                    parent.Symbols.Find(s => s.Name == target.Value);
                }

                if (symbol == null)
                {
                    return null;
                }

                if (symbol is TypeSymbol type)
                {
                    typeSymbol = type;
                }
                else if (symbol is VariableSymbol variable)
                {
                    typeSymbol = variable.Type;
                }
                else if (symbol is PropertySymbol property)
                {
                    typeSymbol = property.Type;
                }
                else if (symbol is ParameterSymbol parameter)
                {
                    typeSymbol = parameter.Type;
                }

                if (typeSymbol == null)
                {
                    return null;
                }

                target.ResultType = new TypeReferenceNode(typeSymbol.Name, target)
                {
                    FullyQualifiedName = typeSymbol.FullyQualifiedName,
                    TypeKind = Utils.SymbolKindToASTKind(typeSymbol.TypeKind),
                    Status = ResolutionStatus.Resolved
                };
            }
            else if (propAccess.Object is SelfNode self)
            {
                var type = GetTypeOfSelf(self);

                typeSymbol = _symbolTable.FindTypeByFQN(type.FullyQualifiedName);

                if (typeSymbol == null)
                {
                    return null;
                }

                self.ResultType = type;
            }
            else
            {
                return null;
            }

            if (propAccess.Property is IdentifierNode identifier)
            {
                var block = typeSymbol.Symbols.OfType<BlockSymbol>().First();
                var prop = block.Symbols.OfType<PropertySymbol>().ToList().Find(symbol => symbol.Name == identifier.Value);

                var typeRef = new TypeReferenceNode(prop.Type.Name, propAccess)
                {
                    FullyQualifiedName = prop.Type.FullyQualifiedName,
                    TypeKind = Utils.SymbolKindToASTKind(prop.Type.TypeKind),
                    Status = ResolutionStatus.Resolved
                };

                identifier.ResultType = typeRef;

                propAccess.ResultType = typeRef;

                return typeRef;
            }
            else if (propAccess.Property is PropAccessNode prop)
            {
                return GetTypeOfPropAccess(prop, typeSymbol);
            }

            return null;
        }

        private void ResolveParameter(ParameterNode param)
        {
            if (param.TypeNode is TypeReferenceNode type)
            {
                var actualType = CheckNodeType(type);

                var symbol = (ParameterSymbol?)_symbolTable.FindBy(param);

                if (actualType != null && symbol != null)
                {
                    param.TypeNode = actualType;

                    if (actualType is TypeReferenceNode typeRef)
                    {
                        // Set the type of the parameter (symbol)
                        var typeSymbol = _symbolTable.FindTypeByFQN(typeRef.FullyQualifiedName);

                        if (typeSymbol != null)
                        {
                            symbol.Type = typeSymbol;
                        }
                        else
                        {
                            param.Status = ResolutionStatus.Failed;
                            _errorCollector.Collect(CompilerErrorFactory.TopLevelDefinitionError(typeRef.Name, typeRef.Meta));
                        }
                    }

                    // TODO: Add array and generic type support
                }
                else
                {
                    var error = CompilerErrorFactory.TopLevelDefinitionError(
                        type.Name,
                        type.Meta
                    );

                    _errorCollector.Collect(error);

                    param.Status = ResolutionStatus.Failed;
                }
            }
        }

        private bool CheckFileImportsModule(INode node)
        {
            if (node is not TypeReferenceNode type)
            {
                return false;
            }

            var file = ((FileNode)node.Root);

            var imports = file.Children.OfType<ImportNode>();

            ModuleNode? moduleNode = null;

            var module = _symbolTable.FindModuleByFQN(type.FullyQualifiedName);

            while (node.Parent != null)
            {
                if (node is ModuleNode mod)
                {
                    moduleNode = mod;
                    break;
                }

                node = node.Parent;
            }

            if (moduleNode == null)
            {
                return false;
            }

            // Always import the module of the node itself
            imports = imports.Append(new ImportNode(moduleNode.Name, null));

            foreach (var import in imports)
            {
                if (import.Name == module.Name)
                {
                    return true;
                }
            }

            return false;
        }

        private void EmitImportMissing(INode node, TypeSymbol type)
        {
            var error = CompilerErrorFactory.TopLevelDefinitionError(
                    type.Name,
                    node.Meta
                );

            _errorCollector.Collect(error);

            var module = _symbolTable.FindModuleByFQN(type.FullyQualifiedName);

            if (module == null)
            {
                return;
            }

            // Show a fix-it to import the module
            var fixIt = FixItFactory.ImportMissing(
                module.Name,
                new Metadata
                {
                    File = node.Root.ToString(),
                    LineStart = 0,
                    LineEnd = 0,
                    ColumnStart = 0,
                    ColumnEnd = 1
                }
            );

            _fixItCollector.Collect(fixIt);
        }
    }
}
