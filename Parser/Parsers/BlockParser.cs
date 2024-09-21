using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class BlockParser
    {
        private ExpressionParser? expressionParser;
        private MemberAccessParser? memberAccessParser;
        private StatementParser? statementParser;

        internal BlockParser()
        {
        }

        internal void Setup(
            ExpressionParser expressionParser,
            MemberAccessParser memberAccessParser,
            StatementParser statementParser
        )
        {
            this.expressionParser = expressionParser;
            this.memberAccessParser = memberAccessParser;
            this.statementParser = statementParser;
        }

        public INode Parse(TokenStream stream, INode? parent)
        {
            if (expressionParser == null || memberAccessParser == null || statementParser == null)
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            BlockNode? block = null;

            try
            {
                var token = stream.Peek();
                block = new BlockNode(parent);
                Utils.SetStart(block, token);

                // Consume the opening brace
                stream.Consume(TokenType.CurlyLeft, TokenFamily.Keyword);

                while (token.Type == TokenType.Linebreak)
                {
                    stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                    token = stream.Peek();
                }

                while (token.Type != TokenType.CurlyRight)
                {
                    if (statementParser.IsStatement(stream))
                    {
                        block.AddChild(statementParser.Parse(stream, block));
                    }
                    else if (expressionParser.IsExpression(stream))
                    {
                        block.AddChild(expressionParser.Parse(stream, block));
                    }
                    else if (memberAccessParser.IsMemberAccess(stream))
                    {
                        block.AddChild(memberAccessParser.Parse(stream, block));
                    }
                    else if (token.Type == TokenType.Linebreak)
                    {
                        stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                    }
                    token = stream.Peek();
                }

                token = stream.Consume(TokenType.CurlyRight, TokenFamily.Keyword);
                Utils.SetEnd(block, token);
            }
            catch (TokenStreamException exception)
            {
                if (block == null)
                {
                    block = new BlockNode(parent);
                }

                block.AddChild(new ErrorNode(
                    exception.ErrorToken.Value
                ));

                // TODO: Proper error metadata
            }

            return block;
        }
    }
}
