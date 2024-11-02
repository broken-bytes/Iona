using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using Parser.Parsers.Parser.Parsers;
using Shared;
using System.Xml.Linq;

namespace Parser.Parsers
{
    public class ExpressionParser
    {
        FuncCallParser funcCallParser;
        MemberAccessParser memberAccessParser;
        TypeParser typeParser;
        private readonly IErrorCollector errorCollector;

        internal ExpressionParser(
            FuncCallParser funcCallParser,
            MemberAccessParser memberAccessParser,
            TypeParser typeParser,
            IErrorCollector errorCollector
        )
        {
            this.funcCallParser = funcCallParser;
            this.memberAccessParser = memberAccessParser;
            this.typeParser = typeParser;
            this.errorCollector = errorCollector;
        }

        public IExpressionNode Parse(TokenStream stream, INode? parent)
        {
            return ParseExpression(stream, parent);
        }

        public bool IsExpression(TokenStream stream)
        {
            // Expressions start with an identifier, operator, paren or a literal
            var token = stream.Peek();

            if (
                token.Family is TokenFamily.Literal ||
                token.Family is TokenFamily.Identifier ||
                token.Type is TokenType.ParenLeft ||
                token.Family is TokenFamily.Operator
            )
            {
                return true;
            }

            return false;
        }

        // ------------------- Helper methods -------------------
        private IExpressionNode? ParseExpression(TokenStream stream, INode? parent)
        {
            var tokens = new List<Token>();
            var token = stream.Peek();

            tokens.Add(token);

            while (
                token.Type == TokenType.ParenLeft ||
                token.Type == TokenType.ParenRight ||
                token.Family == TokenFamily.Operator ||
                token.Family == TokenFamily.Literal ||
                token.Family == TokenFamily.Identifier ||
                token.Type == TokenType.BracketLeft ||
                token.Type == TokenType.BracketRight ||
                token.Type == TokenType.CurlyLeft ||
                token.Type == TokenType.CurlyRight ||
                token.Type == TokenType.Dot
            )
            {
                stream.Consume();
                var nextToken = stream.Peek();

                while (nextToken.Type == TokenType.Linebreak)
                {
                    stream.Consume();
                    nextToken = stream.Peek();
                }

                // If we encounter any operator that is not a binary operator, we stop parsing as we found the end of the expression
                if (
                    nextToken.Family is TokenFamily.Operator &&
                    (!IsBinaryOperator(nextToken) && nextToken.Type != TokenType.Dot)
                )
                {
                    break;
                }

                // If we encounter a keyword or curly bracket, we stop parsing as we found the end of the expression
                if (
                    nextToken.Family is TokenFamily.Keyword ||
                    nextToken.Type == TokenType.CurlyRight
                )
                {
                    break;
                }

                // If the token is unary it needs to be followed by an identifier
                if (IsUnaryOperator(token) && nextToken.Type is not TokenType.Identifier)
                {
                    var meta = new Metadata
                    {
                        ColumnEnd = nextToken.ColumnEnd,
                        ColumnStart = nextToken.ColumnStart,
                        LineStart = nextToken.Line,
                        LineEnd = nextToken.Line,
                        File = nextToken.File
                    };
                    errorCollector.Collect(CompilerErrorFactory.SyntaxError("Unary operations are only allowed on symbols", meta));

                    return null;
                }

                if (
                    IsBinaryOperator(token) && (
                        nextToken.Type != TokenType.Identifier &&
                        nextToken.Family is not TokenFamily.Literal
                    )
                )
                {
                    var meta = new Metadata
                    {
                        ColumnEnd = nextToken.ColumnEnd,
                        ColumnStart = nextToken.ColumnStart,
                        LineStart = nextToken.Line,
                        LineEnd = nextToken.Line,
                        File = nextToken.File
                    };
                    errorCollector.Collect(CompilerErrorFactory.SyntaxError("Binary operations are only allowed on symbols and literals", meta));

                    return null;
                }

                if (
                    token.Family is TokenFamily.Identifier ||
                    token.Family is TokenFamily.Literal
                )
                {
                    if (
                        nextToken.Type != TokenType.Dot &&
                        nextToken.Type != TokenType.ParenLeft &&
                        !IsBinaryOperator(nextToken)
                    )
                    {
                        var meta = new Metadata
                        {
                            ColumnEnd = nextToken.ColumnEnd,
                            ColumnStart = nextToken.ColumnStart,
                            LineStart = nextToken.Line,
                            LineEnd = nextToken.Line,
                            File = nextToken.File
                        };
                        errorCollector.Collect(CompilerErrorFactory.SyntaxError("Expected operator", meta));

                        return null;
                    }
                }

                // If we encounter a right parenthesis that isn't followed by a binary operator,
                // we stop parsing as we found the end of the expression
                if (token.Type is TokenType.ParenRight && !IsBinaryOperator(nextToken))
                {
                    break;
                }

                token = nextToken;

                tokens.Add(nextToken);
            }

            var tokenStream = new TokenStream(tokens);

            var postfix = InfixToPostfix(tokenStream);
            var expression = BuildBinaryExpressionNode(postfix, parent);

            return (IExpressionNode)expression;

            /*
            var left = ParsePrimaryExpression(stream, parent);
            var op = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);
            var right = ParsePrimaryExpression(stream, parent);

            // Get the operation for the token
            BinaryOperation? operation = GetBinaryOperation(op);

            if (left == null || right == null)
            {
                return null;
            }

            var expr = new BinaryExpressionNode(left, right, operation ?? BinaryOperation.Noop, null, parent);

            left.Parent = expr;
            right.Parent = expr;

            Utils.SetMeta(expr, left, right);

            return expr;
            */

        }

