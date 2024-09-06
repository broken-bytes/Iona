using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using Parser.Parsers;
using System.IO;

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

        public bool isCompoundExpression(TokenStream stream)
        {
            var tokens = stream.Peek(3);

            if (
                (tokens[0].Family == TokenFamily.Identifier) &&
                IsCompoundOperator(tokens[1]) &&
                (tokens[2].Family == TokenFamily.Identifier || tokens[2].Family == TokenFamily.Literal)
            )
            {
                return true;
            }

            return false;
        }

        public bool IsCompoundOperator(Token token)
        {
            // If the token is not an operator, it cannot be a compound operator
            if (token.Family != TokenFamily.Operator)
            {
                return false;
            }

            // Check what token it is
            switch (token.Value)
            {
                case "+=":
                case "-=":
                case "*=":
                case "/=":
                case "%=":
                    return true;
                default:
                    return false;
            }
        }

        public bool IsExpression(TokenStream stream)
        {
            return IsBinaryExpression(stream) || IsUnaryExpression(stream) || IsComparisonExpression(stream);
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

        public AccessLevel ParseAccessLevel(TokenStream stream)
        {
            // Check if the contract has an access modifier
            AccessLevel accessLevel = AccessLevel.Internal;
            var token = stream.Peek();

            while(token.Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                token = stream.Peek();
            }

            if (token.Type == TokenType.Public || token.Type == TokenType.Private || token.Type == TokenType.Internal)
            {
                switch (token.Type)
                {
                    case TokenType.Public:
                        accessLevel = AccessLevel.Public;
                        break;
                    case TokenType.Private:
                        accessLevel = AccessLevel.Private;
                        break;
                    case TokenType.Internal:
                        accessLevel = AccessLevel.Internal;
                        break;
                }

                // Consume the access modifier
                stream.Consume(token.Type, TokenFamily.Keyword);
            }

            return accessLevel;
        }

        public List<GenericArgument> ParseGenericArgs(TokenStream stream)
        {
            var args = new List<GenericArgument>();

            if(stream.Peek().Type != TokenType.Less)
            {
                return args;
            }

            while(stream.Peek().Type != TokenType.Greater)
            {
                var token = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
                var arg = new GenericArgument { Name = token.Value };

                // Check if the generic argument has constraints
                // TODO: Implement constraints

                args.Add(arg);

                if (stream.Peek().Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenFamily.Operator);
                }
            }

            return args;
        }
    }
}
