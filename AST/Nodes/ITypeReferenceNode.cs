using AST.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Nodes
{
    public enum TypeReferenceKind
    {
        Array,
        Concrete,
        Generic
    }

    public interface ITypeReferenceNode : INode
    {
        public string Name { get; set; }
        public string FullyQualifiedName { get; set; }
        public TypeReferenceKind ReferenceKind { get; }
        public Kind TypeKind { get; set; }
    }
}
