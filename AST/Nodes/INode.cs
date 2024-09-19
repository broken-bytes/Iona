using AST.Types;

namespace AST.Nodes
{
    public interface INode
    {
        public struct Metadata
        {
            public string File { get; set; }
            public int LineStart { get; set; }
            public int LineEnd { get; set; }
            public int ColumnStart { get; set; }
            public int ColumnEnd { get; set; }
        }

        INode? Parent { get; set; }
        NodeType Type { get; set; }
        INode Root { get; }
        Metadata Meta { get; set; }
    }
}
