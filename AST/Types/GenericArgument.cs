namespace AST.Types
{
    public struct GenericArgument
    {
        public string Name { get; set; }
        public List<GenericConstraint> Constraints { get; set; }
    }
}
