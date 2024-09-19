using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class FuncParser
    {
        private readonly AccessLevelParser accessLevelParser;
        private readonly TypeParser typeParser;
        private ExpressionParser? expressionParser;
        private StatementParser? statementParser;

        internal FuncParser(
            AccessLevelParser accessLevelParser,
            TypeParser typeParser
        )
        {
            this.accessLevelParser = accessLevelParser;
            this.typeParser = typeParser;
        }

        internal void Setup(
            ExpressionParser expressionParser,
            StatementParser statementParser
        )
        {
            this.expressionParser = expressionParser;
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

        public INode Parse(TokenStream stream, INode? parent)
        {
            if (expressionParser == null || statementParser == null)
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            FuncNode? func = null;

            try
            {

                bool isMutating = false;

                // Funcs have an access level
                var accessLevel = accessLevelParser.Parse(stream);

                // Funcs may be static or instance
                var isStatic = false;

                if (stream.Peek().Type == TokenType.Static)
                {
                    stream.Consume(TokenType.Static, TokenFamily.Keyword);
                    isStatic = true;
                }

                // Funcs can be mutating or non-mutating
                var mut = stream.Peek();

                if (mut.Type == TokenType.Mutating)
                {
                    stream.Consume(TokenType.Mutating, TokenFamily.Keyword);
                    isMutating = true;
                }

                // Consume the func keyword
                var token = stream.Consume(TokenType.Fn, TokenFamily.Keyword);

                // Consume the function name
                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                func = new FuncNode(name.Value, accessLevel, isMutating, isStatic, parent);
                Utils.SetStart(func, isMutating ? mut : token);

                // Parse the function parameters
                stream.Consume(TokenType.ParenLeft, TokenFamily.Operator);

                while (stream.Peek().Type != TokenType.ParenRight)
                {
                    // Name of the parameter
                    var paramName = stream.Consume(TokenType.Identifier, TokenType.ParenRight).Value;

                    // Consume the colon
                    stream.Consume(TokenType.Colon, TokenType.ParenRight);

                    // Parse the type of the parameter
                    var paramType = typeParser.Parse(stream, func);

                    // Add the parameter to the function
                    func.Parameters.Add(new Parameter { Name = paramName, Type = paramType });

                    // If the next token is a comma, consume it
                    if (stream.Peek().Type == TokenType.Comma)
                    {
                        stream.Consume(TokenType.Comma, TokenType.ParenRight);
                    }
                }

                token = stream.Consume(TokenType.ParenRight, TokenFamily.Operator);
                Utils.SetEnd(func, token);

                // Check if the function has a return type
                if (stream.Peek().Type == TokenType.Arrow)
                {
                    stream.Consume(TokenType.Arrow, TokenType.CurlyLeft);
                    func.ReturnType = typeParser.Parse(stream, func);
                    func.ReturnType.Parent = func;
                    Utils.SetColumnEnd(func, func.ReturnType.Meta.ColumnEnd);
                }

                token = stream.Peek();

                if (token.Type != TokenType.CurlyLeft)
                {
                    return func;
                }

                func.Body = new BlockNode(func);
                Utils.SetStart(func.Body, token);

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
                    if (statementParser.IsStatement(stream))
                    {
                        func.Body.AddChild(statementParser.Parse(stream, func.Body));
                    } 
                    else if(expressionParser.IsExpression(stream))
                    {
                        func.Body.AddChild(expressionParser.Parse(stream, func.Body));
                    } 
                    else
                    {
                        var error = stream.Consume(token.Type, TokenType.Linebreak);
                        var errorNode = new ErrorNode(
                            $"Unexpected token {token.Value} expected start of expression or statement"
                        );
                        // TODO: Proper error metadata

                        func.Body.AddChild(errorNode);
                    }

                    token = stream.Peek();

                    while (token.Type == TokenType.Linebreak)
                    {
                        stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                        token = stream.Peek();
                    }
                }

                token = stream.Consume(TokenType.CurlyRight, TokenFamily.Keyword);
                Utils.SetEnd(func.Body, token);
            } 
            catch (TokenStreamException exception)
            {
                if (func == null)
                {
                    func = new FuncNode("Error", AccessLevel.Internal, false, false, parent);
                }

                if (func.Body == null)
                {
                    func.Body = new BlockNode(func);
                }

                func.Body.AddChild(new ErrorNode(
                    exception.ErrorToken.Value
                ));

                // TODO: Proper error metadata
            }

            return func;
        }
    }
}
