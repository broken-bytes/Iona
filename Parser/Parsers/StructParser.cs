using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using System.Net.Security;

namespace Parser.Parsers
{
    public class StructParser : IParser
    {
        private readonly FuncParser funcParser;
        private readonly InitParser initParser;
        private readonly PropertyParser propertyParser;
        private readonly TypeParser typeParser;

        internal StructParser(
            FuncParser funcParser, 
            InitParser initParser,
            PropertyParser propertyParser, 
            TypeParser typeParser
        )
        {
            this.funcParser = funcParser;
            this.initParser = initParser;
            this.propertyParser = propertyParser;
            this.typeParser = typeParser;
        }

        public INode Parse(TokenStream stream, INode? parent)
        {
            StructNode? structNode = null;

            try
            {
                AccessLevel accessLevel = (this as IParser).ParseAccessLevel(stream);

                // Consume the contract keyword
                stream.Consume(TokenType.Struct, TokenFamily.Keyword);

                // Consume the contract name
                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                structNode = new StructNode(name.Value, accessLevel, parent);
                structNode.GenericArguments = (this as IParser).ParseGenericArgs(stream);

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
                structNode.Body = new BlockNode(structNode);

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
                            structNode.Body.AddChild(funcParser.Parse(stream, structNode.Body));
                            break;
                        case TokenType.Var or TokenType.Let:
                            structNode.Body.AddChild(propertyParser.Parse(stream, structNode.Body));
                            break;
                        case TokenType.Init:
                            structNode.Body.AddChild(initParser.Parse(stream, structNode.Body));
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
            catch (TokenStreamWrongTypeException exception)
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
                    exception.ErrorToken.Line,
                    exception.ErrorToken.ColumnStart,
                    exception.ErrorToken.ColumnEnd,
                    exception.ErrorToken.File,
                    exception.ErrorToken.Value
                ));
            }

            return structNode;
        }
    }
}
