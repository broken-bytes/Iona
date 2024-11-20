using AST.Nodes;
using Symbols.Symbols;
using Symbols;
using AST.Visitors;
using AST.Types;
using Shared;
using System.Xml.Linq;

namespace Typeck
{
    internal class TopLevelScopeResolver :
        IAssignmentVisitor,
        IBinaryExpressionVisitor,
        IBlockVisitor,
        IClassVisitor,
        IFileVisitor,
        IFuncCallVisitor,
        IFuncVisitor,
        IIdentifierVisitor,
        IInitVisitor,
        ILiteralVisitor,
        IMemberAccessVisitor,
        IModuleVisitor,
        IOperatorVisitor,
        IPropAccessVisitor,
        IPropertyVisitor,
        IReturnVisitor,
        IStructVisitor,
        IVariableVisitor
    {
        private SymbolTable table;
        private readonly IErrorCollector errorCollector;
        private readonly IWarningCollector warningCollector;
        private readonly IFixItCollector fixItCollector;

        internal TopLevelScopeResolver(
            IErrorCollector errorCollector,
            IWarningCollector warningCollector,
            IFixItCollector fixItCollector
        )
        {
            this.table = new SymbolTable();
            this.errorCollector = errorCollector;
            this.warningCollector = warningCollector;
            this.fixItCollector = fixItCollector;
        }

        internal void CheckScopes(INode ast, SymbolTable table)
        {
            this.table = table;

            if (ast is FileNode file)
            {
                file.Accept(this);
            }
        }

        public void Visit(AssignmentNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            HandleNode(node.Target);
            HandleNode(node.Value);

            if (node.Value.Status == INode.ResolutionStatus.Failed)
            {
                var error = CompilerErrorFactory.UndefinedNameError(
                    $"{node.Value}",
                    node.Value.Meta
                );
                errorCollector.Collect(error);

                return;
            }

            if (node.Target.Status == INode.ResolutionStatus.Resolved && node.Value.Status == INode.ResolutionStatus.Resolved)
            {
                node.Status = INode.ResolutionStatus.Resolved;
            }
        }

        public void Visit(BinaryExpressionNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            HandleNode(node.Left);
            HandleNode(node.Right);
        }

        public void Visit(BlockNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            foreach (var child in node.Children)
            {
                HandleNode(child);
            }
        }

        public void Visit(ClassNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
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

            // Check if the types of the parameters are in scope and assign the actual type symbol to them
            foreach (var param in node.Parameters)
            {
                ResolveParameter(param);
            }

            if (node.ReturnType is not null)
            {
                var symbol = FindTypeSymbol(node.ReturnType);

                if (symbol.IsSuccess)
                {
                    node.ReturnType.FullyQualifiedName = symbol.Success!.FullyQualifiedName;
                    node.ReturnType.TypeKind = Utils.SymbolKindToASTKind(symbol.Success!.TypeKind);
                }
            }

            var isResolved = node.Parameters.TrueForAll(p => p.TypeNode.Status == INode.ResolutionStatus.Resolved);

            if (node.Body != null)
            {
                node.Body.Accept(this);
                isResolved &= node.Body.Status == INode.ResolutionStatus.Resolved;
            }

            if (isResolved)
            {
                node.Status = INode.ResolutionStatus.Resolved;
            }
        }

        public void Visit(FuncCallNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;
            // Check if the function is in scope
            var funcWasFound = table.CheckIfFuncExists(node.Root, node);

            // Free function or member function
            if (funcWasFound)
            {
                // Check if the function is a member function or a free function
                var hierarchy = ((INode)node).Hierarchy();
                var currentType = hierarchy.OfType<ITypeNode>().FirstOrDefault();

                var typeSymbol = table.FindTypeByFQN(node.Root, currentType.FullyQualifiedName);

                // Function is inside a type -> member function
                if (typeSymbol != null)
                {
                    var self = new SelfNode(node.Parent);
                    var target = new IdentifierNode(typeSymbol.Name, node.Target);
                    var methodCall = new MethodCallNode(self, target, node.Parent);
                    methodCall.Args = node.Args;
                    methodCall.Meta = node.Meta;
                    self.Parent = methodCall;
                    target.Parent = methodCall;

                    ReplaceNode(node, methodCall);
                }
            } 
            // Init
            else
            {   
                var initWasFound = table.CheckIfInitExists(node.Root, node, null);

                if (!initWasFound)
                {
                    node.Status = INode.ResolutionStatus.Failed;

                    return;
                }
                
                // When an init was found we implicitly know a type with that name exists.
                var typeSearchResult = table.FindTypeBySimpleName(node.Root, node.Target.Value);

                if (typeSearchResult.IsSuccess)
                {
                    var initCall = new InitCallNode(typeSearchResult.Unwrapped().FullyQualifiedName, node.Parent);
                    initCall.Args = node.Args;
                    initCall.Meta = node.Meta;

                    ReplaceNode(node, initCall);
                }
            }
        }

        public void Visit(IdentifierNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Value == "self")
            {
                ReplaceNode(node, new SelfNode(node.Parent) { Meta = node.Meta, Status = INode.ResolutionStatus.Resolved });

                return;
            }

            // Check if the identifier is in the current (or parent) scope
            var symbol = table.FindBy(node);

            if (symbol == null)
            {
                node.Status = INode.ResolutionStatus.Failed;
            }
        }

