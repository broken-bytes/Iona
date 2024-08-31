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
                    module.Module += token.Value;
                    token = stream.Peek();
                }
            }
            catch(TokenStreamException exception)
            {
                var error = new ErrorNode("ERROR", 0, 0, 0, exception.Message, module);

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
