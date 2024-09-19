using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using Parser.Parsers.Parser.Parsers;

namespace Parser.Parsers
{
    public class ExpressionParser
    {
        FuncCallParser funcCallParser;
        MemberAccessParser memberAccessParser;
        TypeParser typeParser;

        internal ExpressionParser(
            FuncCallParser funcCallParser,
            MemberAccessParser memberAccessParser,
            TypeParser typeParser
        )
        {
            this.funcCallParser = funcCallParser;
            this.memberAccessParser = memberAccessParser;
            this.typeParser = typeParser;
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
            else if (IsComparisonExpression(stream))
            {
                return ParseComparisonExpression(stream, parent);
            }
            else if (IsFunctionCall(stream))
            {
                return funcCallParser.Parse(stream, parent);
            }
            else if (IsObjectLiteral(stream))
            {
                return ParseObjectLiteral(stream, parent);
            }
            else if(memberAccessParser.IsMemberAccess(stream))
            {
                return memberAccessParser.Parse(stream, parent);
            }

            return ParsePrimaryExpression(stream, parent);
        }

        public bool IsExpression(TokenStream stream)
        {
            if (
                IsBinaryExpression(stream) ||
                IsUnaryExpression(stream) ||
                IsComparisonExpression(stream) ||
                IsFunctionCall(stream) ||
                IsObjectLiteral(stream) ||
                IsMemberAccess(stream)
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
                var error = new ErrorNode(
                    exception.Message
                );

                // TODO: Proper error metadata

                return error;
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
                    exception.Message
                );

                // TODO: Proper error metadata
            }
        }

        private INode ParseObjectLiteral(TokenStream stream, INode? parent)
        {
            if (!IsObjectLiteral(stream))
            {
                var errorToken = stream.Peek();

                return new ErrorNode(
                    "Invalid object literal",
                    parent
                );

                // TODO: Proper error metadata
            }


            var typeName = stream.Peek();
            // Get the type of the object
            var objectType = typeParser.Parse(stream, null);

            // Parse the object literal
            var token = stream.Consume(TokenType.CurlyLeft, TokenFamily.Keyword);

            var objectLiteral = new ObjectLiteralNode(objectType, parent);
            Utils.SetStart(objectLiteral, typeName);

            objectType.Parent = objectLiteral;

            // Parse the arguments
            while (token.Type != TokenType.CurlyRight)
            {
                var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Identifier).Value;
                stream.Consume(TokenType.Colon, TokenFamily.Operator);
                var expression = Parse(stream, objectLiteral);

                // Add the argument to the object literal
                var arg = new ObjectLiteralNode.Argument { Name = identifier, Value = (IExpressionNode)expression };
                objectLiteral.Arguments.Add(arg);

                token = stream.Peek();

                if (token.Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenFamily.Operator);
                }
            }

            token = stream.Consume(TokenType.CurlyRight, TokenFamily.Keyword);
            Utils.SetEnd(objectLiteral, token);

            return objectLiteral;
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
                    exception.Message
                );

                // TODO: Proper error metadata
            }
        }

        private INode ParsePrimaryExpression(TokenStream stream, INode? parent)
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

                var literal = new LiteralNode(token.Value, type, parent);
                Utils.SetMeta(literal, token);

                return literal;
            }
            else if (token.Family == TokenFamily.Identifier)
            {
                var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                var identifierNode = new IdentifierNode(identifier.Value);
                Utils.SetMeta(identifierNode, identifier);

                return identifierNode;
            }
            else if (token.Type == TokenType.BracketLeft)
            {
                // Could be an array literal
                var array = new ArrayLiteralNode(parent);
                stream.Consume(TokenType.BracketLeft, TokenFamily.Operator);
                Utils.SetStart(array, token);

                while (stream.Peek().Type != TokenType.BracketRight)
                {
                    var expression = Parse(stream, parent);
                    array.Values.Add((IExpressionNode)expression);

                    if (stream.Peek().Type == TokenType.Comma)
                    {
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);
                    }
                }

                token = stream.Consume(TokenType.BracketRight, TokenFamily.Keyword);
                Utils.SetEnd(array, token);

                return array;
            }
            else
            {
                return new ErrorNode(
                    "Unexpected token in expression",
                    parent
                );

                // TODO: Proper error metadata
            }
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
            return funcCallParser.IsFuncCall(stream);
        }

        private bool IsMemberAccess(TokenStream stream)
        {
            return memberAccessParser.IsMemberAccess(stream);
        }

        private bool IsObjectLiteral(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (
                tokens[0].Type == TokenType.Identifier &&
                tokens[1].Type == TokenType.CurlyLeft
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
