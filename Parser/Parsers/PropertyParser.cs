using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal class PropertyParser : IParser
    {
        ExpressionParser expressionParser;
        TypeParser typeParser;

        internal PropertyParser(
            ExpressionParser expressionParser,
            TypeParser typeParser
        )
        {
            this.expressionParser = expressionParser;
            this.typeParser = typeParser;
        }

        public INode Parse(TokenStream stream, INode? parent)
        {
            PropertyNode? property = null;

            try
            {
                AccessLevel accessLevel = (this as IParser).ParseAccessLevel(stream);
                bool isMutable = false;

                // Consume the property keyword, can be either var or let
                if (stream.Peek().Type == TokenType.Var)
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

                // Check if the property has a type
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Keyword);
                    // Consume the type identifier
                    var type = typeParser.Parse(stream);
                    property.TypeNode = type;
                }

                // Parse the property value(if any)
                if (stream.Peek().Type == TokenType.Assign)
                {
                    stream.Consume(TokenType.Assign, TokenFamily.Operator);
                    property.Value = (IExpressionNode?)expressionParser.Parse(stream, property);
                }

                return property;
            }
            catch (TokenStreamWrongTypeException exception)
            {
                return new ErrorNode(
                    exception.ErrorToken.Line,
                    exception.ErrorToken.ColumnStart,
                    exception.ErrorToken.ColumnEnd,
                    exception.ErrorToken.File,
                    exception.ErrorToken.Value,
                    parent
                );
            }
        }
    }
}
