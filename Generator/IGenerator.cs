using AST.Nodes;
using Symbols;

namespace Generator
{
    public interface IGenerator
    {
        public Assembly CreateAssembly(string name, SymbolTable table);
    }
}
