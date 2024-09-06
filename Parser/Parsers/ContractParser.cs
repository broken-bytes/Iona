using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using System.Net.Security;

namespace Parser.Parsers
{
    public class ContractParser : IParser
    {
        FuncParser funcParser;
        PropertyParser propertyParser;

        internal ContractParser(FuncParser funcParser, PropertyParser propertyParser)
        {
            this.funcParser = funcParser;
            this.propertyParser = propertyParser;
        }

        public INode Parse(TokenStream stream)
        {
            try
            {
                AccessLevel accessLevel = (this as IParser).ParseAccessLevel(stream); 

                // Consume the contract keyword
                stream.Consume(TokenType.Contract, TokenFamily.Keyword);

                // Consume the contract name
                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                var contract = new ContractNode(name.Value, accessLevel);
                contract.GenericArguments = (this as IParser).ParseGenericArgs(stream);

                // Consume the opening brace
                stream.Consume(TokenType.CurlyLeft, TokenFamily.Keyword);

                contract.Body = new BlockNode(contract);

                // Parse the contract body
                while (stream.Peek().Type != TokenType.CurlyRight)
                {
                    // Contracts may have props or funcs
                    var token = stream.Peek();
                    switch (token.Type)
                    {
                        case TokenType.Fn:
                            contract.Body.Children.Add(funcParser.Parse(stream));
                            break;
                        case TokenType.Var or TokenType.Let:
                            contract.Body.Children.Add(propertyParser.Parse(stream));
                            break;
                        default:
                            contract.Body.Children.Add(
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
                }

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
            throw new System.NotImplementedException();
        }
    }
}
