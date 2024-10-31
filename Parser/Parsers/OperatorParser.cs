using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class OperatorParser
    {
        private readonly AccessLevelParser accessLevelParser;
        private readonly BlockParser blockParser;
        private readonly TypeParser typeParser;
        private ExpressionParser? expressionParser;
        private StatementParser? statementParser;

        internal OperatorParser(
            AccessLevelParser accessLevelParser,
            BlockParser blockParser,
            TypeParser typeParser
        )
        {
            this.accessLevelParser = accessLevelParser;
            this.blockParser = blockParser;
            this.typeParser = typeParser;
        }

        internal void Setup(
            ExpressionParser expressionParser,
            StatementParser statementParser
        )
        {
            this.expressionParser = expressionParser;
            this.statementParser = statementParser;
        }

        internal bool IsOperator(Lexer.Tokens.TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (tokens[0].Type is TokenType.Op)
            {
                return true;
            }

            if (accessLevelParser.IsAccessLevel(tokens[0]) && tokens[1].Type is TokenType.Op)
            {
                return true;
            }

            return false;
        }

        public INode Parse(TokenStream stream, INode? parent)
        {
            if (expressionParser == null || statementParser == null)
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            OperatorNode? opNode = null;

            try
            {

                // Funcs have an access level
                var accessLevel = accessLevelParser.Parse(stream);

                // Funcs may be static or instance
                var isStatic = false;

                if (stream.Peek().Type == TokenType.Static)
                {
                    stream.Consume(TokenType.Static, TokenFamily.Keyword);
                    isStatic = true;
                }

                // Consume the func keyword
                var token = stream.Consume(TokenType.Op, TokenFamily.Keyword);

                // Consume the operator
                var op = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);

                // Get the operator type
                var operatorType = GetOperatorType(op);

                opNode = new OperatorNode(operatorType, accessLevel, isStatic, parent);
                Utils.SetStart(opNode, token);

                // Parse the function parameters
                stream.Consume(TokenType.ParenLeft, TokenFamily.Operator);

                while (stream.Peek().Type != TokenType.ParenRight)
                {
                    // Name of the parameter
                    var paramName = stream.Consume(TokenType.Identifier, TokenType.ParenRight).Value;

                    // Consume the colon
                    stream.Consume(TokenType.Colon, TokenType.ParenRight);

                    // Parse the type of the parameter
                    var paramType = typeParser.Parse(stream, opNode);

                    // Add the parameter to the function
                    opNode.Parameters.Add(new ParameterNode(paramName, paramType, opNode));

                    // If the next token is a comma, consume it
                    if (stream.Peek().Type == TokenType.Comma)
                    {
                        stream.Consume(TokenType.Comma, TokenType.ParenRight);
                    }
                }

                token = stream.Consume(TokenType.ParenRight, TokenFamily.Operator);
                Utils.SetEnd(opNode, token);

                stream.Consume(TokenType.Arrow, TokenType.CurlyLeft);
                opNode.ReturnType = typeParser.Parse(stream, opNode);
                opNode.ReturnType.Parent = opNode;
                Utils.SetColumnEnd(opNode, opNode.ReturnType.Meta.ColumnEnd);


                token = stream.Peek();

                if (token.Type != TokenType.CurlyLeft)
                {
                    return opNode;
                }

                opNode.Body = (BlockNode?)blockParser.Parse(stream, opNode);
            }
            catch (TokenStreamException exception)
            {
                if (opNode == null)
                {
                    opNode = new OperatorNode(OperatorType.Noop, AccessLevel.Internal, false, parent);
                }

                if (opNode.Body == null)
                {
                    opNode.Body = new BlockNode(opNode);
                }

                opNode.Body.AddChild(new ErrorNode(
                    exception.ErrorToken.Value,
                    opNode,
                    opNode
                ));

                // TODO: Proper error metadata
            }

            return opNode;
        }

        private OperatorType GetOperatorType(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Plus:
                    return OperatorType.Add;
                case TokenType.Minus:
                    return OperatorType.Subtract;
                case TokenType.Multiply:
                    return OperatorType.Multiply;
                case TokenType.Divide:
                    return OperatorType.Divide;
                case TokenType.Modulo:
                    return OperatorType.Modulo;
                case TokenType.Equal:
                    return OperatorType.Equal;
                case TokenType.NotEqual:
                    return OperatorType.NotEqual;
                case TokenType.Greater:
                    return OperatorType.GreaterThan;
                case TokenType.GreaterEqual:
                    return OperatorType.GreaterThanOrEqual;
                case TokenType.Less:
                    return OperatorType.LessThan;
                case TokenType.LessEqual:
                    return OperatorType.LessThanOrEqual;
                case TokenType.And:
                    return OperatorType.And;
                case TokenType.Or:
                    return OperatorType.Or;
                case TokenType.Not:
                    return OperatorType.Not;
                case TokenType.PlusAssign:
                    return OperatorType.AddAssign;
                case TokenType.MinusAssign:
                    return OperatorType.SubtractAssign;
                case TokenType.MultiplyAssign:
                    return OperatorType.MultiplyAssign;
                case TokenType.DivideAssign:
                    return OperatorType.DivideAssign;

                default:
                    return OperatorType.Noop;
            }
        }
    }
}
