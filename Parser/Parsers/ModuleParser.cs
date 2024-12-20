﻿using AST.Nodes;
using Lexer.Tokens;


namespace Parser.Parsers
{
    public class ModuleParser
    {
        private StatementParser? statementParser;

        internal ModuleParser()
        {

        }

        internal void Setup(StatementParser statementParser)
        {
            this.statementParser = statementParser;
        }

        internal bool IsModule(TokenStream stream)
        {
            if (stream.First().Type is TokenType.Module)
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

            // Peek so we have a valid token to begin with
            ModuleNode? module = null;

            try
            {
                var token = stream.Consume(TokenType.Module, TokenFamily.Keyword);

                module = new ModuleNode("", "", parent);
                Utils.SetMeta(module, token);

                token = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                module.Name = token.Value;

                token = stream.Peek();
                // Parse until we reach the end of the module declaration (whitespace or linebreak)
                while (token.Type == TokenType.Dot)
                {
                    stream.Consume(TokenType.Dot, TokenFamily.Keyword);
                    token = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);
                    module.Name += token.Value;
                    Utils.SetEnd(module, token);
                    token = stream.Peek();
                }

                token = stream.Peek();

                while (token.Type == TokenType.Linebreak)
                {
                    stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                    token = stream.Peek();
                }

                // Parse classes, contracts, structs, etc.
                while (token.Type != TokenType.EndOfFile)
                {
                    var child = statementParser.Parse(stream, module);
                    
                    // TODO: Rework this so the file parser handles this.
                    // For now, add `use` nodes to the parent of Module which is always the file

                    if (child is ImportNode import)
                    {
                        var file = module.Parent as FileNode;
                        file.Children.Add(import);
                    }
                    else if (child is not null)
                    {
                        module.AddChild(child);
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
                    exception.Message,
                    module,
                    module
                );

                // TODO: Proper error metadata

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
