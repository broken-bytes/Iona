namespace AST.Types
{
    public struct GenericConstraint
    {
        public GenericCondition Condition { get; set; }
        public IType? Type { get; set; }
    }
}
