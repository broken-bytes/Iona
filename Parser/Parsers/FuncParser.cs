using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class FuncParser
    {
        private readonly AccessLevelParser accessLevelParser;
        private readonly TypeParser typeParser;
        private StatementParser? statementParser;

        internal FuncParser(
            AccessLevelParser accessLevelParser,
            TypeParser typeParser
        )
        {
            this.accessLevelParser = accessLevelParser;
            this.typeParser = typeParser;
        }

        internal void Setup(StatementParser statementParser)
        {
            this.statementParser = statementParser;
        }

        internal bool IsFunc(Lexer.Tokens.TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (tokens[0].Type is TokenType.Fn or TokenType.Mutating)
            {
                return true;
            }

            if (accessLevelParser.IsAccessLevel(tokens[0]) && tokens[1].Type is TokenType.Fn or TokenType.Mutating)
            {
                return true;
            }

            return false;
        }

        public INode Parse(Lexer.Tokens.TokenStream stream, INode? parent)
        {
            if (statementParser == null)
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            bool isMutating = false;

            // Funcs have an access level
            var accessLevel = accessLevelParser.Parse(stream);

            // Funcs may be static or instance
            var isStatic = false;

            if (stream.Peek().Type == TokenType.Static)
            {
                stream.Consume(Lexer.Tokens.TokenType.Static, Lexer.Tokens.TokenFamily.Keyword);
                isStatic = true;
            }

            // Funcs can be mutating or non-mutating
            var token = stream.Peek();

            if (token.Type == TokenType.Mutating)
            {
                stream.Consume(Lexer.Tokens.TokenType.Mutating, Lexer.Tokens.TokenFamily.Keyword);
                isMutating = true;
            }

            // Consume the func keyword
            stream.Consume(TokenType.Fn, TokenFamily.Keyword);

            // Consume the function name
            var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

            var func = new FuncNode(name.Value, accessLevel, isMutating, isStatic, parent);

            // Parse the function parameters
            stream.Consume(TokenType.ParenLeft, TokenFamily.Operator);

            while (stream.Peek().Type != TokenType.ParenRight)
            {
                // Name of the parameter
                var paramName = stream.Consume(TokenType.Identifier, TokenType.ParenRight).Value;

                // Consume the colon
                stream.Consume(TokenType.Colon, TokenType.ParenRight);

                // Parse the type of the parameter
                var paramType = typeParser.Parse(stream);

                // Add the parameter to the function
                func.Parameters.Add(new Parameter { Name = paramName, Type = paramType });

                // If the next token is a comma, consume it
                if (stream.Peek().Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenType.ParenRight);
                }
            }

            stream.Consume(TokenType.ParenRight, TokenFamily.Operator);

            // Check if the function has a return type
            if (stream.Peek().Type == TokenType.Arrow)
            {
                stream.Consume(TokenType.Arrow, TokenType.CurlyLeft);
                func.ReturnType = typeParser.Parse(stream);
            }

            if (stream.Peek().Type != TokenType.CurlyLeft)
            {
                return func;
            }

            func.Body = new BlockNode(func);

            // Consume the opening brace
            stream.Consume(TokenType.CurlyLeft, TokenFamily.Keyword);

            token = stream.Peek();

            while (token.Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                token = stream.Peek();
            }

            while (token.Type != TokenType.CurlyRight)
            {
                func.Body.AddChild(statementParser.Parse(stream, func.Body));

                token = stream.Peek();
            }

            stream.Consume(TokenType.CurlyRight, TokenFamily.Keyword);

            return func;
        }
    }
}
