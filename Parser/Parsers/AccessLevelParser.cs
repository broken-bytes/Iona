using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal class AccessLevelParser
    {
        internal bool IsAccessLevel(Token token)
        {
            return token.Type is TokenType.Public or TokenType.Private or TokenType.Internal;
        }

        public AccessLevel Parse(TokenStream stream)
        {
            // Check if the contract has an access modifier
            AccessLevel accessLevel = AccessLevel.Internal;
            var token = stream.Peek();

            while (token.Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                token = stream.Peek();
            }

            if (IsAccessLevel(stream.First()))
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
    }
}
