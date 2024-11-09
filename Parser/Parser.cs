using AST.Nodes;
using Lexer.Tokens;
using Parser.Parsers;
using System.Reflection;
using Shared;

namespace Parser
{
    public class Parser : IParser
    {
        private readonly StatementParser _statementParser;
        private readonly IErrorCollector _errorCollector;

        internal Parser(
            StatementParser statementParser,
            IErrorCollector errorCollector
            )
        {
            _statementParser = statementParser;
            _errorCollector = errorCollector;
        }

        public INode Parse(TokenStream stream, string assemblyName)
        {
            // Create a file node
            var fileNode = new FileNode(stream.First().File);
            
            Utils.SetMeta(fileNode, stream.First());
            
            var firstNode = _statementParser.Parse(stream, fileNode);

            if (firstNode is not ModuleNode module)
            {
                var error = CompilerErrorFactory.SyntaxError("Files need to start with a module declaration", fileNode.Meta);
                
                _errorCollector.Collect(error);
                
                return fileNode;
            }
            
            module.Assembly = assemblyName;

            fileNode.AddChild(module);

            return fileNode;
        }
    }
}
