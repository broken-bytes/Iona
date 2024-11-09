using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal class FuncCallParser
    {
        private ExpressionParser? expressionParser;

        internal FuncCallParser()
        {
        }

        internal void Setup(ExpressionParser expressionParser)
        {
            this.expressionParser = expressionParser;
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

            return false;
        }

        internal bool IsMemberAccess(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (tokens[0].Type == TokenType.Identifier && tokens[1].Type == TokenType.Dot)
            {
                return true;
            }

            return false;
        }

        public IExpressionNode Parse(TokenStream stream, INode? parent)
        {
            if(expressionParser == null)
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
            var identifierNode = new IdentifierNode(identifier.Value);
            Utils.SetMeta(identifierNode, identifier);
            var funcCall = new FuncCallNode(identifierNode, parent);
            Utils.SetStart(funcCall, identifier);

            stream.Consume(TokenType.ParenLeft, TokenFamily.Operator);

            var token = stream.Peek();
            while (token.Type != TokenType.ParenRight && stream.Any())
            {
                // Parse the name of the argument([name]: value)
                var argName = stream.Consume(TokenType.Identifier, TokenFamily.Keyword).Value;
                stream.Consume(TokenType.Colon, TokenFamily.Operator);
                // The expression parser should not be concerned with handling edge cases `,`, `)` of the function call.
                var expressionStream = GetParameterExpression(stream);
                // Remove either the comma, or last `)` from the stream
                var expression = expressionParser.Parse(expressionStream, parent);
                funcCall.Args.Add(new FuncCallArg { Name = argName, Value = (IExpressionNode)expression });

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
            var expression = stream.Copy();

            // (..) is one subexpression. We use it to check if we are still in a param expression or hit the enclosing `)`
            var subExpressions = 0;

            var token = stream.Peek();
            
            while (stream.Any())
            {
                if (token.Type is TokenType.Comma || (token.Type is TokenType.ParenRight && subExpressions == 0))
                {
                    break;
                }
                
                if (token.Type == TokenType.ParenLeft)
                {
                    subExpressions++;
                }
                
                expression.Append(token);

                stream.Consume();
                
                token = stream.Peek();
            }

            return expression;
        }
    }
}
