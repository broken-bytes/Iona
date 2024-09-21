using AST.Nodes;
using Lexer.Tokens;
using System.IO;

namespace Parser.Parsers
{
    internal class MemberAccessParser
    {
        private ExpressionParser? expressionParser;
        private StatementParser? statementParser;

        internal MemberAccessParser()
        {
        }

        internal void Setup(
            ExpressionParser expressionParser,
            StatementParser statementParser
        )
        {
            this.expressionParser = expressionParser;
            this.statementParser = statementParser;
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
            if (expressionParser == null || statementParser == null)
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

            var dot = stream.Consume(TokenType.Dot, TokenFamily.Operator);

            var target = new IdentifierNode(token.Value, null);
            Utils.SetMeta(target, token);

            if (statementParser.IsStatement(stream))
            {
                var statement = statementParser.Parse(stream, parent);

                return ReorderStatement(target, statement);
            }

            var member = expressionParser.Parse(stream, null);

            var memberAccess = new MemberAccessNode(target, member, parent);

            target.Parent = memberAccess;
            member.Parent = memberAccess;

            Utils.SetStart(memberAccess, token);
            Utils.SetEnd(memberAccess, dot);

            token = stream.Peek();

            while (token.Type == TokenType.Dot)
            {
                stream.Consume(TokenType.Dot, TokenFamily.Keyword);

                if (statementParser.IsStatement(stream))
                {
                    var statement = statementParser.Parse(stream, parent);

                    return ReorderStatement(target, statement);
                }

                var nextMember = expressionParser.Parse(stream, null);

                memberAccess.Target = new MemberAccessNode(memberAccess.Target, nextMember, parent);
            }

            return memberAccess;
        }

        private INode ReorderStatement(INode target, INode member)
        {
            // We need to reorganize the nodes here, the statement (assignment, addition, etc) should be the target
            if (member is AssignmentNode assignment)
            {
                assignment.Parent = target.Parent;
                assignment.Target = new MemberAccessNode(target, assignment.Target, assignment);
                
                return assignment;
            }

            // TODO: Add more nodes here
            return target;
        }
    }
}
