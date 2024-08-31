using AST.Nodes;
using AST.Types;
using Lexer.Tokens;


namespace Parser.Parsers
{
    public class ModuleParser : IParser
    {
        internal ModuleParser()
        {
        }

        public INode Parse(TokenStream stream)
        {
            // Peek so we have a valid token to begin with
            var token = stream.Peek();
            INode? node = null;

            try
            {
                token = stream.Consume(TokenType.Contract);

                var module = new ModuleNode(token.Value, null);
                node = module;
            }
            catch (TokenStreamWrongTypeException exception)
            {
                node = new ErrorNode("ERROR", token.ColumnStart, token.ColumnEnd, token.Line, exception.Message, node);
            }
            catch(TokenStreamEmptyException exception)
            {
                node = new ErrorNode("ERROR", 0, 0, 0, exception.Message, node);
            }

            return node;
        }
    }
}
