using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Symbols;
using Symbols.Symbols;
using System;
using System.Net.Http.Headers;
using static AST.Nodes.INode;

namespace Typeck
{
    internal class TypeChecker :
        IAssignmentVisitor,
        IBlockVisitor,
        IBinaryExpressionVisitor,
        IClassVisitor,
        IContractVisitor,
        IErrorVisitor,
        IFileVisitor,
        IFuncCallVisitor,
        IFuncVisitor,
        IIdentifierVisitor,
        IImportVisitor,
        IInitVisitor,
        ILiteralVisitor,
        IMemberAccessVisitor,
        IModuleVisitor,
        IObjectLiteralVisitor,
        IOperatorVisitor,
        IPropAccessVisitor,
        IPropertyVisitor,
        IReturnVisitor,
        IStructVisitor,
        ITypeReferenceVisitor,
        IUnaryExpressionVisitor,
        IVariableVisitor
    {
        private SymbolTable _symbolTable;

        internal TypeChecker()
        {
            // Dummy symbol table so it doesn't need to be nullable
            _symbolTable = new SymbolTable();
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

            TypeReferenceNode? leftType = GetTypeOf(node.Target);
            TypeReferenceNode? rightType = GetTypeOf(node.Value);

            if (leftType == null || rightType == null)
            {
                node.Status = ResolutionStatus.Failed;
                return;
            }

            // Ensure that the target and value have the same type
            if (leftType.FullyQualifiedName != rightType.FullyQualifiedName)
            {
                node.Target = new ErrorNode(
                    $"Type mismatch. No conversion from {rightType} to {leftType} possible.",
                    node.Target,
                    node
                );
            }
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

            var leftType = GetTypeOf(node.Left);
            var rightType = GetTypeOf(node.Right);

            if (leftType == null || rightType == null)
            {
                node.Status = ResolutionStatus.Failed;
                return;
            }

            if (leftType.FullyQualifiedName != rightType.FullyQualifiedName)
            {
                node.Left = new ErrorNode(
                    $"Type mismatch. No conversion from {rightType} to {leftType} possible.",
                    node.Left,
                    node
                );
            }
        }

        public void Visit(ClassNode node)
        {
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

        public void Visit(ErrorNode node)
        {
            throw new NotImplementedException();
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
                if (param.Type is TypeReferenceNode type)
                {
                    var actualType = CheckNodeType(type);

                    if (actualType != null)
                    {
                        param.Type = actualType;
                    }
                }
                else if (param.Type is MemberAccessNode memberAccess)
                {
                    var actualType = CheckNodeType(memberAccess);

                    if (actualType != null)
                    {
                        param.Type = actualType;
                    }
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
                typeRef.Module = typeSymbol.Parent.Name;
                typeRef.TypeKind = Utils.SymbolKindToASTKind(typeSymbol.TypeKind);
                typeRef.Status = ResolutionStatus.Resolved;
            }
            else
            {
                node.Status = ResolutionStatus.Failed;
            }

            Console.WriteLine(symbol);
        }

        public void Visit(ImportNode import)
        {
            throw new NotImplementedException();
        }

        public void Visit(InitNode node)
        {
            // Check the parameters if they have a (known) type
            foreach (var param in node.Parameters)
            {
                if (param.Type is TypeReferenceNode type)
                {
                    var actualType = CheckNodeType(type);

                    if (actualType != null)
                    {
                        param.Type = actualType;
                    }
                    else
                    {
                        param.Type = new ErrorNode(
                            "Unknown type",
                            param.Type,
                            node
                        );
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

        }

        public void Visit(MemberAccessNode node)
        {
            if (node.Status is INode.ResolutionStatus.Failed or INode.ResolutionStatus.Resolved)
            {
                return;
            }

            if (node.Left is IdentifierNode target)
            {
                if (target.Name == "self")
                {

                }
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
            throw new NotImplementedException();
        }

        public void Visit(OperatorNode node)
        {
            // Check the parameters if they have a (known) type
            foreach (var param in node.Parameters)
            {
                if (param.Type is TypeReferenceNode type)
                {
                    var actualType = CheckNodeType(type);

                    if (actualType != null)
                    {
                        param.Type = actualType;
                    }
                    else
                    {
                        param.Type = new ErrorNode(
                            "Unknown type",
                            param.Type,
                            node
                        );
                    }
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
            if (node.Status is INode.ResolutionStatus.Failed or INode.ResolutionStatus.Resolved)
            {
                return;
            }

            if (node.Object is IdentifierNode target)
            {
                if (target.Name == "self")
                {
                    var type = GetTypeOfSelf(target);

                    if (type != null)
                    {
                        type.Status = ResolutionStatus.Resolved;
                    }

                    Console.WriteLine(type);
                }
                else
                {
                    // Find the symbol `Object` in the current scope
                    var symbol = _symbolTable.FindBy(target);

                    Console.WriteLine(symbol);
                }
            }
        }

        public void Visit(PropertyNode node)
        {
            if (node.TypeNode is TypeReferenceNode type)
            {
                var actualType = CheckNodeType(type);
                node.TypeNode = actualType;
            }
        }

        public void Visit(ReturnNode node)
        {
            throw new NotImplementedException();
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
            else if (node.TypeNode is MemberAccessNode memberAccess)
            {
                var actualType = CheckNodeType(memberAccess);
                node.TypeNode = actualType;
            }
        }

        private INode? CheckNodeType(INode node)
        {
            if (node is TypeReferenceNode typeNode)
            {
                return CheckTypeReferenceNode(typeNode);
            }
            else if (node is PropAccessNode propAccess)
            {
                return CheckPropAccessNode(propAccess);
            }

            return null;
        }

        // ---- Helper methods ----
        private INode? CheckTypeReferenceNode(TypeReferenceNode typeNode)
        {
#if IONA_BOOTSTRAP
            TypeReferenceNode typeRef;
            switch (typeNode.Name)
            {
                case "bool":
                case "byte":
                case "decimal":
                case "double":
                case "float":
                case "int":
                case "long":
                case "nint":
                case "nuint":
                case "sbyte":
                case "short":
                case "string":
                case "uint":
                case "ulong":
                case "ushort":
                    typeRef = new TypeReferenceNode(typeNode.Name, typeNode.Parent);
                    typeRef.Module = "Primitives";
                    typeRef.Status = ResolutionStatus.Resolved;
                    typeRef.TypeKind = AST.Types.Kind.Struct;

                    return typeRef;
            }
#endif

            // We first need to find the scope this type reference is in(to find nested types, or the module)
            List<INode> nodeOrder = ((INode)typeNode).Hierarchy();

            // Now get the first module in the list (each file can only have one module)
            var file = (FileNode)nodeOrder[0];

            var module = file.Children.OfType<ModuleNode>().FirstOrDefault();

            if (module == null)
            {
                return null;
            }

            // Now we know the model and the scopes in correct order, we can traverse both the ast and the symbol table to find the type
            INode? currentScope = module;
            ISymbol? currentSymbol = _symbolTable.Modules.Find(mod => mod.Name == module.Name);

            // Ensure the module is in the symbol table
            if (currentSymbol == null)
            {
                return null;
            }

            var type = currentSymbol.Symbols.OfType<TypeSymbol>().FirstOrDefault(symbol => symbol.Name == typeNode.Name);

            if (type != null)
            {
                typeNode.Module = type.Parent.Name;
                typeNode.Status = ResolutionStatus.Resolved;
                typeNode.TypeKind = Utils.SymbolKindToASTKind(type.TypeKind);

                return typeNode;
            }

            return null;
        }

        private INode CheckPropAccessNode(PropAccessNode propAccess)
        {
            // We first need to find the scope this type reference is in(to find nested types, or the module)
            // Step 1: Check if the leftmost node is a module
            if (propAccess.Object is not IdentifierNode target)
            {
                return new ErrorNode(
                    "Invalid member access in type reference",
                    propAccess,
                    propAccess.Parent
                );
            }

            // Step 2: Find the module
            var isModule = _symbolTable.Modules.Any(mod => mod.Name == target.Name);

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

        private void CheckNode(INode node)
        {
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
                case ErrorNode errorNode:
                    errorNode.Accept(this);
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
                case InitNode initNode:
                    initNode.Accept(this);
                    break;
                case LiteralNode literalNode:
                    literalNode.Accept(this);
                    break;
                case MemberAccessNode memberAccessNode:
                    memberAccessNode.Accept(this);
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

        private TypeReferenceNode? GetTypeOfSelf(IdentifierNode self)
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
                        typeRef.Module = moduleNode.Name;

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

        private TypeReferenceNode? GetTypeOfPropAccess(PropAccessNode propAccess)
        {
            if (propAccess.Object is IdentifierNode target)
            {
                if (target.Name == "self")
                {
                    var type = GetTypeOfSelf(target);

                    if (type != null)
                    {
                        type.Status = ResolutionStatus.Resolved;
                    }

                    return type;
                }
                else
                {
                    // Find the symbol `Object` in the current scope
                    var symbol = _symbolTable.FindBy(target);

                    Console.WriteLine(symbol);
                }
            }
            else
            {
                return null;
            }

            if(propAccess.Property is PropAccessNode nestedPropAccess)
            {
                return GetTypeOfPropAccess(nestedPropAccess);
            }
            else if (propAccess.Property is IdentifierNode identifier)
            {
                return identifier.ResultType as TypeReferenceNode;
            }

            return null;
        }
    }
}
