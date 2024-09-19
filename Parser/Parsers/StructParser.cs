using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class StructParser
    {
        private readonly AccessLevelParser accessLevelParser;
        private readonly GenericArgsParser genericArgsParser;
        private readonly TypeParser typeParser;
        private StatementParser? statementParser;

        internal StructParser(
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

        internal bool IsStruct(Lexer.Tokens.TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (tokens[0].Type is TokenType.Struct)
            {
                return true;
            }

            if (accessLevelParser.IsAccessLevel(tokens[0]) && tokens[1].Type is TokenType.Struct)
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

            StructNode? structNode = null;

            try
            {
                AccessLevel accessLevel = accessLevelParser.Parse(stream);

                // Consume the contract keyword
                var token = stream.Consume(TokenType.Struct, TokenFamily.Keyword);

                // Consume the contract name
                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                structNode = new StructNode(name.Value, accessLevel, parent);
                Utils.SetStart(structNode, token);
                Utils.SetEnd(structNode, name);
                structNode.GenericArguments = genericArgsParser.Parse(stream, structNode);

                if(structNode.GenericArguments.Count > 0)
                {
                    Utils.SetColumnEnd(structNode, structNode.GenericArguments[structNode.GenericArguments.Count - 1].Meta.ColumnEnd);
                }

                // Check if the struct fulfills a contract
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);

                    var contract = typeParser.Parse(stream, structNode);
                    structNode.Contracts.Add(contract);

                    while (stream.Peek().Type != TokenType.CurlyLeft)
                    {
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);

                        contract = typeParser.Parse(stream, structNode);
                        structNode.Contracts.Add(contract);
                    }
                }

                // Consume the opening brace
                token = stream.Consume(TokenType.CurlyLeft, TokenFamily.Keyword);
                structNode.Body = new BlockNode(structNode);
                Utils.SetStart(structNode.Body, token);

                token = stream.Peek();

                while (token.Type == TokenType.Linebreak)
                {
                    stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                    token = stream.Peek();
                }

                // Parse the contract body
                while (token.Type != TokenType.CurlyRight)
                {
                    // Structs may have props or funcs

                    structNode.Body.AddChild(statementParser.Parse(stream, structNode.Body));
                    token = stream.Peek();

                    while (token.Type == TokenType.Linebreak)
                    {
                        stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                        token = stream.Peek();
                    }
                }

                // Consume the closing brace
                token = stream.Consume(TokenType.CurlyRight, TokenFamily.Keyword);
                Utils.SetEnd(structNode.Body, token);
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
                    exception.ErrorToken.Value
                ));

                // TODO: Proper error metadata
            }

            return structNode;
        }
    }
}
