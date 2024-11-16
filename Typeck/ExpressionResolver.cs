using System.Reflection.Metadata;
using AST;
using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Shared;
using Symbols;
using Symbols.Symbols;

namespace Typeck
{
    internal class ExpressionResolver :
        IAssignmentVisitor,
        IBinaryExpressionVisitor,
        IBlockVisitor,
        IClassVisitor,
        IFileVisitor,
        IFuncVisitor,
        IInitVisitor,
        ILiteralVisitor,
        IModuleVisitor,
        IOperatorVisitor,
        IPropAccessVisitor,
        IPropertyVisitor,
        IStructVisitor
    {
        private SymbolTable table;
        private IErrorCollector _errorCollector;
        /// The type an expression shall resolve to. Used in stuff liek assignments or parameters
        private string? _contextualTypeFqn;

        internal ExpressionResolver(IErrorCollector errorCollector)
        {
            table = new SymbolTable();
            _errorCollector = errorCollector;
        }

        internal void CheckScopes(INode ast, SymbolTable table)
        {
            this.table = table;
            CheckNode(ast);
        }

        public void Visit(AssignmentNode node)
        {
            if (node.Target is PropAccessNode propAccess)
            {
                _contextualTypeFqn = propAccess.ResultType.FullyQualifiedName;
            }
            else if (node.Target is IdentifierNode ident)
            {
                _contextualTypeFqn = ident.ResultType.FullyQualifiedName;
            }
            else if (node.Target is ScopeResolutionNode scope)
            {
                _contextualTypeFqn = scope.ResultType.FullyQualifiedName;
            }
            
            CheckNode(node.Value);
            _contextualTypeFqn = null;
        }

        public void Visit(BinaryExpressionNode node)
        {
            CheckNode(node.Left);
            CheckNode(node.Right);

            if (node.Left.ResultType is null || node.Right.ResultType is null)
            {
                return;
            }
            
            if (node.Left.ResultType?.FullyQualifiedName == node.Right.ResultType?.FullyQualifiedName)
            {
                node.Status = INode.ResolutionStatus.Resolved;
                
                return;
            }

            var leftType = table.FindTypeBy(node.Left.ResultType.Name, null);
            var rightType = table.FindTypeBy(node.Right.ResultType.Name, null);

            if (leftType is null || rightType is null)
            {
                return;
            }
            
            var leftOp = FindMatchingOperator(leftType, leftType.FullyQualifiedName, rightType.FullyQualifiedName, _contextualTypeFqn);
            var rightOp = FindMatchingOperator(rightType, leftType.FullyQualifiedName, rightType.FullyQualifiedName,  _contextualTypeFqn);

            if (leftOp is not null)
            {
                node.Status = INode.ResolutionStatus.Resolved;
                
                return;
            }
            
            if (rightOp is not null)
            {
                node.Status = INode.ResolutionStatus.Resolved;
                
                return;
            }

            node.Status = INode.ResolutionStatus.Failed;
            
            var error = CompilerErrorFactory.NoBinaryOverload(
                node.Operation.CSharpOperator(),
                node.Left.ResultType.Name,
                node.Right.ResultType.Name,
                _contextualTypeFqn,
                node.Meta
            );
            
            _errorCollector.Collect(error);
        }

        public void Visit(BlockNode node)
        {
            foreach (var child in node.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(ClassNode node)
        {
            if (node.Body is BlockNode blockNode)
            {
                CheckNode(blockNode);
            }
        }
        
        public void Visit(FileNode node)
        {
            foreach (var child in node.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(FuncNode node)
        {
            if (node.Body == null)
            {
                return;
            }

            foreach (var child in node.Body.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(InitNode node)
        {
            if (node.Body == null)
            {
                return;
            }

            foreach (var child in node.Body.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(LiteralNode node)
        {
            var typeNode = new TypeReferenceNode(node.LiteralType.Name(), node)
            {
                FullyQualifiedName = $"Iona.Builtins.{node.LiteralType.Name()}",
                Assembly = "Iona.Builtins",
                TypeKind = Kind.Struct
            };
            
            node.ResultType = typeNode;
        }

        public void Visit(ModuleNode node)
        {
            foreach (var child in node.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(OperatorNode node)
        {
            if (node.Body == null)
            {
                return;
            }

            foreach (var child in node.Body.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(PropAccessNode node)
        {
            
        }

        public void Visit(PropertyNode node)
        {
            if (node.Value != null)
            {
                // Resolve the value
                CheckNode(node.Value);
                
                // Two cases:
                // - The node does not have a type defined -> Use value as inferred type
                // - The node has a fixed type -> Compare type and assigned expression
                if (node.TypeNode is TypeReferenceNode typeNode)
                {
                    if (typeNode.FullyQualifiedName == node.Value.ResultType.FullyQualifiedName)
                    {
                        node.Status = INode.ResolutionStatus.Resolved;
                        
                        return;
                    }
                    
                    node.Status = INode.ResolutionStatus.Failed;
                    // TODO: Print assignment error
                    
                    return;
                }

                node.TypeNode = node.Value.ResultType;
                
                return;
            }

            if (node.TypeNode is null)
            {
                node.Status = INode.ResolutionStatus.Failed;

                var error = CompilerErrorFactory.MissingTypeAnnotation(node.Name, node.Meta);
                _errorCollector.Collect(error);
            }
            else
            {
                node.Status = INode.ResolutionStatus.Resolved;
            }
        }
        
        public void Visit(StructNode node)
        {
            if (node.Body == null)
            {
                return;
            }

            foreach (var child in node.Body.Children)
            {
                CheckNode(child);
            }
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
                case FileNode fileNode:
                    fileNode.Accept(this);
                    break;
                case FuncNode funcNode:
                    funcNode.Accept(this);
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
                case OperatorNode operatorNode:
                    operatorNode.Accept(this);
                    break;
                case PropAccessNode propAccessNode:
                    propAccessNode.Accept(this);
                    break;
                case PropertyNode propertyNode:
                    propertyNode.Accept(this);
                    break;
                case StructNode structNode:
                    structNode.Accept(this);
                    break;
            }
        }

        /// <summary>
        /// Tries to find an
        /// </summary>
        /// <param name="type"></param>
        /// <param name="leftFqn"></param>
        /// <param name="rightFqn"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        private OperatorSymbol? FindMatchingOperator(TypeSymbol type, string leftFqn, string rightFqn, string? reference)
        {
            var leftOp = type
                .Symbols
                .OfType<OperatorSymbol>()
                .FirstOrDefault(op =>
                    {
                        var parameters = op.Symbols.OfType<ParameterSymbol>().ToList();

                        if (parameters.Count != 2)
                        {
                            return false;
                        }
                        
                        if (
                            parameters[0].Type.FullyQualifiedName == leftFqn && 
                            parameters[1].Type.FullyQualifiedName == rightFqn
                        )
                        {
                            // We need to check if we have a required return type and match against it if so

                            if (reference is not null)
                            {
                                return op.ReturnType.FullyQualifiedName == reference;
                            }
                            
                            return true;
                        }

                        return false;
                    }
                );

            return leftOp;
        }
    }
}