        private IExpressionNode? ParseObjectLiteral(TokenStream stream, INode? parent)
        {
            if (!IsObjectLiteral(stream))
            {
                var errorToken = stream.Peek();

                var meta = new Metadata
                {
                    ColumnEnd = errorToken.ColumnEnd,
                    ColumnStart = errorToken.ColumnStart,
                    LineStart = errorToken.Line,
                    LineEnd = errorToken.Line,
                    File = errorToken.File
                };


                errorCollector.Collect(CompilerErrorFactory.SyntaxError("Invalid object literal", meta));

                return null;
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

        private IExpressionNode ParseUnaryExpression(TokenStream stream, INode? parent)
        {
            var op = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);
            var right = ParsePrimaryExpression(stream, parent);

            // Get the operation for the token
            UnaryOperation? operation = GetUnaryOperation(op);

            return new UnaryExpressionNode(right, operation ?? UnaryOperation.Noop, null, parent);
        }

        private IExpressionNode? ParsePrimaryExpression(TokenStream stream, INode? parent)
        {
            // Check if literal or identifier
            var token = stream.Peek();

            if (IsMemberAccess(stream))
            {
                return memberAccessParser.Parse(stream, parent);
            }

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
                IExpressionNode identifierNode;

                if (identifier.Value == "self")
                {
                    identifierNode = new SelfNode(parent);
                }
                else
                {
                    identifierNode = new IdentifierNode(identifier.Value, parent);
                }

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
                var errorToken = stream.Peek();

                var meta = new Metadata
                {
                    ColumnEnd = errorToken.ColumnEnd,
                    ColumnStart = errorToken.ColumnStart,
                    LineStart = errorToken.Line,
                    LineEnd = errorToken.Line,
                    File = errorToken.File
                };

                var error = CompilerErrorFactory.SyntaxError("Unexpected token in expression", meta);

                errorCollector.Collect(error);

                return null;
            }
        }

