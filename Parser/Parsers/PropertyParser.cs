using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal class PropertyParser : IParser
    {
        ExpressionParser expressionParser;

        internal PropertyParser(ExpressionParser expressionParser)
        {
            this.expressionParser = expressionParser;
        }

        public INode Parse(TokenStream stream)
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

                property = new PropertyNode(name.Value, accessLevel, isMutable);

                // Check if the property has a type
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Keyword);
                    // Consume the type identifier
                    var typeToken = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
                    AST.Types.Type type = new(typeToken.Value);

                    property.TypeNode = type;
                }

                // TODO: Parse the property body

                return property;
            }
            catch (TokenStreamWrongTypeException exception)
            {
                return new ErrorNode(
                    exception.ErrorToken.Line,
                    exception.ErrorToken.ColumnStart,
                    exception.ErrorToken.ColumnEnd,
                    exception.ErrorToken.File,
                    exception.ErrorToken.Value
                );
            }
        }
    }
}
