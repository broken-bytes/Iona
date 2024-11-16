using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal class PropertyParser
    {
        private readonly AccessLevelParser accessLevelParser;
        private readonly ExpressionParser expressionParser;
        private readonly TypeParser typeParser;
        private StatementParser? statementParser;

        internal PropertyParser(
            AccessLevelParser accessLevelParser,
            ExpressionParser expressionParser,
            TypeParser typeParser
        )
        {
            this.accessLevelParser = accessLevelParser;
            this.expressionParser = expressionParser;
            this.typeParser = typeParser;
        }

        internal void Setup(StatementParser statementParser)
        {
            this.statementParser = statementParser;
        }

        internal bool IsProperty(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (tokens[0].Type is TokenType.Var or TokenType.Let)
            {
                return true;
            }

            if (accessLevelParser.IsAccessLevel(tokens[0]) && tokens[1].Type is TokenType.Var or TokenType.Let)
            {
                return true;
            }

            return false;
        }

        public INode Parse(TokenStream stream, INode? parent)
        {
            if (statementParser == null)
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            PropertyNode? property = null;

            try
            {
                AccessLevel accessLevel = accessLevelParser.Parse(stream);
                bool isMutable = false;

                var decl = stream.Peek();

                // Consume the property keyword, can be either var or let
                if (decl.Type == TokenType.Var)
                {
                    stream.Consume(TokenType.Var, TokenFamily.Keyword);
                    isMutable = true;
                }
                else
                {
                    stream.Consume(TokenType.Let, TokenFamily.Keyword);
                }

                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                property = new PropertyNode(name.Value, accessLevel, isMutable, null, null, parent);
                Utils.SetStart(property, decl);
                Utils.SetEnd(property, name);

                // Check if the property has a type
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Keyword);
                    // Consume the type identifier
                    var typeToken = stream.Peek();
                    var type = typeParser.Parse(stream, property);
                    property.TypeNode = type;
                    type.Parent = property;

                    Utils.SetEnd(property, typeToken);
                }

                // Parse the property value(if any)
                if (stream.Peek().Type == TokenType.Assign)
                {
                    var assignToken = stream.Consume(TokenType.Assign, TokenFamily.Operator);
                    property.Value = (IExpressionNode?)expressionParser.Parse(stream, property);

                    if(property.Value != null)
                    {
                        property.Value.Parent = property;
                        Utils.SetStart(property.Value, assignToken);
                    }
                }

                return property;
            }
            catch (TokenStreamWrongTypeException exception)
            {
                return new ErrorNode(
                    exception.ErrorToken.Value,
                    parent
                );

                // TODO: Proper error metadata
            }
        }
    }
}
