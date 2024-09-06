using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class FuncParser : IParser
    {
        ExpressionParser expressionParser;
        VariableParser variableParser;
        TypeParser typeParser;

        internal FuncParser(
            ExpressionParser expressionParser,
            VariableParser variableParser, 
            TypeParser typeParser
        )
        {
            this.expressionParser = expressionParser;
            this.variableParser = variableParser;
            this.typeParser = typeParser;
        }

        public INode Parse(Lexer.Tokens.TokenStream stream)
        {
            bool isMutating = false;

            // Funcs have an access level
            var accessLevel = (this as IParser).ParseAccessLevel(stream);

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

            var func = new FuncNode(name.Value, accessLevel, isMutating, isStatic);

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

            if(stream.Peek().Type != TokenType.CurlyLeft)
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

            while(token.Type != TokenType.CurlyRight)
            {
                // Funcs may have variables
                if (token.Type is TokenType.Var or TokenType.Let)
                {
                    func.Body.AddChild(variableParser.Parse(stream));
                } 
                else if(token.Type is TokenType.Return)
                {
                    stream.Consume(TokenType.Return, TokenFamily.Keyword);
                    // Parse the return value
                    var expression = (IExpressionNode)expressionParser.Parse(stream);
                    var returnNode = new ReturnNode(expression, func);

                    func.Body.AddChild(returnNode);
                }
                else
                {
                    stream.Consume();
                }

                // TODO: Parse if statements, while loops, etc.

                token = stream.Peek();
            }

            stream.Consume(TokenType.CurlyRight, TokenFamily.Keyword);

            return func;
        }
    }
}
