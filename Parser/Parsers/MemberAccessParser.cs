using AST.Nodes;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal class MemberAccessParser
    {
        private ExpressionParser? expressionParser;

        internal MemberAccessParser()
        {
        }

        internal void Setup(ExpressionParser expressionParser)
        {
            this.expressionParser = expressionParser;
        }

        internal bool IsMemberAccess(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (tokens[0].Type == TokenType.Identifier && tokens[1].Type == TokenType.Dot)
            {
                return true;
            }

            return false;
        }

        public INode Parse(TokenStream stream, INode? parent) {
            if (expressionParser == null)
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            if (!IsMemberAccess(stream))
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            var token = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

            stream.Consume(TokenType.Dot, TokenFamily.Operator);

            var target = new IdentifierNode(token.Value, null);
            var member = expressionParser.Parse(stream, null);

            var memberAccess = new MemberAccessNode(target, member, parent);

            token = stream.Peek();

            while (token.Type == TokenType.Dot)
            {
                stream.Consume(TokenType.Dot, TokenFamily.Keyword);
                var nextMember = expressionParser.Parse(stream, null);

                memberAccess.Target = new MemberAccessNode(memberAccess.Target, nextMember, parent);
            }

            return memberAccess;
        }
    }
}
