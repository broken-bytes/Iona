using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal class TypeParser
    {
        internal TypeParser()
        {
        }

        public IType Parse(TokenStream stream)
        {
            // The type may be an array type([T])
            var token = stream.Peek();

            if (token.Type == TokenType.BracketLeft)
            {
                stream.Consume(TokenType.BracketLeft, TokenFamily.Keyword);

                // Parse the type of the array
                IType arrayType = Parse(stream);

                stream.Consume(TokenType.BracketRight, TokenFamily.Keyword);

                return new ArrayType(arrayType);
            }

            // We need to be able to parse types and generics
            token = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

            // Check if the type is a generic
            if (stream.Peek().Type == TokenType.Less)
            {
                // Consume the less than token
                stream.Consume(TokenType.Less, TokenFamily.Operator);

                var genericType = new GenericType(token.Value);

                while (stream.Peek().Type != TokenType.Greater)
                {
                    // Parse the generic argument
                    IType genericArg = Parse(stream);

                    // Add the generic argument to the list of generic arguments
                    // of the generic type
                    genericType.GenericArguments.Add(genericArg);

                    // Check if there are more generic arguments
                    if (stream.Peek().Type == TokenType.Comma)
                    {
                        // Consume the comma token
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);
                    }
                }

                stream.Consume(TokenType.Greater, TokenFamily.Operator);

                return genericType;
            }

            return new AST.Types.Type(token.Value);
        }
    }
}
