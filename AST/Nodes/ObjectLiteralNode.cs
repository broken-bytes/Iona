using AST.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Nodes
{
    public class ObjectLiteralNode : INode
    {
        public struct Argument
        {
            public string Name;
            public IExpressionNode Value;
        }

        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public IdentifierNode Target { get; set; }
        public List<Argument> Arguments { get; set; }

        public ObjectLiteralNode(IdentifierNode target, List<Argument> arguments, INode parent)
        {
            Name = target.Name;
            Module = target.Module;
            Parent = parent;
            Type = NodeType.ObjectLiteral;
            Target = target;
            Arguments = arguments;
        }
    }
}
