using AST.Nodes;
using Lexer.Tokens;
using Parser.Parsers.Parser.Parsers;
using System.IO;

namespace Parser.Parsers
{
    internal class MemberAccessParser
    {
        private ExpressionParser? expressionParser;
        private FuncCallParser? funcCallParser;
        private StatementParser? statementParser;

        internal MemberAccessParser()
        {
        }

        internal void Setup(
            ExpressionParser expressionParser,
            FuncCallParser funcCallParser,
            StatementParser statementParser
        )
        {
            this.expressionParser = expressionParser;
            this.funcCallParser = funcCallParser;
            this.statementParser = statementParser;
        }

        internal bool IsMemberAccess(TokenStream stream)
        {
            if (stream.Count() < 2)
            {
                return false;
            }

            var tokens = stream.Peek(2);

            if ((tokens[0].Type is TokenType.Identifier or TokenType.Self) && tokens[1].Type == TokenType.Dot)
            {
                return true;
            }

            return false;
        }

        public IExpressionNode Parse(TokenStream stream, INode? parent) {
            if (expressionParser == null || statementParser == null || funcCallParser == null)
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

            INode target;

            if (token.Type == TokenType.Self)
            {
                target = new SelfNode();
                Utils.SetMeta(target, token);
            } else
            {
                target = new IdentifierNode(token.Value);
                Utils.SetMeta(target, token);
            }

            var dot = stream.Consume(TokenType.Dot, TokenFamily.Operator);

            INode member;
            if (funcCallParser.IsFuncCall(stream))
            {
                var funcCall = funcCallParser.Parse(stream, parent);
                member = funcCall;
            }
            else
            {
                var memberIdentifier = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
                member = new IdentifierNode(memberIdentifier.Value, parent);
                Utils.SetMeta(member, memberIdentifier);
            }

            var propAccess = new PropAccessNode(target, member, parent);
            Utils.SetMeta(propAccess, token);

            target.Parent = propAccess;
            member.Parent = propAccess;

            Utils.SetColumnEnd(propAccess, member.Meta.ColumnEnd);
            Utils.SetLineEnd(propAccess, member.Meta.LineEnd);

            if (stream.IsEmpty())
            {
                return propAccess;
            }

            token = stream.Peek();

            while (token.Type == TokenType.Dot)
            {
                stream.Consume(TokenType.Dot, TokenFamily.Keyword);

                INode nextMember;
                if (funcCallParser.IsFuncCall(stream))
                {
                    var funcCall = funcCallParser.Parse(stream, parent);
                    nextMember = funcCall;
                }
                else
                {
                    var memberIdentifier = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
                    nextMember = new IdentifierNode(memberIdentifier.Value, null);
                    Utils.SetMeta(nextMember, memberIdentifier);
                }

                propAccess.Property = new PropAccessNode(propAccess.Object, nextMember, parent);
                propAccess.Object.Parent = propAccess;
                propAccess.Property.Parent = propAccess;
                Utils.SetColumnEnd(propAccess, nextMember.Meta.ColumnEnd);
                Utils.SetLineEnd(propAccess, nextMember.Meta.LineEnd);
            }

            return propAccess;
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
