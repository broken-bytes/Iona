using AST.Nodes;
using Lexer.Tokens;
using Parser.Parsers;
using System.IO;

namespace Parser.Parsers
{
    internal class ScopeResolutionParser
    {
        private ExpressionParser? expressionParser;
        private FuncCallParser? funcCallParser;
        private StatementParser? statementParser;

        internal ScopeResolutionParser()
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

        internal bool IsScopeResolution(TokenStream stream)
        {
            if (stream.Count() < 2)
            {
                return false;
            }

            var tokens = stream.Peek(2);

            if ((tokens[0].Type is TokenType.Identifier or TokenType.Self) && tokens[1].Type == TokenType.Scope)
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

            if (!IsScopeResolution(stream))
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
            } 
            else
            {
                target = new IdentifierNode(token.Value);
                Utils.SetMeta(target, token);
            }

            var scope = stream.Consume(TokenType.Scope, TokenFamily.Operator);

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

            var scopeResolution = new ScopeResolutionNode((IdentifierNode)target, member, parent);
            Utils.SetMeta(scopeResolution, token);

            target.Parent = scopeResolution;
            member.Parent = scopeResolution;

            Utils.SetColumnEnd(scopeResolution, member.Meta.ColumnEnd);
            Utils.SetLineEnd(scopeResolution, member.Meta.LineEnd);

            if (stream.IsEmpty())
            {
                return scopeResolution;
            }

            token = stream.Peek();

            while (token.Type == TokenType.Scope)
            {
                stream.Consume(TokenType.Scope, TokenFamily.Keyword);

                IExpressionNode nextMember;
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

                scopeResolution.Property = new PropAccessNode(scopeResolution.Scope, nextMember, parent);
                scopeResolution.Scope.Parent = scopeResolution;
                scopeResolution.Property.Parent = scopeResolution;
                Utils.SetColumnEnd(scopeResolution, nextMember.Meta.ColumnEnd);
                Utils.SetLineEnd(scopeResolution, nextMember.Meta.LineEnd);
            }

            return scopeResolution;
        }

        public Token PeekTokenAfterMemberAccess(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            var lastToken = tokens[1];

            while (lastToken.Type is TokenType.Scope or TokenType.Identifier)
            {
                lastToken = tokens[tokens.Count - 1];
                tokens = stream.Peek(tokens.Count + 1);
            }

            return lastToken;
        }
    }
}
