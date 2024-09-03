using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser
{
    public interface IParser
    {
        public INode Parse(TokenStream stream);

        public BinaryOperation? GetBinaryOperation(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Plus:
                    return BinaryOperation.Add;
                case TokenType.Minus:
                    return BinaryOperation.Subtract;
                case TokenType.Multiply:
                    return BinaryOperation.Multiply;
                case TokenType.Divide:
                    return BinaryOperation.Divide;
                case TokenType.Modulo:
                    return BinaryOperation.Mod;
            }

            return null;
        }

        public ComparisonOperation? GetComparisonOperation(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Equal:
                    return ComparisonOperation.Equal;
                case TokenType.NotEqual:
                    return ComparisonOperation.NotEqual;
                case TokenType.Greater:
                    return ComparisonOperation.GreaterThan;
                case TokenType.Less:
                    return ComparisonOperation.LessThan;
                case TokenType.GreaterEqual:
                    return ComparisonOperation.GreaterThanOrEqual;
                case TokenType.LessEqual:
                    return ComparisonOperation.LessThanOrEqual;
            }

            return null;
        }

        public UnaryOperation? GetUnaryOperation(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Increment:
                    return UnaryOperation.Increment;
                case TokenType.Decrement:
                    return UnaryOperation.Decrement;
                case TokenType.Not:
                    return UnaryOperation.Not;
            }

            return null;
        }

        public bool IsBinaryExpression(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (
                (tokens[0].Family == TokenFamily.Identifier || tokens[0].Family == TokenFamily.Literal) &&
                IsBinaryOperator(tokens[1])
            )
            {
                return true;
            }

            return false;
        }

        public bool IsBinaryOperator(Token token)
        {
            // If the token is not an operator, it cannot be a binary operator
            if (token.Family != TokenFamily.Operator)
            {
                return false;
            }

            // Check what token it is
            switch (token.Value)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                case "%":
                case "==":
                case "!=":
                case ">":
                case "<":
                case ">=":
                case "<=":
                case "&&":
                case "||":
                    return true;
                default:
                    return false;
            }
        }

        public bool IsComparisonExpression(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (
                (tokens[0].Family == TokenFamily.Identifier || tokens[0].Family == TokenFamily.Literal) &&
                IsComparisonOperator(tokens[1])
            )
            {
                return true;
            }

            return false;
        }

        public bool IsComparisonOperator(Token token)
        {
            // If the token is not an operator, it cannot be a comparison operator
            if (token.Family != TokenFamily.Operator)
            {
                return false;
            }

            // Check what token it is
            switch (token.Value)
            {
                case "==":
                case "!=":
                case ">":
                case "<":
                case ">=":
                case "<=":
                    return true;
                default:
                    return false;
            }
        }

        public bool IsUnaryExpression(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (
                (tokens[0].Family == TokenFamily.Identifier || tokens[0].Family == TokenFamily.Literal) &&
                IsUnaryOperator(tokens[1])
            )
            {
                return true;
            }

            return false;
        }

        public bool IsUnaryOperator(Token token)
        {
            // If the token is not an operator, it cannot be a unary operator
            if (token.Family != TokenFamily.Operator)
            {
                return false;
            }

            // Check what token it is
            switch (token.Value)
            {
                case "++":
                case "--":
                case "!":
                    return true;
                default:
                    return false;
            }
        }
    }
}
