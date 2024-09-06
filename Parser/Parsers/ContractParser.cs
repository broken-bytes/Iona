using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using System.Net.Security;

namespace Parser.Parsers
{
    public class ContractParser : IParser
    {
        private readonly FuncParser funcParser;
        private readonly PropertyParser propertyParser;
        private readonly TypeParser typeParser;

        internal ContractParser(FuncParser funcParser, PropertyParser propertyParser, TypeParser typeParser)
        {
            this.funcParser = funcParser;
            this.propertyParser = propertyParser;
            this.typeParser = typeParser;
        }

        public INode Parse(TokenStream stream)
        {
            ContractNode? contract = null;

            try
            {
                AccessLevel accessLevel = (this as IParser).ParseAccessLevel(stream); 

                // Consume the contract keyword
                stream.Consume(TokenType.Contract, TokenFamily.Keyword);

                // Consume the contract name
                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                contract = new ContractNode(name.Value, accessLevel);
                contract.GenericArguments = (this as IParser).ParseGenericArgs(stream);
                contract.Body = new BlockNode(contract);

                // Check if the struct fulfills a contract
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);

                    var refinement = typeParser.Parse(stream);
                    contract.Refinements.Add(refinement);

                    while (stream.Peek().Type != TokenType.CurlyLeft)
                    {
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);

                        refinement = typeParser.Parse(stream);
                        contract.Refinements.Add(refinement);
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
                            contract.Body.AddChild(funcParser.Parse(stream));
                            break;
                        case TokenType.Var or TokenType.Let:
                            contract.Body.AddChild(propertyParser.Parse(stream));
                            break;
                        default:
                            contract.Body.AddChild(
                                new ErrorNode(
                                    token.Line,
                                    token.ColumnStart,
                                    token.ColumnEnd,
                                    token.File,
                                    $"Unexpected token {token.Value}",
                                    contract.Body
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
                if (contract == null)
                {
                    contract = new ContractNode("Error", AccessLevel.Internal);
                }

                if (contract.Body == null)
                {
                    contract.Body = new BlockNode(contract);
                }

                contract.Body.AddChild(new ErrorNode(
                    exception.ErrorToken.Line,
                    exception.ErrorToken.ColumnStart,
                    exception.ErrorToken.ColumnEnd,
                    exception.ErrorToken.File,
                    exception.ErrorToken.Value
                ));
            }

            return contract;
        }
    }
}
