using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using Parser.Parsers;
using System.IO;

namespace Parser
{
    public interface IParser
    {
        public INode Parse(TokenStream stream, INode? parent);

        public AccessLevel ParseAccessLevel(TokenStream stream)
        {
            // Check if the contract has an access modifier
            AccessLevel accessLevel = AccessLevel.Internal;
            var token = stream.Peek();

            while (token.Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                token = stream.Peek();
            }

            if (token.Type == TokenType.Public || token.Type == TokenType.Private || token.Type == TokenType.Internal)
            {
                switch (token.Type)
                {
                    case TokenType.Public:
                        accessLevel = AccessLevel.Public;
                        break;
                    case TokenType.Private:
                        accessLevel = AccessLevel.Private;
                        break;
                    case TokenType.Internal:
                        accessLevel = AccessLevel.Internal;
                        break;
                }

                // Consume the access modifier
                stream.Consume(token.Type, TokenFamily.Keyword);
            }

            return accessLevel;
        }

        public List<GenericArgument> ParseGenericArgs(TokenStream stream)
        {
            var args = new List<GenericArgument>();

            if (stream.Peek().Type != TokenType.Less)
            {
                return args;
            }

            stream.Consume(TokenType.Less, TokenFamily.Operator);

            while (stream.Peek().Type != TokenType.Greater)
            {
                var token = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
                var arg = new GenericArgument { Name = token.Value };

                // Check if the generic argument has constraints
                // TODO: Implement constraints

                args.Add(arg);

                if (stream.Peek().Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenFamily.Operator);
                }
            }

            stream.Consume(TokenType.Greater, TokenFamily.Operator);

            return args;
        }
    }
}
