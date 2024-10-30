using Lexer.Processors;
using Lexer.Tokens;
using Shared;

namespace Lexer
{
    public class Lexer : ILexer
    {
        private readonly List<IProcessor> processors;
        private readonly IErrorCollector errorCollector;
        private readonly IWarningCollector warningCollector;
        private readonly IFixItCollector fixItCollector;

        internal Lexer(
            List<IProcessor> processors, 
            IErrorCollector errorCollector,
            IWarningCollector warningCollector,
            IFixItCollector fixItCollector
        )
        {
            this.processors = processors;
            this.errorCollector = errorCollector;
            this.warningCollector = warningCollector;
            this.fixItCollector = fixItCollector;
        }

        public TokenStream Tokenize(string code, string fileName)
        {
            var tokens = new List<Token>();
            int start = 0;
            int column = 1;
            int currentLine = 1;

            while (start < code.Length)
            {
                string substring = code.Substring(start);

                int whitespaceCount = Utils.DropWhitespace(substring);
                start += whitespaceCount;
                column += whitespaceCount;

                if (start >= code.Length)
                {
                    break;
                }

                // Handle comments
                var commentAfter = HandleComments(substring, start, currentLine);
                if (commentAfter.HasValue)
                {
                    start = commentAfter.Value.Item1;
                    column = 1;
                    currentLine = commentAfter.Value.Item2;
                    continue;
                }

                // Try each processor until one succeeds
                foreach (var processor in processors)
                {
                    if (processor.Process(substring) is Token token)
                    {
                        Utils.UpdateToken(ref token, fileName, currentLine, column);
                        start += token.Value.Length;
                        column += token.Value.Length;
                        tokens.Add(token);

                        if(token.Type == TokenType.Linebreak)
                        {
                            currentLine++;
                            column = 1;
                        }

                        break;
                    }
                }

                // Handle line breaks
                if (start < code.Length && code[start] == '\n')
                {
                    currentLine++;
                    start++;
                    column = 1;
                    tokens.Add(Utils.MakeToken(TokenType.Linebreak, "\n"));
                }
            }

            // Add EOF token
            tokens.Add(Utils.MakeToken(TokenType.EndOfFile, ""));

            return new TokenStream(tokens);
        }

        private (int, int)? HandleComments(string substring, int currentStart, int currentLine)
        {
            if (substring.StartsWith("//"))
            {
                // Single-line comment: skip to the end of the line
                int endOfLine = substring.IndexOf('\n');
                if (endOfLine == -1)
                {
                    endOfLine = substring.Length;
                }
                return (currentStart + endOfLine, currentLine);
            }
            else if (substring.StartsWith("/*"))
            {
                var currLine = currentLine;
                // Multi-line comment: find the closing "*/"
                int endOfComment = substring.IndexOf("*/");
                if (endOfComment != -1)
                {
                    var counter = 0;
                    while (counter < endOfComment)
                    {
                        if (substring[counter] == '\n')
                        {
                            // We need to increase the line count for each line break in the comment
                            currLine++;
                        }
                        counter++;
                    }

                    // We need to increase the line count for each line break in the comment
                    return (currentStart + endOfComment + 2, currLine); // +2 to include the "*/"
                }
                else
                {
                    // TODO: Handle error (e.g., throw an exception or return a special error token)
                    return null;
                }
            }

            return null; // No comment found
        }
    }
}