        public void Visit(InitNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            // Check if the types of the parameters are in scope and assign the actual type symbol to them
            foreach (var param in node.Parameters)
            {
                ResolveParameter(param);
            }

            var isResolved = node.Parameters.TrueForAll(p => p.TypeNode.Status == INode.ResolutionStatus.Resolved);

            if (node.Body != null)
            {
                node.Body.Accept(this);
                isResolved &= node.Body.Status == INode.ResolutionStatus.Resolved;
            }

            if (isResolved)
            {
                node.Status = INode.ResolutionStatus.Resolved;
            }
        }

        public void Visit(LiteralNode node)
        {
            node.Status = INode.ResolutionStatus.Resolved;
        }

        public void Visit(MemberAccessNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            HandleNode(node.Left);
        }

        public void Visit(ModuleNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            foreach (var child in node.Children)
            {
                HandleNode(child);
            }
        }

        public void Visit(OperatorNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            // Check if the types of the parameters are in scope
            foreach (var param in node.Parameters)
            {
                ResolveParameter(param);
            }

            if (node.ReturnType is not null)
            {
                var symbol = FindTypeSymbol(node.ReturnType);

                if (symbol.IsSuccess)
                {
                    node.ReturnType.FullyQualifiedName = symbol.Success!.FullyQualifiedName;
                    node.ReturnType.TypeKind = Utils.SymbolKindToASTKind(symbol.Success!.TypeKind);
                }
            }

            var isResolved = node.Parameters.TrueForAll(p => p.TypeNode.Status == INode.ResolutionStatus.Resolved);

            if (node.Body != null)
            {
                node.Body.Accept(this);
                isResolved &= node.Body.Status == INode.ResolutionStatus.Resolved;
            }

            if (isResolved)
            {
                node.Status = INode.ResolutionStatus.Resolved;
            }
        }

        public void Visit(PropAccessNode node)
        {
            HandleNode(node.Object);
            node.Status = node.Status = INode.ResolutionStatus.Resolving;
        }

        public void Visit(PropertyNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Value != null)
            {
                HandleNode(node.Value);
            }
            