        private BinaryOperation GetBinaryOperation(Token token)
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
                case TokenType.Equal:
                    return BinaryOperation.Equal;
                case TokenType.NotEqual:
                    return BinaryOperation.NotEqual;
                case TokenType.Greater:
                    return BinaryOperation.GreaterThan;
                case TokenType.Less:
                    return BinaryOperation.LessThan;
                case TokenType.GreaterEqual:
                    return BinaryOperation.GreaterThanOrEqual;
                case TokenType.LessEqual:
                    return BinaryOperation.LessThanOrEqual;
                default:
                    return BinaryOperation.Noop;
            }
        }

        private UnaryOperation GetUnaryOperation(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Increment:
                    return UnaryOperation.Increment;
                case TokenType.Decrement:
                    return UnaryOperation.Decrement;
                case TokenType.Not:
                    return UnaryOperation.Not;
                default:
                    return UnaryOperation.Noop;
            }
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

        private int Precedence(BinaryOperation op)
        {
            if (op == BinaryOperation.Add || op == BinaryOperation.Subtract)
            {
                return 1;
            }

            if (op == BinaryOperation.Multiply || op == BinaryOperation.Divide)
            {
                return 2;
            }

            return 0;
        }

        private TokenStream InfixToPostfix(TokenStream stream)
        {
            var output = new List<Token>();
            var stack = new Stack<Token>();

            while (!stream.IsEmpty())
            {
                var token = stream.Consume();
                if (token.Family == TokenFamily.Identifier || token.Family == TokenFamily.Literal)
                {
                    output.Add(token);
                }
                else if (token.Type == TokenType.ParenLeft)
                {
                    stack.Push(token);
                }
                else if (token.Type == TokenType.ParenRight)
                {
                    while (stack.Count > 0 && stack.Peek().Type != TokenType.ParenLeft)
                    {
                        output.Add(stack.Pop());
                    }
                    stack.Pop(); // Pop the '('
                }
                else // The token is an operator
                {
                    // We need to check if the operator is just a dot for property access
                    if (token.Type == TokenType.Dot)
                    {
                        output.Add(token);
                        continue;
                    }

                    BinaryOperation? op = null;

                    // Check if there is an operator on the stack
                    if (stack.Count > 0)
                    {
                        op = GetBinaryOperation(stack.Peek());
                    }

                    var otherOp = GetBinaryOperation(token);

                    if (op is BinaryOperation binary)
                    {
                        while (stack.Count > 0 && (Precedence(binary) >= Precedence(otherOp)))
                        {
                            output.Add(stack.Pop());
                        }
                    }
                    stack.Push(token);
                }
            }

            while (stack.Count > 0)
            {
                output.Add(stack.Pop());
            }

            return new TokenStream(output);
        }

        private INode BuildBinaryExpressionNode(TokenStream stream, INode? parent)
        {
            var stack = new Stack<INode>();
            var token = stream.Peek();
            while (!stream.IsEmpty())
            {
                if (token.Family == TokenFamily.Identifier)
                {
                    // We don't consume from the stream here, as the member access parser does that
                    if (IsMemberAccess(stream))
                    {
                        var memberAccess = memberAccessParser.Parse(stream, parent);
                        stack.Push(memberAccess);
                    }
                    else
                    {
                        var identifier = new IdentifierNode(token.Value, parent);

                        Utils.SetMeta(identifier, token);

                        stream.Consume();

                        stack.Push(identifier);
                    }
                }
                else if (token.Family == TokenFamily.Literal)
                {
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
                        case TokenType.NullLiteral:
                            type = LiteralType.Null;
                            break;
                    }
                    var literal = new LiteralNode(token.Value, type);

                    Utils.SetMeta(literal, token);

                    stream.Consume();

                    stack.Push(literal);
                }
                else // The token is an operator
                {
                    var right = stack.Pop();
                    var left = stack.Pop();

                    var operation = GetBinaryOperation(token);
                    var node = new BinaryExpressionNode(left, right, operation, null, parent);
                    left.Parent = node;
                    right.Parent = node;

                    Utils.SetMeta(node, left, right);

                    stack.Push(node);

                    stream.Consume();
                }

                if (stream.IsEmpty())
                {
                    break;
                }

                token = stream.Peek();
            }

            var upperMost = stack.Pop();
            upperMost.Parent = parent;

            return upperMost;
        }
    }
}
