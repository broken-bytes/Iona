using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using System.Net.Security;

namespace Parser.Parsers
{
    public class StructParser : IParser
    {
        private readonly FuncParser funcParser;
        private readonly PropertyParser propertyParser;
        private readonly TypeParser typeParser;

        internal StructParser(FuncParser funcParser, PropertyParser propertyParser, TypeParser typeParser)
        {
            this.funcParser = funcParser;
            this.propertyParser = propertyParser;
            this.typeParser = typeParser;
        }

        public INode Parse(TokenStream stream)
        {
            StructNode? structNode = null;

            try
            {
                AccessLevel accessLevel = (this as IParser).ParseAccessLevel(stream);

                // Consume the contract keyword
                stream.Consume(TokenType.Struct, TokenFamily.Keyword);

                // Consume the contract name
                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                structNode = new StructNode(name.Value, accessLevel);
                structNode.GenericArguments = (this as IParser).ParseGenericArgs(stream);
                structNode.Body = new BlockNode(structNode);

                // Check if the struct fulfills a contract
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);

                    var contract = typeParser.Parse(stream);
                    structNode.Contracts.Add(contract);

                    while (stream.Peek().Type != TokenType.CurlyLeft)
                    {
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);

                        contract = typeParser.Parse(stream);
                        structNode.Contracts.Add(contract);
                    }
                }

                // Consume the opening brace
                stream.Consume(TokenType.CurlyLeft, TokenFamily.Keyword);

                var token = stream.Peek();

                while (token.Type == TokenType.Linebreak)
                {
                    stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                    token = stream.Peek();
                }

                // Parse the contract body
                while (token.Type != TokenType.CurlyRight)
                {
                    // Contracts may have props or funcs
                    switch (token.Type)
                    {
                        case TokenType.Fn or TokenType.Mutating:
                            structNode.Body.AddChild(funcParser.Parse(stream));
                            break;
                        case TokenType.Var or TokenType.Let:
                            structNode.Body.AddChild(propertyParser.Parse(stream));
                            break;
                        default:
                            structNode.Body.AddChild(
                                new ErrorNode(
                                    token.Line,
                                    token.ColumnStart,
                                    token.ColumnEnd,
                                    token.File,
                                    $"Unexpected token {token.Value}",
                                    structNode.Body
                                )
                            );
                            stream.Consume();
                            break;
                    }

                    token = stream.Peek();

                    while (token.Type == TokenType.Linebreak)
                    {
                        stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                        token = stream.Peek();
                    }
                }

                // Consume the closing brace
                stream.Consume(TokenType.CurlyRight, TokenFamily.Keyword);
            }
            catch (ParserException exception)
            {
                if (structNode == null)
                {
                    structNode = new StructNode("Error", AccessLevel.Internal);
                }

                if (structNode.Body == null)
                {
                    structNode.Body = new BlockNode(structNode);
                }

                structNode.Body.AddChild(new ErrorNode(
                    exception.Line,
                    exception.StartColumn,
                    exception.EndColumn,
                    exception.File,
                    exception.Message
                ));
            }

            return structNode;
        }
    }
}
