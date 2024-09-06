﻿using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using System.Linq.Expressions;

namespace Parser.Parsers
{
    public class ExpressionParser : IParser
    {
        internal ExpressionParser()
        {
        }

        public INode Parse(TokenStream stream)
        {
            if ((this as IParser).IsBinaryExpression(stream))
            {
                return ParseBinaryExpression(stream);
            }
            else if ((this as IParser).IsUnaryExpression(stream))
            {
                return ParseUnaryExpression(stream);
            }
            else if((this as IParser).IsComparisonExpression(stream))
            {
                return ParseComparisonExpression(stream);
            }
            else
            {
                // Get the last token
                var token = stream.Peek();

                // Throw an exception that the token was not expected
                throw new ParserException(
                    ParserExceptionCode.UnexpectedToken,
                    token.Line,
                    token.ColumnStart,
                    token.ColumnEnd,
                    token.File
                );
            }
        }

        private INode ParseBinaryExpression(TokenStream stream)
        {
            try
            {
                var left = ParsePrimaryExpression(stream);
                var op = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);
                var right = ParsePrimaryExpression(stream);

                // Get the operation for the token
                BinaryOperation? operation = (this as IParser).GetBinaryOperation(op);

                return new BinaryExpressionNode(left, right, operation ?? BinaryOperation.Noop);
            }
            catch (ParserException exception)
            {
                return new ErrorNode(
                    exception.Line,
                    exception.StartColumn,
                    exception.EndColumn,
                    exception.File,
                    exception.Message
                );
            }
        }

        private INode ParseComparisonExpression(TokenStream stream)
        {
            try
            {
                var left = ParsePrimaryExpression(stream);
                var op = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);
                var right = ParsePrimaryExpression(stream);

                // Get the operation for the token
                ComparisonOperation? operation = (this as IParser).GetComparisonOperation(op);

                return new ComparisonExpressionNode(left, right, operation ?? ComparisonOperation.Noop);
            }
            catch (ParserException exception)
            {
                return new ErrorNode(
                    exception.Line,
                    exception.StartColumn,
                    exception.EndColumn,
                    exception.File,
                    exception.Message
                );
            }
        }

        private INode ParseUnaryExpression(TokenStream stream)
        {
            try
            {
                var op = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);
                var right = ParsePrimaryExpression(stream);

                // Get the operation for the token
                UnaryOperation? operation = (this as IParser).GetUnaryOperation(op);

                return new UnaryExpressionNode(right, operation ?? UnaryOperation.Noop, null, null);
            }
            catch (ParserException exception)
            {
                return new ErrorNode(
                    exception.Line,
                    exception.StartColumn,
                    exception.EndColumn,
                    exception.File,
                    exception.Message
                );
            }
        }

        private IExpressionNode ParsePrimaryExpression(TokenStream stream)
        {
            // Check if literal or identifier
            var token = stream.Peek();
            if (token.Family == TokenFamily.Literal)
            {
                token = stream.Consume(TokenFamily.Literal, TokenFamily.Keyword);
                LiteralType type = LiteralType.Unknown;
                switch (token.Type)
                {
                    case TokenType.Integer:
                        type = LiteralType.Integer;
                        break;
                    case TokenType.Float:
                        type = LiteralType.Float;
                        break;
                    case TokenType.String:
                        type = LiteralType.String;
                        break;
                    case TokenType.Boolean:
                        type = LiteralType.Boolean;
                        break;
                }

                return new LiteralNode(token.Value, type);
            }
            else if (token.Family == TokenFamily.Identifier)
            {
                var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                return new IdentifierNode(identifier.Value);
            }
            else
            {
                throw new ParserException(
                    ParserExceptionCode.UnexpectedToken,
                    token.Line,
                    token.ColumnStart,
                    token.ColumnEnd,
                    token.File
                );
            }
        }
    }
}
