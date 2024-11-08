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

                // Consume the class keyword
                var token = stream.Consume(TokenType.Class, TokenFamily.Keyword);

                // Consume the class name
                var name = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                classNode = new ClassNode(name.Value, accessLevel, parent);
                classNode.FullyQualifiedName = Utils.ResolveFullyQualifiedName(classNode);

                classNode.GenericArguments = genericArgsParser.Parse(stream, classNode);

                Utils.SetMeta(classNode, new List<Token> { token, name });

                if(classNode.GenericArguments.Count > 0)
                {
                    // Increase the column end to include the generic arguments + 1 for the closing bracket
                    Utils.SetColumnEnd(classNode, classNode.GenericArguments[classNode.GenericArguments.Count - 1].Meta.ColumnEnd + 1);
                }

                // Check if the class fulfills a contract
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);

                    var reference = typeParser.Parse(stream, classNode);

                    if (reference != null)
                    {
                        classNode.Contracts.Add(reference);
                    }

                    while (stream.Peek().Type != TokenType.CurlyLeft)
                    {
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);

                        var contract = typeParser.Parse(stream, classNode);
                        if (contract.TypeKind != Kind.Contract)
                        {
                            // TODO: Error when multiple classes
                        }
                        classNode.Contracts.Add(contract);
                        Utils.SetColumnEnd(classNode, contract.Meta.ColumnEnd + 1);
                    }

                    Utils.IncreaseColumn(classNode, 1);
                }

                // Consume the opening brace
                token = stream.Consume(TokenType.CurlyLeft, TokenFamily.Keyword);
                classNode.Body = new BlockNode(classNode);
                Utils.SetMeta(classNode.Body, new List<Token> { token });

                token = stream.Peek();

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
                token = stream.Consume(TokenType.CurlyRight, TokenFamily.Keyword);
                Utils.SetEnd(classNode.Body, token);
            }
            catch (TokenStreamException exception)
            {
                if (classNode == null)
                {
                    classNode = new ClassNode("Error", AccessLevel.Internal);
                    Utils.SetMeta(classNode, new List<Token> { exception.ErrorToken });
                }

                if (classNode.Body == null)
                {
                    classNode.Body = new BlockNode(classNode);
                }

                throw new ParserException(
                    ParserExceptionCode.Unknown,
                    exception.ErrorToken.Line,
                    exception.ErrorToken.ColumnStart,
                    exception.ErrorToken.ColumnEnd,
                    exception.ErrorToken.File
                );
            }

            return classNode;
        }
    }
}
