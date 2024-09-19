using AST.Nodes;
using AST.Types;
using AST.Visitors;
using System.Reflection.PortableExecutable;
using Typeck.Symbols;

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
            // We first need to find the scope this type reference is in(to find nested types, or the module)
            var nodeOrder = new List<INode>();

            INode current = node;

            while (current.Parent != null)
            {
                nodeOrder.Add(current);
                current = current.Parent;
            }

            // Reverse the list so we start at the root
            nodeOrder.Reverse();

            // Now get the first module in the list (each file can only have one module)
            var module = (ModuleNode)nodeOrder[0];

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

            // Traverse the scopes
            for (int x = 0; x < nodeOrder.Count; x++)
            {
                // If the node is an member access, we need to check if the left side is a type in the current module, or a different module
                // If it is a different module, we need to find the module in the symbol table and check if the right side is a type in that module
                if (nodeOrder[x] is MemberAccessNode member)
                {
                    var left = currentSymbol.Symbols.Find(sym => sym is TypeSymbol && sym.Name == ((IdentifierNode)member.Root).Name);

                    if (left != null)
                    {
                        Console.WriteLine(left);
                    }
                }
                else if (nodeOrder[x] is TypeReferenceNode type)
                {
                    var symbol = currentSymbol.Symbols.Find(sym => sym is TypeSymbol && sym.Name == type.Name);

                    if (symbol != null)
                    {
                        return type;
                    }

                    // If the type is not found in the current module, we need to check if we find it in an imported module
                    foreach (var import in ((FileNode)module.Root).Children.OfType<ImportNode>().Select(import => import.Name))
                    {
                        var importedModule = _symbolTable.Modules.Find(mod => mod.Name == import);

                        if (importedModule != null)
                        {
                            var importedSymbol = importedModule.Symbols.Find(sym => sym is TypeSymbol && sym.Name == type.Name);

                            if (importedSymbol == null)
                            {
                                // Add an error to the node
                                var errorNode = new ErrorNode($"Type {type.Name} not found. Are you missing an import?", type);
                                errorNode.Parent = type.Parent;

                                return errorNode;
                            }

                            return node;
                        }
                    }
                }
            }

            return null;
        }

        private ModuleNode? GetModuleForNode(INode node)
        {
            if (node is ModuleNode or FileNode)
            {
                return null;
            }


            INode current = node;

            while (current.Parent != null)
            {
                if (current is ModuleNode module)
                {
                    return module;
                }

                current = current.Parent;
            }

            return null;
        }
    }
}
