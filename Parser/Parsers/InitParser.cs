using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class InitParser : IParser
    {
        VariableParser variableParser;
        TypeParser typeParser;

        internal InitParser(VariableParser variableParser, TypeParser typeParser)
        {
            this.variableParser = variableParser;
            this.typeParser = typeParser;
        }

        public INode Parse(Lexer.Tokens.TokenStream stream)
        {
            // Funcs have an access level
            var accessLevel = (this as IParser).ParseAccessLevel(stream);

            // Funcs can be mutating or non-mutating
            var token = stream.Peek();

            // Consume the init keyword
            stream.Consume(TokenType.Init, TokenFamily.Keyword);

            var init = new InitNode(accessLevel);

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
                    var paramType = typeParser.Parse(stream);

                    // Add the parameter to the function
                    init.Parameters.Add(new Parameter { Name = paramName, Type = paramType });

                    // If the next token is a comma, consume it
                    if (stream.Peek().Type == TokenType.Comma)
                    {
                        stream.Consume(TokenType.Comma, TokenType.ParenRight);
                    }
                }

                stream.Consume(TokenType.ParenRight, TokenFamily.Operator);
            }

            if (stream.Peek().Type != TokenType.CurlyLeft)
            {
                return init;
            }

            init.Body = new BlockNode(init);

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
                // Funcs may have variables
                if (token.Type is TokenType.Var or TokenType.Let)
                {
                    init.Body.AddChild(variableParser.Parse(stream));
                }
                else
                {
                    stream.Consume();
                }

                // TODO: Parse if statements, while loops, etc.

                token = stream.Peek();
            }

            stream.Consume(TokenType.CurlyRight, TokenFamily.Keyword);

            return init;
        }
    }
}
