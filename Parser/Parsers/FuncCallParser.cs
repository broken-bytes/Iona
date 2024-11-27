using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using Shared;

namespace Parser.Parsers
{
    internal class FuncCallParser
    {
        private ExpressionParser? _expressionParser;
        private readonly IErrorCollector _errorCollector;

        internal FuncCallParser(IErrorCollector errorCollector)
        {
            _errorCollector = errorCollector;
        }

        internal void Setup(ExpressionParser expressionParser)
        {
            this._expressionParser = expressionParser;
        }

        internal bool IsFuncCall(TokenStream stream)
        {
            if (stream.Count() < 3)
            {
                return false;
            }

            var tokens = stream.Peek(2);

            if (tokens[0].Type == TokenType.Identifier && tokens[1].Type == TokenType.ParenLeft)
            {
                return true;
            }
            
            if (tokens[0].Type == TokenType.Identifier && tokens[1].Type == TokenType.ArrowLeft)
            {
                return true;
            }

            return false;
        }

        public IExpressionNode Parse(TokenStream stream, INode? parent)
        {
            if(_expressionParser == null)
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
            var identifierNode = new IdentifierNode(identifier.Value);
            Utils.SetMeta(identifierNode, identifier);
            var funcCall = new FuncCallNode(identifierNode, parent);
            Utils.SetStart(funcCall, identifier);
            
            // Check if the function call includes generic arguments
            var hasGenerics = stream.Peek().Type == TokenType.ArrowLeft;

            if (hasGenerics)
            {
                var next = stream.Consume(TokenType.ArrowLeft, TokenType.ParenLeft);
                next = stream.Peek();

                while (next.Type is not TokenType.ArrowRight)
                {
                    var name = stream.Consume(TokenType.Identifier, TokenType.ParenLeft);
                    
                    funcCall.GenericArgs.Add(new GenericArgument(name.Value, funcCall));

                    next = stream.Peek();

                    if (stream.Peek().Type == TokenType.Comma)
                    {
                        stream.Consume(TokenType.Comma, TokenType.ParenLeft);
                        
                        next = stream.Peek();
                    }
                    else
                    {
                        // TODO: Error -> Unexpected token in generic arguments
                    }
                }
                
                stream.Consume(TokenType.ArrowRight, TokenType.ParenLeft);
            }

            stream.Consume(TokenType.ParenLeft, TokenFamily.Operator);

            var token = stream.Peek();
            while (token.Type != TokenType.ParenRight && stream.Any())
            {
                string argName = "";
                // Parse the name of the argument([name]: value)
                try
                {
                    argName = stream.Consume(TokenType.Identifier, TokenFamily.Keyword).Value;
                }
                catch
                {
                    var meta = new Metadata
                    {
                        ColumnStart = token.ColumnStart,
                        ColumnEnd = token.ColumnEnd,
                        LineStart = token.Line,
                        LineEnd = token.Line,
                        File = token.File,
                    };
                    var error = CompilerErrorFactory.MissingParameterName(meta);
                    
                    _errorCollector.Collect(error);

                    funcCall.Status = INode.ResolutionStatus.Failed;
                    
                    return funcCall;
                }

                try
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);
                }
                catch
                {
                    var meta = new Metadata
                    {
                        ColumnStart = token.ColumnStart,
                        ColumnEnd = token.ColumnEnd,
                        LineStart = token.Line,
                        LineEnd = token.Line,
                        File = token.File,
                    };
                    
                    var error = CompilerErrorFactory.MissingColonAfterParameterName(meta);
                    
                    _errorCollector.Collect(error);

                    funcCall.Status = INode.ResolutionStatus.Failed;
                    
                    return funcCall;
                }

                // The expression parser should not be concerned with handling edge cases `,`, `)` of the function call.
                var expressionStream = GetParameterExpression(stream);
                // Remove either the comma, or last `)` from the stream
                var expression = _expressionParser.Parse(expressionStream, funcCall);
                funcCall.Args.Add(new FuncCallArg(argName, (IExpressionNode)expression));

                token = stream.Peek();

                if (token.Type == TokenType.Comma)
                {
                    token = stream.Consume(TokenType.Comma, TokenFamily.Operator);
                }
            }

            token = stream.Consume(TokenType.ParenRight, TokenFamily.Keyword);
            Utils.SetEnd(funcCall, token);

            return funcCall;
        }
        
        private TokenStream GetParameterExpression(TokenStream stream)
        {
            int functionNesting = 0;
            var tokens = new List<Token>();
            
            // (..) is one subexpression. We use it to check if we are still in a param expression or hit the enclosing `)`
            var subExpressions = 0;

            var token = stream.Peek();

            while (stream.Any())
            {
                if (token.Type is TokenType.Comma)
                {
                    // When we are inside of another function call we do not want to break
                    if (functionNesting == 0)
                    {
                        break;
                    }
                }

                if (token.Type is TokenType.ParenRight)
                {
                    if (functionNesting > 0)
                    {
                        functionNesting--;
                    }
                    else
                    {
                        if (subExpressions == 0)
                        {
                            break;
                        }

                        subExpressions--;
                    }
                }

                if (token.Type == TokenType.ParenLeft)
                {
                    // Nested function call
                    if (tokens.Last().Type is TokenType.Identifier)
                    {
                        functionNesting++;
                    }
                    else
                    {
                        subExpressions++;
                    }
                }
                
                tokens.Add(token);

                stream.Consume();
                
                token = stream.Peek();
            }

            return new TokenStream(tokens);
        }
    }
}
