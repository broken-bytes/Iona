using AST.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbols.Symbols
{
    public class AssemblySymbol : ISymbol
    {
        public string Name { get; set; }
        public string FullName => Name;
        public List<ISymbol> Symbols { get; set; } = new List<ISymbol>();
        public SymbolKind Kind { get; set; } = SymbolKind.Assembly;
        public ISymbol? Parent { get; set; } = null;

        public AssemblySymbol(string name)
        {
            Name = name;
        }
    }
}
