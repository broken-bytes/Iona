﻿using Lexer.Processors;
using Lexer.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    public class Lexer
    {
        private List<IProcessor> processors;

        public Lexer()
        {
            processors = new List<IProcessor>();
            var numberProcessor = new NumberProcessor();

            processors.Add(new ControlFlowProcessor());
            processors.Add(new GroupingProcessor());
            processors.Add(new KeywordProcessor());
            processors.Add(new OperatorProcessor());
            processors.Add(new PunctuationProcessor());
            processors.Add(new SpecialProcessor());
            processors.Add(new IdentifierProcessor());
            processors.Add(new LiteralProcessor(numberProcessor));
        }

        public TokenStream Tokenize(string code, string fileName)
        {
            var tokens = new List<Token>();
            int start = 0;
            int currentLine = 1;

            while (start < code.Length)
            {
                string substring = code.Substring(start);

                int whitespaceCount = Utils.DropWhitespace(substring);
                start += whitespaceCount;

                if (start >= code.Length)
                {
                    break;
                }

                // Handle comments
                int? newStart = HandleComments(substring, start);
                if (newStart.HasValue)
                {
                    start = newStart.Value;
                    continue;
                }

                // Try each processor until one succeeds
                foreach (var processor in processors)
                {
                    if (processor.Process(substring) is Token token)
                    {
                        Utils.UpdateToken(token, fileName, currentLine, start + 1);
                        start += token.Value.Length;
                        tokens.Add(token);
                        break;
                    }
                }

                // Handle line breaks
                if (code[start] == '\n')
                {
                    currentLine++;
                    start++;
                    tokens.Add(Utils.MakeToken(TokenType.Linebreak, "\n"));
                }
            }

            // Add EOF token
            tokens.Add(Utils.MakeToken(TokenType.EndOfFile, ""));

            return new TokenStream(tokens);
        }

        private int? HandleComments(string substring, int currentStart)
        {
            if (substring.StartsWith("//"))
            {
                // Single-line comment: skip to the end of the line
                int endOfLine = substring.IndexOf('\n');
                if (endOfLine == -1)
                {
                    endOfLine = substring.Length;
                }
                return currentStart + endOfLine;
            }
            else if (substring.StartsWith("/*"))
            {
                // Multi-line comment: find the closing "*/"
                int endOfComment = substring.IndexOf("*/");
                if (endOfComment != -1)
                {
                    return currentStart + endOfComment + 2; // +2 to include the "*/"
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
