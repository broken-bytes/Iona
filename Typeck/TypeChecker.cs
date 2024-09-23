using AST.Nodes;
using AST.Visitors;
using Symbols;
using Symbols.Symbols;

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
        IPropertyVisitor,
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

        internal void TypeCheckAST(ref FileNode file, SymbolTable table)
        {
            _symbolTable = table;

            file.Accept(this);
        }

        public void Visit(AssignmentNode node)
        {
            throw new NotImplementedException();
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
                    var actualType = CheckTypeReference(type);

                    if (actualType != null)
                    {
                        param.Type = actualType;
                    }
                }
                else if (param.Type is MemberAccessNode memberAccess)
                {
                    var actualType = CheckTypeReference(memberAccess);

                    if (actualType != null)
                    {
                        param.Type = actualType;
                    }
                }
            }

            // Check the return type
            if (node.ReturnType is TypeReferenceNode returnType)
            {
                var actualType = CheckTypeReference(returnType);

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
                    var actualType = CheckTypeReference(type);

                    if (actualType != null)
                    {
                        param.Type = actualType;
                    }
                    else
                    {
                        param.Type = new TypeReferenceNode("None", node);
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
            throw new NotImplementedException();
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

        public void Visit(PropertyNode node)
        {
            if (node.TypeNode is TypeReferenceNode type)
            {
                var actualType = CheckTypeReference(type);
                node.TypeNode = actualType;
            }
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
                var actualType = CheckTypeReference(type);
                node.TypeNode = actualType;
            }
        }

        // ---- Helper methods ----
        private INode? CheckTypeReference(INode node)
        {
            if (node is not TypeReferenceNode typeNode)
            {
                return null;
            }

#if IONA_BOOTSTRAP
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
                    return typeNode;
            }
#endif

            // We first need to find the scope this type reference is in(to find nested types, or the module)
            List<INode> nodeOrder = node.Hierarchy();

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
                return typeNode;
            }

            return null;
        }
    }
}
