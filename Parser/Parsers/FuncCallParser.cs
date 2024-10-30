using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.Parsers
{
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
                while (token.Type != TokenType.ParenRight)
                {
                    // Parse the name of the argument([name]: value)
                    var argName = stream.Consume(TokenType.Identifier, TokenFamily.Keyword).Value;
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);
                    var expression = expressionParser.Parse(stream, parent);
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
        }
    }

}
