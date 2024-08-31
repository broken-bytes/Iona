using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class ErrorNode : INode
    {
        public string Name { get; set; }
        public string Module { get; set; }
         public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public string Message { get; set; }


        public ErrorNode(string name, int line, int startColumn, int endColumn, string message, INode? parent)
        {
            Name = name;
            Module = "";
            Parent = parent;
            Type = NodeType.Error;
            Line = line;
            StartColumn = startColumn;
            EndColumn = endColumn;
            Message = message;
        }

        public void Accept(IErrorVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
