namespace AST.Types
{
    public enum BinaryOperation
    {
        Add,
        And,
        Divide,
        Equal,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Mod,
        Multiply,
        Or,
        Noop,
        NotEqual,
        Subtract,
    }
    
    public static class BinaryOperationExtensions
    {
        public static string CSharpOperator(this BinaryOperation operation)
        {
            switch (operation)
            {
                case BinaryOperation.Add:
                    return "+";
                case BinaryOperation.Subtract:
                    return "-";
                case BinaryOperation.Multiply:
                    return "*";
                case BinaryOperation.Divide:
                    return "/";
                case BinaryOperation.Mod:
                    return "%";
                case BinaryOperation.Equal:
                    return "==";
                case BinaryOperation.NotEqual:
                    return "!=";
                case BinaryOperation.GreaterThan:
                    return ">";
                case BinaryOperation.GreaterThanOrEqual:
                    return ">=";
                case BinaryOperation.LessThan:
                    return "<";
                case BinaryOperation.LessThanOrEqual:
                    return "<=";
                case BinaryOperation.And:
                    return "&&";
                case BinaryOperation.Or:
                    return "||";
                case BinaryOperation.Noop:
                    return "";  // No operation (noop), can return empty or custom
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
