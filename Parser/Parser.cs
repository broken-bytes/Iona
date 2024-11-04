using AST.Nodes;
using Lexer.Tokens;
using Parser.Parsers;
using System.Reflection;

namespace Parser
{
    public class Parser : IParser
    {
        private readonly StatementParser statementParser;

        internal Parser(StatementParser statementParser)
        {
            this.statementParser = statementParser;
        }

        public INode Parse(TokenStream stream, string assemblyName)
        {
            // Create a file node
            var fileNode = new FileNode(stream.First().File);

            var module = (ModuleNode)statementParser.Parse(stream, fileNode);
            module.Assembly = assemblyName;

            fileNode.AddChild(module);

            return fileNode;
        }
    }
}
