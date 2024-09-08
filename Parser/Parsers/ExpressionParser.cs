using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using System.Linq.Expressions;

namespace Parser.Parsers
{
    public class ExpressionParser : IParser
    {
        internal ExpressionParser()
        {
        }

        public INode Parse(TokenStream stream, INode? parent)
        {
            if (IsBinaryExpression(stream))
            {
                return ParseBinaryExpression(stream, parent);
            }
            else if (IsUnaryExpression(stream))
            {
                return ParseUnaryExpression(stream, parent);
            }
            else if(IsComparisonExpression(stream))
            {
                return ParseComparisonExpression(stream, parent);
            }
            else if(IsFunctionCall(stream))
            {
                return ParseFuncCall(stream, parent);
            }
           
            return ParsePrimaryExpression(stream, parent);
        }

        public bool IsExpression(TokenStream stream)
        {
            if (
                IsBinaryExpression(stream) || 
                IsUnaryExpression(stream) || 
                IsComparisonExpression(stream) || 
                IsFunctionCall(stream)
            )
            {
                return true;
            }

            return false;
        }

        // ------------------- Helper methods -------------------
        private INode ParseBinaryExpression(TokenStream stream, INode? parent)
        {
            try
            {
                var left = ParsePrimaryExpression(stream, parent);
                var op = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);
                var right = ParsePrimaryExpression(stream, parent);

                // Get the operation for the token
                BinaryOperation? operation = GetBinaryOperation(op);

                return new BinaryExpressionNode(left, right, operation ?? BinaryOperation.Noop, null, parent);
            }
            catch (ParserException exception)
            {
                return new ErrorNode(
                    exception.Line,
                    exception.StartColumn,
                    exception.EndColumn,
                    exception.File,
                    exception.Message
                );
            }
        }

        private INode ParseComparisonExpression(TokenStream stream, INode? parent)
        {
            try
            {
                var left = ParsePrimaryExpression(stream, parent);
                var op = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);
                var right = ParsePrimaryExpression(stream, parent);

                // Get the operation for the token
                ComparisonOperation? operation = GetComparisonOperation(op);

                return new ComparisonExpressionNode(left, right, operation ?? ComparisonOperation.Noop, parent);
            }
            catch (ParserException exception)
            {
                return new ErrorNode(
                    exception.Line,
                    exception.StartColumn,
                    exception.EndColumn,
                    exception.File,
                    exception.Message
                );
            }
        }

        private INode ParseUnaryExpression(TokenStream stream, INode? parent)
        {
            try
            {
                var op = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);
                var right = ParsePrimaryExpression(stream, parent);

                // Get the operation for the token
                UnaryOperation? operation = GetUnaryOperation(op);

                return new UnaryExpressionNode(right, operation ?? UnaryOperation.Noop, null, parent);
            }
            catch (ParserException exception)
            {
                return new ErrorNode(
                    exception.Line,
                    exception.StartColumn,
                    exception.EndColumn,
                    exception.File,
                    exception.Message
                );
            }
        }

        private IExpressionNode ParsePrimaryExpression(TokenStream stream, INode? parent)
        {
            // Check if literal or identifier
            var token = stream.Peek();
            if (token.Family == TokenFamily.Literal)
            {
                token = stream.Consume(TokenFamily.Literal, TokenFamily.Keyword);
                LiteralType type = LiteralType.Unknown;
                switch (token.Type)
                {
                    case TokenType.Integer:
                        type = LiteralType.Integer;
                        break;
                    case TokenType.Float:
                        type = LiteralType.Float;
                        break;
                    case TokenType.String:
                        type = LiteralType.String;
                        break;
                    case TokenType.Boolean:
                        type = LiteralType.Boolean;
                        break;
                }

                return new LiteralNode(token.Value, type, parent);
            }
            else if (token.Family == TokenFamily.Identifier)
            {
                var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                return new IdentifierNode(identifier.Value);
            }
            else if(token.Type == TokenType.BracketLeft)
            {
                // Could be an array literal
                var array = new ArrayLiteralNode(parent);
                stream.Consume(TokenType.BracketLeft, TokenFamily.Operator);

                while(stream.Peek().Type != TokenType.BracketRight)
                {
                    var expression = Parse(stream, parent);
                    array.Values.Add((IExpressionNode)expression);

                    if(stream.Peek().Type == TokenType.Comma)
                    {
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);
                    }
                }

                stream.Consume(TokenType.BracketRight, TokenFamily.Keyword);

                return array;
            }
            else
            {
                throw new ParserException(
                    ParserExceptionCode.UnexpectedToken,
                    token.Line,
                    token.ColumnStart,
                    token.ColumnEnd,
                    token.File
                );
            }
        }

        private INode ParseFuncCall(TokenStream stream, INode? parent)
        {
            var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
            var identifierNode = new IdentifierNode(identifier.Value);
            var funcCall = new FuncCallNode(identifierNode, parent);

            stream.Consume(TokenType.ParenLeft, TokenFamily.Operator);

            while(stream.Peek().Type != TokenType.ParenRight)
            {
                // Parse the name of the argument([name]: value)
                var argName = stream.Consume(TokenType.Identifier, TokenFamily.Keyword).Value;
                stream.Consume(TokenType.Colon, TokenFamily.Operator);
                var expression = Parse(stream, parent);
                funcCall.Args.Add(new FuncCallArg { Name = argName, Value = (IExpressionNode)expression });

                if(stream.Peek().Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenFamily.Operator);
                }
            }

            stream.Consume(TokenType.ParenRight, TokenFamily.Keyword);

            return funcCall;
        }

        private BinaryOperation? GetBinaryOperation(Token token)
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

        private ComparisonOperation? GetComparisonOperation(Token token)
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

        private UnaryOperation? GetUnaryOperation(Token token)
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

        private bool IsBinaryExpression(TokenStream stream)
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

        private bool IsBinaryOperator(Token token)
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

        private bool IsComparisonExpression(TokenStream stream)
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

        private bool IsComparisonOperator(Token token)
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

        private bool IsFunctionCall(TokenStream stream)
        {
            var tokens = stream.Peek(3);

            if (
                tokens[0].Family == TokenFamily.Identifier &&
                tokens[1].Type == TokenType.ParenLeft
            )
            {
                return true;
            }

            return false;
        }

        private bool IsUnaryExpression(TokenStream stream)
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

        private bool IsUnaryOperator(Token token)
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
