using AST.Nodes;
using AST.Visitors;
using Symbols;
using Symbols.Symbols;
using System.Net.Http.Headers;

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

            // Both must be expressions, so cast them to get the expression result
            if (node.Target is not IExpressionNode target || node.Value is not IExpressionNode value)
            {
                return;
            }

            // Check if the types are the same
            if (target.ResultType != value.ResultType)
            {
                // Add an error node
                return;
            }
        }

        public void Visit(BlockNode node)
        {
            foreach (var child in node.Children)
            {
                switch (child)
                {
                    case BlockNode block:
                        block.Accept(this);
                        break;
                    case ClassNode classNode:
                        classNode.Accept(this);
                        break;
                    case ContractNode contract:
                        contract.Accept(this);
                        break;
                    case FuncCallNode funcCall:
                        funcCall.Accept(this);
                        break;
                    case FuncNode func:
                        func.Accept(this);
                        break;
                    case InitNode init:
                        init.Accept(this);
                        break;
                    case PropertyNode property:
                        property.Accept(this);
                        break;
                    case StructNode structNode:
                        structNode.Accept(this);
                        break;
                    case VariableNode variable:
                        variable.Accept(this);
                        break;
                    default:
                        break;
                }
            }
        }

        public void Visit(BinaryExpressionNode node)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
                        param.Type = new ErrorNode("Unknown type");
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            else if (node is MemberAccessNode memberAccessNode)
            {
                return CheckMemberAccessNode(memberAccessNode);
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
                    typeRef.FullyQualifiedName = typeNode.Name;

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
                typeNode.FullyQualifiedName = type.Parent.Name + "." + type.Name;
                return typeNode;
            }

            return null;
        }

        private INode CheckMemberAccessNode(MemberAccessNode memberAccessNode)
        {
            // We first need to find the scope this type reference is in(to find nested types, or the module)
            // Step 1: Check if the leftmost node is a module
            if (memberAccessNode.Target is not IdentifierNode target)
            {
                return new ErrorNode("Invalid member access in type reference");
            }

            // Step 2: Find the module
            var isModule = _symbolTable.Modules.Any(mod => mod.Name == target.Name);

            // Edge case: If the target is indeed a module, we still need to check if the module has a type with the same name
            // To do so we need to do the following:
            // - Check if the the member node is an identifier (if not we cannot mean the type within the module but rather the module)
            // - If it does, we need to further check that the next node is not the type of that very same name
            if (isModule)
            {
                if (memberAccessNode.Member is not IdentifierNode member)
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
    }
}
