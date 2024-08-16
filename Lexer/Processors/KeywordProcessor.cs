using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexer.Processors;
using Lexer.Tokens;

namespace Lexer.Processors
{
    public class KeywordProcessor : IProcessor
    {
        public Token? Process(string source)
        {
            // No keyword is less than 2 characters
            if (source.Length < 2)
            {
                return null;
            }

            // Check if the first character is a letter
            if (!char.IsLetter(source[0]))
            {
                return null;
            }

            // Check if the keyword is a valid keyword
            if (Utils.CheckMatchingSequence(source, Keyword.Async.AsString()))
            {
                return Utils.MakeToken(TokenType.Async, Keyword.Async.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Await.AsString()))
            {
                return Utils.MakeToken(TokenType.Await, Keyword.Await.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Class.AsString()))
            {
                return Utils.MakeToken(TokenType.Class, Keyword.Class.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Contract.AsString()))
            {
                return Utils.MakeToken(TokenType.Contract, Keyword.Contract.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Fn.AsString()))
            {
                return Utils.MakeToken(TokenType.Fn, Keyword.Fn.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Let.AsString()))
            {
                return Utils.MakeToken(TokenType.Let, Keyword.Let.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Mut.AsString()))
            {
                return Utils.MakeToken(TokenType.Mutating, Keyword.Mut.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Struct.AsString()))
            {
                return Utils.MakeToken(TokenType.Struct, Keyword.Struct.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Var.AsString()))
            {
                return Utils.MakeToken(TokenType.Var, Keyword.Var.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Module.AsString()))
            {
                return Utils.MakeToken(TokenType.Module, Keyword.Module.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Use.AsString()))
            {
                return Utils.MakeToken(TokenType.Use, Keyword.Use.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Public.AsString()))
            {
                return Utils.MakeToken(TokenType.Public, Keyword.Public.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Private.AsString()))
            {
                return Utils.MakeToken(TokenType.Private, Keyword.Private.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Static.AsString()))
            {
                return Utils.MakeToken(TokenType.Static, Keyword.Static.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.Init.AsString()))
            {
                return Utils.MakeToken(TokenType.Init, Keyword.Init.AsString());
            }

            return null;
        }
    }
}
