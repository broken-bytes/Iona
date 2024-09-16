using AST.Nodes;

namespace Generator
{
    public interface IGenerator
    {
        public string GenerateCIL(INode node);
    }
}
