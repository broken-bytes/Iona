using AST.Types;
using AST.Visitors;

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


        public ErrorNode(int line, int startColumn, int endColumn, string file, string message, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.Error;
            Line = line;
            StartColumn = startColumn;
            EndColumn = endColumn;
            File = file;
            Message = message;
        }

        public void Accept(IErrorVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
