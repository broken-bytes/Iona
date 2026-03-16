using AST.Nodes;

namespace AST.Visitors
{
    public interface IRecordVisitor
    {
        public void Visit(RecordNode node);
    }
}
