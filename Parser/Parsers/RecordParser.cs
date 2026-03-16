using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class RecordParser
    {
        private readonly AccessLevelParser accessLevelParser;
        private readonly GenericArgsParser genericArgsParser;
        private readonly TypeParser typeParser;
        private StatementParser? statementParser;

        internal RecordParser(
            AccessLevelParser accessLevelParser,
            GenericArgsParser genericArgsParser,
            TypeParser typeParser
        )
        {
            this.accessLevelParser = accessLevelParser;
            this.genericArgsParser = genericArgsParser;
            this.typeParser = typeParser;
        }

        internal void Setup(StatementParser statementParser)
        {
            this.statementParser = statementParser;
        }

        internal bool IsRecord(Lexer.Tokens.TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (tokens[0].Type is TokenType.Record)
            {
                return true;
            }

            if (accessLevelParser.IsAccessLevel(tokens[0]) && tokens[1].Type is TokenType.Record)
            {
                return true;
            }

            return false;
        }

        public INode Parse(TokenStream stream, INode? parent)
        {
            if (statementParser == null)
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            RecordNode? recordNode = null;

            try
            {
                AccessLevel accessLevel = accessLevelParser.Parse(stream);

                var token = stream.Consume(TokenType.Record, TokenFamily.Keyword);

                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                recordNode = new RecordNode(name.Value, accessLevel, parent);

                recordNode.FullyQualifiedName = Utils.ResolveFullyQualifiedName(recordNode);

                Utils.SetStart(recordNode, token);
                Utils.SetEnd(recordNode, name);
                recordNode.GenericArguments = genericArgsParser.Parse(stream, recordNode);

                if(recordNode.GenericArguments.Count > 0)
                {
                    Utils.SetColumnEnd(recordNode, recordNode.GenericArguments[recordNode.GenericArguments.Count - 1].Meta.ColumnEnd);
                }

                // Check if the record fulfills a contract
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);

                    var contract = typeParser.Parse(stream, recordNode);
                    recordNode.Contracts.Add(contract);

                    while (stream.Peek().Type != TokenType.CurlyLeft)
                    {
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);

                        contract = typeParser.Parse(stream, recordNode);
                        recordNode.Contracts.Add(contract);
                    }
                }

                // Consume the opening brace
                token = stream.Consume(TokenType.CurlyLeft, TokenFamily.Keyword);
                recordNode.Body = new BlockNode(recordNode);
                Utils.SetStart(recordNode.Body, token);

                token = stream.Peek();

                while (token.Type == TokenType.Linebreak)
                {
                    stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                    token = stream.Peek();
                }

                while (token.Type != TokenType.CurlyRight)
                {
                    recordNode.Body.AddChild(statementParser.Parse(stream, recordNode.Body));
                    token = stream.Peek();

                    while (token.Type == TokenType.Linebreak)
                    {
                        stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                        token = stream.Peek();
                    }
                }

                // Consume the closing brace
                token = stream.Consume(TokenType.CurlyRight, TokenFamily.Keyword);
                Utils.SetEnd(recordNode.Body, token);
            }
            catch (TokenStreamWrongTypeException exception)
            {
                if (recordNode == null)
                {
                    recordNode = new RecordNode("Error", AccessLevel.Internal);
                }

                if (recordNode.Body == null)
                {
                    recordNode.Body = new BlockNode(recordNode);
                }

                throw new ParserException(
                   ParserExceptionCode.Unknown,
                   exception.ErrorToken.Line,
                   exception.ErrorToken.ColumnStart,
                   exception.ErrorToken.ColumnEnd,
                   exception.ErrorToken.File
               );
            }

            return recordNode;
        }
    }
}
