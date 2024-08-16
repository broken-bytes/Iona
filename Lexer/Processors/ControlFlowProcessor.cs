using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexer.Tokens;

namespace Lexer.Processors
{
    internal class ControlFlowProcessor : IProcessor
    {
        public Token? Process(string source)
        {
            if (source.Length < 2)
            {
                return null;
            }

            if (Utils.CheckMatchingSequence(source, Keyword.If.AsString()))
            {
                return Utils.MakeToken(TokenType.If, Keyword.If.AsString());
            }

            if (Utils.CheckMatchingSequence(source, Keyword.Else.AsString()))
            {
                return Utils.MakeToken(TokenType.Else, Keyword.Else.AsString());
            }

            if (Utils.CheckMatchingSequence(source, Keyword.Do.AsString()))
            {
                return Utils.MakeToken(TokenType.Do, Keyword.Do.AsString());
            }

            if (Utils.CheckMatchingSequence(source, Keyword.While.AsString()))
            {
                return Utils.MakeToken(TokenType.While, Keyword.While.AsString());
            }

            if (Utils.CheckMatchingSequence(source, Keyword.For.AsString()))
            {
                return Utils.MakeToken(TokenType.For, Keyword.For.AsString());
            }

            if (Utils.CheckMatchingSequence(source, Keyword.Break.AsString()))
            {
                return Utils.MakeToken(TokenType.Break, Keyword.Break.AsString());
            }

            if (Utils.CheckMatchingSequence(source, Keyword.Continue.AsString()))
            {
                return Utils.MakeToken(TokenType.Continue, Keyword.Continue.AsString());
            }

            if (Utils.CheckMatchingSequence(source, Keyword.Return.AsString()))
            {
                return Utils.MakeToken(TokenType.Return, Keyword.Return.AsString());
            }

            if (Utils.CheckMatchingSequence(source, Keyword.When.AsString()))
            {
                return Utils.MakeToken(TokenType.When, Keyword.When.AsString());
            }

            if (Utils.CheckMatchingSequence(source, Keyword.Try.AsString()))
            {
                return Utils.MakeToken(TokenType.Try, Keyword.Try.AsString());
            }

            return null;
        }
    }
}
