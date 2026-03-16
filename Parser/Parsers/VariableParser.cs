using AST.Nodes;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class VariableParser
    {
        private readonly ExpressionParser expressionParser;

        internal VariableParser(ExpressionParser expressionParser)
        {
            this.expressionParser = expressionParser;
        }

        internal bool IsVariable(TokenStream stream)
        {
            return stream.Peek().Type is TokenType.Var or TokenType.Let;
        }

        public INode Parse(TokenStream stream, INode? parent)
        {
            var token = stream.Peek();
            bool isMutable = token.Type == TokenType.Var;

            if (token.Type == TokenType.Var)
            {
                stream.Consume(TokenType.Var, TokenFamily.Keyword);
            }
            else if (token.Type == TokenType.Let)
            {
                stream.Consume(TokenType.Let, TokenFamily.Keyword);
            }

            try
            {
                var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);

                var varNode = new VariableNode(identifier.Value, null, parent) { IsMutable = isMutable };
                Utils.SetStart(varNode, token);
                Utils.SetEnd(varNode, identifier);

                // Check if the variable has a type (next token is a colon)
                token = stream.Peek();
                if (token.Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Keyword);
                    var type = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                    varNode.TypeNode = new TypeReferenceNode(type.Value, varNode);
                    Utils.SetEnd(varNode.TypeNode, type);

                    // Check for optional (?) or weak (!) suffix
                    if (!stream.IsEmpty() && stream.Peek().Type == TokenType.SoftUnwrap)
                    {
                        var suffixToken = stream.Consume(TokenType.SoftUnwrap, TokenFamily.Keyword);
                        varNode.TypeNode.IsOptional = true;
                        Utils.SetEnd(varNode.TypeNode, suffixToken);
                    }
                    else if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Not)
                    {
                        var suffixToken = stream.Consume(TokenType.Not, TokenFamily.Keyword);
                        varNode.TypeNode.IsImplicitlyUnwrapped = true;
                        Utils.SetEnd(varNode.TypeNode, suffixToken);
                    }
                }

                // Check if the variable has a value (next token is an equals sign)
                token = stream.Peek();
                if (token.Type == TokenType.Assign)
                {
                    stream.Consume(TokenType.Assign, TokenFamily.Keyword);
                    var node = expressionParser.Parse(stream, varNode);
                    varNode.Value = node;
                }

                return varNode;
            }
            catch (ParserException exception)
            {
                return new ErrorNode(
                    exception.Message,
                    parent
                );

                // TODO: Proper error metadata
            }
        }
    }
}
