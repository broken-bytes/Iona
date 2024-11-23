using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal class GenericArgsParser
    {
        public List<GenericParameter> Parse(TokenStream stream, INode? parent)
        {
            var args = new List<GenericParameter>();

            if (stream.Peek().Type != TokenType.ArrowLeft)
            {
                return args;
            }

            stream.Consume(TokenType.ArrowLeft, TokenFamily.Operator);

            while (stream.Peek().Type != TokenType.ArrowRight)
            {
                var token = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
                var arg = new GenericParameter(token.Value, parent);
                Utils.SetMeta(arg, token);

                // Check if the generic argument has constraints
                // TODO: Implement constraints

                args.Add(arg);

                if (stream.Peek().Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenFamily.Operator);
                }
            }

            stream.Consume(TokenType.ArrowRight, TokenFamily.Operator);

            return args;
        }
    }
}
