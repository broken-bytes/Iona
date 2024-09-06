using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class ClassParser : IParser
    {
        private readonly FuncParser funcParser;
        private readonly PropertyParser propertyParser;
        private readonly TypeParser typeParser;

        internal ClassParser(FuncParser funcParser, PropertyParser propertyParser, TypeParser typeParser)
        {
            this.funcParser = funcParser;
            this.propertyParser = propertyParser;
            this.typeParser = typeParser;
        }

        public INode Parse(TokenStream stream)
        {
            ClassNode? classNode = null;

            try
            {
                AccessLevel accessLevel = (this as IParser).ParseAccessLevel(stream);

                // Consume the contract keyword
                stream.Consume(TokenType.Class, TokenFamily.Keyword);

                // Consume the contract name
                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                classNode = new ClassNode(name.Value, accessLevel);
                classNode.GenericArguments = (this as IParser).ParseGenericArgs(stream);
                classNode.Body = new BlockNode(classNode);

                // Check if the struct fulfills a contract
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);

                    var contract = typeParser.Parse(stream);
                    classNode.Contracts.Add(contract);

                    while (stream.Peek().Type != TokenType.CurlyLeft)
                    {
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);

                        contract = typeParser.Parse(stream);
                        classNode.Contracts.Add(contract);
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
                            classNode.Body.AddChild(funcParser.Parse(stream));
                            break;
                        case TokenType.Var or TokenType.Let:
                            classNode.Body.AddChild(propertyParser.Parse(stream));
                            break;
                        default:
                            classNode.Body.AddChild(
                                new ErrorNode(
                                    token.Line,
                                    token.ColumnStart,
                                    token.ColumnEnd,
                                    token.File,
                                    $"Unexpected token {token.Value}",
                                    classNode.Body
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
                if (classNode == null)
                {
                    classNode = new ClassNode("Error", AccessLevel.Internal);
                }

                if (classNode.Body == null)
                {
                    classNode.Body = new BlockNode(classNode);
                }

                classNode.Body.AddChild(new ErrorNode(
                    exception.Line,
                    exception.StartColumn,
                    exception.EndColumn,
                    exception.File,
                    exception.Message
                ));
            }

            return classNode;
        }
    }
}
