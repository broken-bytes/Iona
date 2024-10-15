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
                throw new ParserException(
                    ParserExceptionCode.Unknown, 
                    error.Line, 
                    error.ColumnStart, 
                    error.ColumnEnd, 
                    error.File
                );
            }

            if (!IsMemberAccess(stream))
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            var token = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

            var dot = stream.Consume(TokenType.Dot, TokenFamily.Operator);

            var target = new IdentifierNode(token.Value);
            Utils.SetMeta(target, token);

            var memberIdentifier = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

            var member = new IdentifierNode(memberIdentifier.Value, null);
            Utils.SetMeta(member, memberIdentifier);

            var memberAccess = new MemberAccessNode(target, member, parent);
            Utils.SetMeta(memberAccess, token);

            target.Parent = memberAccess;
            member.Parent = memberAccess;

            Utils.SetEnd(memberAccess, memberIdentifier);

            token = stream.Peek();

            while (token.Type == TokenType.Dot)
            {
                stream.Consume(TokenType.Dot, TokenFamily.Keyword);

                var nextIdentifier = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
                var next = new IdentifierNode(nextIdentifier.Value, null);

                memberAccess.Left = new MemberAccessNode(memberAccess.Left, next, parent);
                memberAccess.Left.Parent = memberAccess;
                memberAccess.Right.Parent = memberAccess;
                Utils.SetEnd(memberAccess, nextIdentifier);
            }

            return memberAccess;
        }

        public Token PeekTokenAfterMemberAccess(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            var lastToken = tokens[1];

            while (lastToken.Type is TokenType.Dot or TokenType.Identifier)
            {
                lastToken = tokens[tokens.Count - 1];
                tokens = stream.Peek(tokens.Count + 1);
            }

            return lastToken;
        }

        private INode ReorderStatement(INode target, INode member)
        {
            // We need to reorganize the nodes here, the statement (assignment, addition, etc) should be the target
            if (member is AssignmentNode assignment)
            {
                assignment.Parent = target.Parent;
                assignment.Target = new MemberAccessNode(target, assignment.Target, assignment);

                assignment.Target.Parent = assignment;
                assignment.Value.Parent = assignment;
                
                return assignment;
            }

            // TODO: Add more nodes here
            return target;
        }
    }
}
