using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using Shared;
using System.Net.Http.Headers;
using System.Text;

namespace Parser.Parsers
{
    internal class TypeParser
    {
        internal TypeParser()
        {
        }

        public TypeReferenceNode? Parse(TokenStream stream, INode? parent)
        {
            // The type may be an array type([T])
            var token = stream.Peek();

            var endToken = token;

            var nameBuilder = new StringBuilder();

            if (token.Type == TokenType.BracketLeft)
            {
                token = stream.Consume(TokenType.BracketLeft, TokenFamily.Keyword);

                // Parse the type of the array
                var arrayType = Parse(stream, parent);

                var end = stream.Consume(TokenType.BracketRight, TokenFamily.Keyword);

                if (arrayType == null)
                {
                    return null;
                }

                var arrayRef = new TypeReferenceNode("Array");
                arrayRef.GenericArguments.Add(arrayType);

                Utils.SetStart(arrayRef, token);
                Utils.SetEnd(arrayRef, end);

                return arrayRef;
            }

            // We need to be able to parse types and generics
            var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

            nameBuilder.Append(identifier.Value);

            // A type may be qualified by modules (IO.File) and not just the type name
            while (stream.Peek().Type == TokenType.Dot)
            {
                // Consume the dot token
                stream.Consume(TokenType.Dot, TokenFamily.Operator);

                // Consume the identifier
                var nextNamePart = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                nameBuilder.Append(".");
                nameBuilder.Append(nextNamePart.Value);
                endToken = nextNamePart;
            }

            // Check if the type is a generic
            if (stream.Peek().Type == TokenType.Less)
            {
                // Consume the less than token
                stream.Consume(TokenType.Less, TokenFamily.Operator);

                var genericType = new TypeReferenceNode(token.Value);
                Utils.SetStart(genericType, token);

                while (stream.Peek().Type != TokenType.Greater)
                {
                    // Parse the generic argument
                    ITypeReferenceNode? genericArg = Parse(stream, parent);

                    if (genericArg == null)
                    {
                        EmitTypeNotFoundError(genericType.Name, genericType.Meta);
                        return null;
                    }

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

                token = stream.Consume(TokenType.Greater, TokenFamily.Operator);
                Utils.SetEnd(genericType, token);

                return genericType;
            }

            var type = new TypeReferenceNode(nameBuilder.ToString(), parent);

            Utils.SetStart(type, token);
            Utils.SetEnd(type, endToken);

            return type;
        }

        private void EmitTypeNotFoundError(string typeName, Metadata meta)
        {
            CompilerErrorFactory.TopLevelDefinitionError(
                typeName,
                meta
            );
        }
    }
}
