using AST.Nodes;
using Lexer.Tokens;
using Parser.Parsers;

namespace Parser
{
    public class Parser : IParser
    {
        private readonly StatementParser statementParser;

        internal Parser(StatementParser statementParser)
        {
            this.statementParser = statementParser;
        }

        public INode Parse(TokenStream stream)
        {
            // Create a file node
            var fileNode = new FileNode(stream.First().File);

            var module = statementParser.Parse(stream, fileNode);

            fileNode.AddChild(module);

            return fileNode;
        }
    }
}
