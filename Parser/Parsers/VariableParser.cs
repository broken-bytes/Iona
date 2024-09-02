using AST.Nodes;
using Lexer.Tokens;

namespace Parser.Parsers
{
    public class VariableParser
    {
        internal VariableParser()
        {
        }

        public INode Parse(TokenStream stream)
        {
            var token = stream.Peek();
            if (token.Type == TokenType.Var)
            {
                stream.Consume(TokenType.Var, TokenFamily.Keyword);
            } 
            else if(token.Type == TokenType.Let)
            {
                stream.Consume(TokenType.Let, TokenFamily.Keyword);
            }

            var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
            var varNode = new VariableNode(identifier.Value, null);

            // Check if the variable has a type (next token is a colon)
            token = stream.Peek();
            if (token.Type == TokenType.Colon)
            {
                stream.Consume(TokenType.Colon, TokenFamily.Keyword);
                var type = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                varNode.VariableType = new AST.Nodes.Type(type.Value);
            }

            // Check if the variable has a value (next token is an equals sign)
            token = stream.Peek();
            if (token.Type == TokenType.Equal)
            {
                stream.Consume(TokenType.Equal, TokenFamily.Keyword);

            }
        }
    }
}
