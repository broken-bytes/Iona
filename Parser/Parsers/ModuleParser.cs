using AST.Nodes;
using Lexer.Tokens;


namespace Parser.Parsers
{
    public class ModuleParser
    {
        private StatementParser? statementParser;

        internal ModuleParser()
        {

        }

        internal void Setup(StatementParser statementParser)
        {
            this.statementParser = statementParser;
        }

        internal bool IsModule(TokenStream stream)
        {
            if (stream.First().Type is TokenType.Module)
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

            // Peek so we have a valid token to begin with
            ModuleNode? module = null;

            try
            {
                stream.Consume(TokenType.Module, TokenFamily.Keyword);

                module = new ModuleNode("", parent);

                var token = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                module.Name = token.Value;

                token = stream.Peek();
                // Parse until we reach the end of the module declaration (whitespace or linebreak)
                while (token.Type == TokenType.Dot)
                {
                    stream.Consume(TokenType.Dot, TokenFamily.Keyword);
                    token = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
                    module.Name += token.Value;
                    token = stream.Peek();
                }

                token = stream.Peek();

                while (token.Type == TokenType.Linebreak)
                {
                    stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                    token = stream.Peek();
                }

                // Parse classes, contracts, structs, etc.
                while (token.Type != TokenType.EndOfFile)
                {
                    module.AddChild(statementParser.Parse(stream, module));

                    token = stream.Peek();

                    while (token.Type == TokenType.Linebreak)
                    {
                        stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                        token = stream.Peek();
                    }
                }
            }
            catch (TokenStreamException exception)
            {
                var error = new ErrorNode(
                    exception.ErrorToken.Line,
                    exception.ErrorToken.ColumnStart,
                    exception.ErrorToken.ColumnEnd,
                    exception.ErrorToken.File,
                    exception.Message
                );

                if (module != null)
                {
                    module.Children.Add(error);

                    return module;
                }

                return error;
            }

            return module;
        }
    }
}
