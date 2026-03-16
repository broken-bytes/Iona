using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class GuardNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        /// <summary>
        /// The condition expression (must resolve to Bool).
        /// Null when this is a binding guard (guard var/let).
        /// </summary>
        public IExpressionNode? Condition { get; set; }

        /// <summary>
        /// The else body that executes when the condition is false or the binding is null.
        /// Must contain an early exit (return, break, continue).
        /// </summary>
        public BlockNode Body { get; set; }

        /// <summary>
        /// The variable name to bind the unwrapped value to.
        /// Null when this is a condition guard.
        /// </summary>
        public string? BindingName { get; set; }

        /// <summary>
        /// Whether the binding is mutable (var) or immutable (let).
        /// </summary>
        public bool BindingIsMutable { get; set; }

        /// <summary>
        /// The expression to unwrap for binding guards.
        /// Null when this is a condition guard.
        /// </summary>
        public IExpressionNode? BindingExpression { get; set; }

        /// <summary>
        /// The resolved type of the unwrapped binding variable.
        /// Set during type checking.
        /// </summary>
        public TypeReferenceNode? BindingTypeNode { get; set; }

        /// <summary>
        /// Creates a condition guard: guard condition else { body }
        /// </summary>
        public GuardNode(IExpressionNode condition, BlockNode body, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.GuardStatement;
            StatementType = StatementType.Guard;
            Condition = condition;
            Body = body;
        }

        /// <summary>
        /// Creates a binding guard: guard var/let name = expression else { body }
        /// </summary>
        public GuardNode(string bindingName, bool isMutable, IExpressionNode bindingExpression, BlockNode body, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.GuardStatement;
            StatementType = StatementType.Guard;
            BindingName = bindingName;
            BindingIsMutable = isMutable;
            BindingExpression = bindingExpression;
            Body = body;
        }

        public void Accept(IGuardVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
