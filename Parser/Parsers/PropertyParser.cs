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
                else if (stream.Peek().Type == TokenType.Let)
                {
                    stream.Consume(TokenType.Let, TokenFamily.Keyword);
                }
                else
                {
                    throw new ParserException(
                        ParserExceptionCode.UnexpectedToken,
                        stream.Peek().Line,
                        stream.Peek().ColumnStart,
                        stream.Peek().ColumnEnd,
                        stream.Peek().File
                    );
                }

                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                var property = new PropertyNode(name.Value, accessLevel, isMutable);

                // Check if the property has a type
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);
                    // Consume the type identifier
                    var typeToken = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
                    AST.Types.Type type = new(typeToken.Value);

                    property.TypeNode = type;
                }

                // TODO: Parse the property body

                return property;
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
    }
}
