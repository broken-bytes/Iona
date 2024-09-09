using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class ClassParser
    {
        private StatementParser? statementParser;
        private readonly AccessLevelParser accessLevelParser;
        private readonly GenericArgsParser genericArgsParser;
        private readonly TypeParser typeParser;

        internal ClassParser(
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

        internal bool IsClass(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (tokens[0].Type is TokenType.Class)
            {
                return true;
            }

            if (accessLevelParser.IsAccessLevel(tokens[0]) && tokens[1].Type is TokenType.Class)
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

            ClassNode? classNode = null;

            try
            {
                AccessLevel accessLevel = accessLevelParser.Parse(stream);

                // Consume the contract keyword
                stream.Consume(TokenType.Class, TokenFamily.Keyword);

                // Consume the contract name
                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                classNode = new ClassNode(name.Value, accessLevel, parent);
                classNode.GenericArguments = genericArgsParser.Parse(stream);

                // Check if the class fulfills a contract
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
                classNode.Body = new BlockNode(classNode);

                var token = stream.Peek();

                while (token.Type == TokenType.Linebreak)
                {
                    stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                    token = stream.Peek();
                }

                // Parse the contract body
                while (token.Type != TokenType.CurlyRight)
                {
                    classNode.Body.AddChild(statementParser.Parse(stream, classNode.Body));

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
            catch (TokenStreamException exception)
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
                    exception.ErrorToken.Line,
                    exception.ErrorToken.ColumnStart,
                    exception.ErrorToken.ColumnEnd,
                    exception.ErrorToken.File,
                    exception.ErrorToken.Value
                ));
            }

            return classNode;
        }
    }
}
