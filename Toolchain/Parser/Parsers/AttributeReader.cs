//|--- AttributeReader.cs --------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using AST.Types;
using Lexer;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal static class AttributeReader
    {
        // Consume any leading `#name(args)` runs into a list of AttributeNodes. Stops at the
        // first non-`#` token. Used by parameter/property/method parsers so attribute syntax
        // is uniform across every declaration site.
        internal static List<AttributeNode> ReadAttributes(
            TokenStream stream,
            ExpressionParser expressionParser,
            INode? parent)
        {
            var attrs = new List<AttributeNode>();
            while (!stream.IsEmpty() && stream.Peek().Type == TokenType.Hash)
            {
                stream.Consume(TokenType.Hash, TokenFamily.Special);
                var nameTok = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                attrs.Add(ParseOne(stream, expressionParser, parent, nameTok));
                while (!stream.IsEmpty() && stream.Peek().Type == TokenType.Linebreak)
                {
                    stream.Consume(TokenType.Linebreak, TokenFamily.Special);
                }
            }
            return attrs;
        }

        private static AttributeNode ParseOne(
            TokenStream stream,
            ExpressionParser expressionParser,
            INode? parent,
            Token nameTok)
        {
            var attr = new AttributeNode(nameTok.Value, parent);
            Utils.SetStart(attr, nameTok);
            Utils.SetEnd(attr, nameTok);

            if (stream.IsEmpty() || stream.Peek().Type != TokenType.ParenLeft)
            {
                return attr;
            }
            stream.Consume(TokenType.ParenLeft, TokenFamily.Operator);
            while (!stream.IsEmpty() && stream.Peek().Type != TokenType.ParenRight)
            {
                string argName = "";
                if (stream.Count() >= 2
                    && stream.Peek().Type == TokenType.Identifier
                    && stream.Peek(2)[1].Type == TokenType.Colon)
                {
                    argName = stream.Consume(TokenType.Identifier, TokenFamily.Identifier).Value;
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);
                }
                var argTokens = new List<Token>();
                int paren = 0, bracket = 0, brace = 0;
                while (!stream.IsEmpty())
                {
                    var t = stream.Peek();
                    if (paren == 0 && bracket == 0 && brace == 0
                        && (t.Type == TokenType.Comma || t.Type == TokenType.ParenRight))
                    {
                        break;
                    }
                    if (t.Type == TokenType.ParenLeft) { paren++; }
                    else if (t.Type == TokenType.ParenRight) { paren--; }
                    else if (t.Type == TokenType.BracketLeft) { bracket++; }
                    else if (t.Type == TokenType.BracketRight) { bracket--; }
                    else if (t.Type == TokenType.CurlyLeft) { brace++; }
                    else if (t.Type == TokenType.CurlyRight) { brace--; }
                    argTokens.Add(t);
                    stream.Consume();
                }
                var argExpr = expressionParser.Parse(new TokenStream(argTokens), attr);
                attr.Args.Add(new FuncCallArg(argName, argExpr));
                if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenFamily.Operator);
                }
            }
            var closeParen = stream.Consume(TokenType.ParenRight, TokenFamily.Operator);
            Utils.SetEnd(attr, closeParen);
            return attr;
        }
    }
}