            if (node.TypeNode != null)
            {
                var symbol = FindTypeSymbol(node.TypeNode);

                if (symbol.IsSuccess)
                {
                    node.TypeNode.FullyQualifiedName = symbol.Success!.FullyQualifiedName;
                    node.TypeNode.TypeKind = Utils.SymbolKindToASTKind(symbol.Success!.TypeKind);
                    node.TypeNode.Assembly = symbol.Success!.Assembly;
                }
                else
                {
                    node.Status = INode.ResolutionStatus.Failed;
                    errorCollector.Collect(CompilerErrorFactory.TopLevelDefinitionError(node.TypeNode.Name, node.Meta));
                }
            }
        }

        public void Visit(ReturnNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Value != null)
            {
                HandleNode(node.Value);
            }
        }

        public void Visit(StructNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(VariableNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Value != null)
            {
                HandleNode(node.Value);
            }
        }

        private void HandleNode(INode node)
        {
            switch (node)
            {
                case AssignmentNode assignment:
                    assignment.Accept(this);
                    break;
                case BinaryExpressionNode binary:
                    binary.Accept(this);
                    break;
                case BlockNode block:
                    block.Accept(this);
                    break;
                case ClassNode classNode:
                    classNode.Accept(this);
                    break;
                case FuncCallNode funcCall:
                    funcCall.Accept(this);
                    break;
                case FuncNode func:
                    func.Accept(this);
                    break;
                case IdentifierNode identifier:
                    identifier.Accept(this);
                    break;
                case InitNode init:
                    init.Accept(this);
                    break;
                case LiteralNode literal:
                    literal.Accept(this);
                    break;
                case MemberAccessNode member:
                    member.Accept(this);
                    break;
                case ModuleNode module:
                    module.Accept(this);
                    break;
                case OperatorNode op:
                    op.Accept(this);
                    break;
                case PropAccessNode propAccess:
                    propAccess.Accept(this);
                    break;
                case PropertyNode property:
                    property.Accept(this);
                    break;
                case ReturnNode returnNode:
                    returnNode.Accept(this);
                    break;
                case StructNode structNode:
                    structNode.Accept(this);
                    break;
                case VariableNode variableNode:
                    variableNode.Accept(this);
                    break;
            }
        }

        private void ResolveParameter(ParameterNode param)
        {
            var type = param.TypeNode;
            
            var result = FindTypeSymbol(type);

            if (result.IsError)
            {
                param.Status = INode.ResolutionStatus.Failed;

                var error = CompilerErrorFactory.TopLevelDefinitionError(
                    type.Name,
                    type.Meta
                );

                errorCollector.Collect(error);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Result<TypeSymbol, SymbolError> FindTypeSymbol(TypeReferenceNode? typeNode)
        {
            if (typeNode == null)
            {
                return Result<TypeSymbol, SymbolError>.Err(new SymbolError("Unknown type"));
            }
            var symbol = table.FindTypeBy(typeNode.Root, typeNode, null);

            if (symbol == null)
            {
                return Result<TypeSymbol, SymbolError>.Err(new SymbolError("Unknown type"));
            }
            
            return Result<TypeSymbol, SymbolError>.Ok(symbol);
        }
        
        private void ReplaceNode(INode node, INode newNode)
        {
            // Case 1: Node is inside a block
            if (node.Parent is BlockNode block)
            {
                var index = block.Children.IndexOf(node);
                block.Children[index] = newNode;
            }
            // Case 2: Node is value of a variable decl
            else if (node.Parent is VariableNode variable)
            {
                variable.Value = (IExpressionNode)newNode;
            }
            // Case 3: Node is value of a property decl
            else if (node.Parent is PropertyNode property)
            {
                property.Value = (IExpressionNode)newNode;
            }
            // Case 4: Node is value of a return statement
            else if (node.Parent is ReturnNode returnNode)
            {
                returnNode.Value = (IExpressionNode)newNode;
            }
            // Case 5: Node is value of an assignment
            else if (node.Parent is AssignmentNode assignment)
            {
                if (assignment.Target == node)
                {
                    assignment.Target = newNode;
                }
                else
                {
                    assignment.Value = newNode;
                }
            }
            // Case 6: Node is value of a binary expression
            else if (node.Parent is BinaryExpressionNode binary)
            {
                if (binary.Left == node)
                {
                    binary.Left = (IExpressionNode)newNode;
                }
                else
                {
                    binary.Right = (IExpressionNode)newNode;
                }
            }
            // Case 7: Node is the object or prop of a property access node
            else if (node.Parent is PropAccessNode propAccess)
            {
                if (propAccess.Object == node)
                {
                    propAccess.Object = (IExpressionNode)newNode;
                }
                else
                {
                    propAccess.Property = (IExpressionNode)newNode;
                }
            }
        }
    }
}
