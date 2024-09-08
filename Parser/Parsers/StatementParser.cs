using AST.Nodes;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal class StatementParser : IParser
    {
        ExpressionParser expressionParser;

        internal StatementParser(ExpressionParser expressionParser)
        {
            this.expressionParser = expressionParser;
        }

        public INode Parse(TokenStream stream)
        {
            throw new NotImplementedException();
        }

        public bool IsStatement(TokenStream stream)
        {
            return isCompoundAssignment(stream);
        } 

        // ------------------- Helper methods -------------------
        private INode ParseAssignment(TokenStream stream)
        {
            if(isCompoundAssignment(stream))
            {
                return ParseCompoundAssignment(stream);
            }

            throw new NotImplementedException();
        }

        private INode ParseCompoundAssignment(TokenStream stream)
        {
            throw new NotImplementedException();
        }

        private bool isCompoundAssignment(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (
                (tokens[0].Family == TokenFamily.Identifier) &&
                IsCompoundOperator(tokens[1])
            )
            {
                return true;
            }

            return false;
        }

        private bool IsCompoundOperator(Token token)
        {
            // If the token is not an operator, it cannot be a compound operator
            if (token.Family != TokenFamily.Operator)
            {
                return false;
            }

            // Check what token it is
            switch (token.Value)
            {
                case "+=":
                case "-=":
                case "*=":
                case "/=":
                case "%=":
                    return true;
                default:
                    return false;
            }
        }
    }
}
