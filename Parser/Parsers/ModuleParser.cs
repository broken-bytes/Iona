using AST.Nodes;
using AST.Types;
using Lexer.Tokens;


namespace Parser.Parsers
{
    public class ModuleParser : IParser
    {
        ClassParser classParser;
        ContractParser contractParser;
        FuncParser funcParser;
        StructParser structParser;
        VariableParser variableParser;

        internal ModuleParser(
            ClassParser classParser,
            ContractParser contractParser, 
            FuncParser funcParser, 
            VariableParser variableParser,
            StructParser structParser
        )
        {
            this.classParser = classParser;
            this.contractParser = contractParser;
            this.funcParser = funcParser;
            this.variableParser = variableParser;
            this.structParser = structParser;
        }

        public INode Parse(TokenStream stream, INode? parent)
        {
            // Peek so we have a valid token to begin with
            ModuleNode? module = null;

            try
            {
                stream.Consume(TokenType.Module, TokenFamily.Keyword);

                module = new ModuleNode("", null);

                var token = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                module.Name = token.Value;

                token = stream.Peek();
                // Parse until we reach the end of the module declaration (whitespace or linebreak)
                while (token.Type == TokenType.Dot)
                {
                    stream.Consume(TokenType.Dot, TokenFamily.Keyword);
                    token = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
                    module.Name += token.Value;
                    token = stream.Peek();
                }

                token = stream.Peek();

                while(token.Type == TokenType.Linebreak)
                {
                    stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                    token = stream.Peek();
                }

                // Parse classes, contracts, structs, etc.
                while (token.Type != TokenType.EndOfFile)
                {
                    switch (token.Type)
                    {
                        case TokenType.Class:
                            var clazz = classParser.Parse(stream);
                            module.AddChild(clazz);
                            break;
                        case TokenType.Contract:
                            var contract = contractParser.Parse(stream);
                            module.AddChild(contract);
                            break;
                        case TokenType.Struct:
                            var structure = structParser.Parse(stream);
                            module.AddChild(structure);
                            break;
                        case TokenType.Fn or TokenType.Mutating:
                            var func = funcParser.Parse(stream);
                            module.AddChild(func);
                            break;
                        case TokenType.Var or TokenType.Let:
                            var variable = variableParser.Parse(stream);
                            module.AddChild(variable);
                            break;
                        default:
                            module.AddChild(
                                new ErrorNode(
                                    token.Line,
                                    token.ColumnStart,
                                    token.ColumnEnd,
                                    token.File,
                                    $"Unexpected token {token.Value}"
                                )
                            );
                            break;
                    }

                    token = stream.Peek();

                    while (token.Type == TokenType.Linebreak)
                    {
                        stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                        token = stream.Peek();
                    }
                }
            }
            catch (TokenStreamException exception)
            {
                var error = new ErrorNode(
                    exception.ErrorToken.Line,
                    exception.ErrorToken.ColumnStart,
                    exception.ErrorToken.ColumnEnd,
                    exception.ErrorToken.File,
                    exception.Message
                );

                if (module != null)
                {
                    module.Children.Add(error);

                    return module;
                }

                return error;
            }

            return module;
        }
    }
}
