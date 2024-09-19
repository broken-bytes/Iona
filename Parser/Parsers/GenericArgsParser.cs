using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal class GenericArgsParser
    {
        public List<GenericArgument> Parse(TokenStream stream, INode? parent)
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
                var arg = new GenericArgument(token.Value, parent);
                Utils.SetMeta(arg, token);

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
