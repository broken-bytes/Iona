using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ErrorNode : INode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public string File { get; set; }
        public string Message { get; set; }
        public Metadata Meta { get; set; }


        public ErrorNode(string message, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.Error;
            Line = 0;
            StartColumn = 0;
            EndColumn = 0;
            File = "";
            Message = message;
        }

        public void Accept(IErrorVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
