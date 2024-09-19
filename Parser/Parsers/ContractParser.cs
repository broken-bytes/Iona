using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class ContractParser
    {
        private StatementParser? statementParser;
        private readonly AccessLevelParser accessLevelParser;
        private readonly GenericArgsParser genericArgsParser;
        private readonly TypeParser typeParser;

        internal ContractParser(
            AccessLevelParser accessLevelParser,
            GenericArgsParser genericArgsParser,
            TypeParser typeParser
        )
        {
            this.accessLevelParser = accessLevelParser;
            this.genericArgsParser = genericArgsParser;
            this.typeParser = typeParser;
        }

        internal void Setup(StatementParser statementParser)
        {
            this.statementParser = statementParser;
        }

        internal bool IsContract(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (tokens[0].Type is TokenType.Contract)
            {
                return true;
            }

            if (accessLevelParser.IsAccessLevel(tokens[0]) && tokens[1].Type is TokenType.Contract)
            {
                return true;
            }

            return false;
        }

        public INode Parse(TokenStream stream, INode? parent)
        {
            if (statementParser == null)
            {
                var error = stream.Peek();
                throw new ParserException(ParserExceptionCode.Unknown, error.Line, error.ColumnStart, error.ColumnEnd, error.File);
            }

            ContractNode? contract = null;

            try
            {
                AccessLevel accessLevel = accessLevelParser.Parse(stream);

                // Consume the contract keyword
                stream.Consume(TokenType.Contract, TokenFamily.Keyword);

                // Consume the contract name
                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                contract = new ContractNode(name.Value, accessLevel, parent);
                contract.GenericArguments = genericArgsParser.Parse(stream, contract);
                contract.Body = new BlockNode(contract);

                // Check if the struct fulfills a contract
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);

                    var refinement = typeParser.Parse(stream, contract);
                    contract.Refinements.Add(refinement);

                    while (stream.Peek().Type != TokenType.CurlyLeft)
                    {
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);

                        refinement = typeParser.Parse(stream, contract);
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
                    contract.Body.AddChild(statementParser.Parse(stream, contract.Body));

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
                    exception.ErrorToken.Value
                ));

                // TODO: Proper error metadata
            }

            return contract;
        }
    }
}
