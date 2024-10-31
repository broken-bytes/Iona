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
        public string FullyQualifiedName { get; }
        public TypeReferenceKind ReferenceKind { get; }
    }
}
