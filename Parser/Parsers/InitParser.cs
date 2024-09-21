using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using System;

namespace Parser.Parsers
{
    public class InitParser
    {
        private StatementParser? statementParser;
        private readonly AccessLevelParser accessLevelParser;
        private readonly BlockParser blockParser;
        private readonly TypeParser typeParser;

        internal InitParser(
            AccessLevelParser accessLevelParser,
            BlockParser blockParser,
            TypeParser typeParser
        )
        {
            this.accessLevelParser = accessLevelParser;
            this.blockParser = blockParser;
            this.typeParser = typeParser;
        }

        internal void Setup(StatementParser statementParser)
        {
            this.statementParser = statementParser;
        }

        internal bool IsInit(Lexer.Tokens.TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (tokens[0].Type is TokenType.Init)
            {
                return true;
            }

            if (accessLevelParser.IsAccessLevel(tokens[0]) && tokens[1].Type is TokenType.Init)
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

            // Funcs have an access level
            var accessLevel = accessLevelParser.Parse(stream);

            // Consume the init keyword
            var token = stream.Consume(TokenType.Init, TokenFamily.Keyword);

            var init = new InitNode(accessLevel, parent);
            Utils.SetStart(init, token);

            // Inits may omit the parentheses if they have no parameters
            if (stream.Peek().Type == TokenType.ParenLeft)
            {
                // Parse the init parameters
                stream.Consume(TokenType.ParenLeft, TokenFamily.Operator);

                while (stream.Peek().Type != TokenType.ParenRight)
                {
                    // Name of the parameter
                    var paramName = stream.Consume(TokenType.Identifier, TokenType.ParenRight).Value;

                    // Consume the colon
                    stream.Consume(TokenType.Colon, TokenType.ParenRight);

                    // Parse the type of the parameter
                    var paramType = typeParser.Parse(stream, init);

                    // Add the parameter to the function
                    init.Parameters.Add(new Parameter { Name = paramName, Type = paramType });

                    // If the next token is a comma, consume it
                    if (stream.Peek().Type == TokenType.Comma)
                    {
                        stream.Consume(TokenType.Comma, TokenType.ParenRight);
                    }
                }

                token = stream.Consume(TokenType.ParenRight, TokenFamily.Operator);
            }

            Utils.SetEnd(init, token);

            token = stream.Peek();

            if (token.Type != TokenType.CurlyLeft)
            {
                return init;
            }
            
            init.Body = (BlockNode?)blockParser.Parse(stream, init);

            return init;
        }
    }
}
