using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using Shared;

namespace Parser.Parsers
{
    public class FuncParser
    {
        private readonly AccessLevelParser accessLevelParser;
        private readonly BlockParser blockParser;
        private readonly TypeParser typeParser;
        private readonly IErrorCollector errorCollector;
        private ExpressionParser? expressionParser;
        private StatementParser? statementParser;

        internal FuncParser(
            AccessLevelParser accessLevelParser,
            BlockParser blockParser,
            TypeParser typeParser,
            IErrorCollector errorCollector
        )
        {
            this.accessLevelParser = accessLevelParser;
            this.blockParser = blockParser;
            this.typeParser = typeParser;
            this.errorCollector = errorCollector;
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

                    if (paramType != null)
                    {
                        // Add the parameter to the function
                        func.Parameters.Add(new ParameterNode(paramName, paramType, parent));
                    }

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

                func.Body = (BlockNode?)blockParser.Parse(stream, func);
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

                throw new ParserException(
                    ParserExceptionCode.Unknown, 
                    exception.ErrorToken.Line, 
                    exception.ErrorToken.ColumnStart, 
                    exception.ErrorToken.ColumnEnd,
                    exception.ErrorToken.File
                );
            }

            return func;
        }
    }
}
