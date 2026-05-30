//|--- TypeParser.cs -------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using Shared;
using System.Net.Http.Headers;
using System.Text;

namespace Parser.Parsers
{
    internal class TypeParser
    {
        internal TypeParser()
        {
        }

        public TypeReferenceNode? Parse(TokenStream stream, INode? parent)
        {
            // The type may be an array type([T])
            var token = stream.Peek();

            var endToken = token;

            var nameBuilder = new StringBuilder();

            // Function type: `(T1, T2) -> R` (or `() -> R`). Distinguished from a grouped
            // type by the `->` after the closing `)`.
            if (token.Type == TokenType.ParenLeft && IsFunctionTypeAhead(stream))
            {
                return ParseFunctionType(stream, parent);
            }

            if (token.Type == TokenType.BracketLeft)
            {
                token = stream.Consume(TokenType.BracketLeft, TokenFamily.Keyword);

                // Parse the type of the array
                var arrayType = Parse(stream, parent);

                var end = stream.Consume(TokenType.BracketRight, TokenFamily.Keyword);

                if (arrayType == null)
                {
                    return null;
                }

                var arrayRef = new TypeReferenceNode("Array");
                arrayRef.GenericArguments.Add(arrayType);

                Utils.SetStart(arrayRef, token);
                Utils.SetEnd(arrayRef, end);

                return ApplyOptionalOrWeak(stream, arrayRef);
            }

            // We need to be able to parse types and generics
            var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

            nameBuilder.Append(identifier.Value);

            // A type may be qualified by modules (IO.File) and not just the type name
            while (stream.Peek().Type == TokenType.Dot)
            {
                // Consume the dot token
                stream.Consume(TokenType.Dot, TokenFamily.Operator);

                // Consume the identifier
                var nextNamePart = stream.Consume(TokenType.Identifier, TokenFamily.Keyword);

                nameBuilder.Append(".");
                nameBuilder.Append(nextNamePart.Value);
                endToken = nextNamePart;
            }

            // Check if the type is a generic
            if (stream.Peek().Type == TokenType.ArrowLeft)
            {
                // Consume the less than token
                stream.Consume(TokenType.ArrowLeft, TokenFamily.Operator);

                var genericType = new TypeReferenceNode(token.Value);
                Utils.SetStart(genericType, token);

                while (stream.Peek().Type != TokenType.ArrowRight)
                {
                    // Parse the generic argument
                    ITypeReferenceNode? genericArg = Parse(stream, parent);

                    if (genericArg == null)
                    {
                        EmitTypeNotFoundError(genericType.Name, genericType.Meta);
                        return null;
                    }

                    // Add the generic argument to the list of generic arguments
                    // of the generic type
                    genericType.GenericArguments.Add(genericArg);

                    // Check if there are more generic arguments
                    if (stream.Peek().Type == TokenType.Comma)
                    {
                        // Consume the comma token
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);
                    }
                }

                token = stream.Consume(TokenType.ArrowRight, TokenFamily.Operator);
                Utils.SetEnd(genericType, token);

                return ApplyOptionalOrWeak(stream, genericType);
            }

            var type = new TypeReferenceNode(nameBuilder.ToString(), parent);

            Utils.SetStart(type, token);
            Utils.SetEnd(type, endToken);

            return ApplyOptionalOrWeak(stream, type);
        }

        private TypeReferenceNode ApplyOptionalOrWeak(TokenStream stream, TypeReferenceNode type)
        {
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.SoftUnwrap)
            {
                var token = stream.Consume(TokenType.SoftUnwrap, TokenFamily.Keyword);
                type.IsOptional = true;
                Utils.SetEnd(type, token);
            }
            else if (!stream.IsEmpty() && (stream.Peek().Type == TokenType.Not || stream.Peek().Type == TokenType.HardUnwrap))
            {
                var token = stream.Consume();
                type.IsImplicitlyUnwrapped = true;
                Utils.SetEnd(type, token);
            }

            return type;
        }

        // Cheap lookahead: walk a parenthesised region, counting depth, then check whether
        // the immediately-following token is `->`. Stops at the first imbalance.
        private static bool IsFunctionTypeAhead(TokenStream stream)
        {
            int depth = 0;
            int idx = 0;
            var snapshot = stream.Peek(Math.Min(stream.Count(), 256));
            foreach (var t in snapshot)
            {
                if (t.Type == TokenType.ParenLeft) { depth++; }
                else if (t.Type == TokenType.ParenRight)
                {
                    depth--;
                    if (depth == 0)
                    {
                        idx++;
                        return idx < snapshot.Count && snapshot[idx].Type == TokenType.Arrow;
                    }
                }
                idx++;
            }
            return false;
        }

        private FunctionTypeNode ParseFunctionType(TokenStream stream, INode? parent)
        {
            var lparen = stream.Consume(TokenType.ParenLeft, TokenFamily.Grouping);
            var fnType = new FunctionTypeNode(parent);
            Utils.SetStart(fnType, lparen);

            while (stream.Peek().Type != TokenType.ParenRight)
            {
                var paramType = Parse(stream, fnType);
                if (paramType != null)
                {
                    fnType.ParameterTypes.Add(paramType);
                }
                if (stream.Peek().Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenFamily.Operator);
                }
            }
            stream.Consume(TokenType.ParenRight, TokenFamily.Grouping);
            stream.Consume(TokenType.Arrow, TokenFamily.Operator);

            fnType.ReturnType = Parse(stream, fnType);

            // Encode arity in the FQN so DeclPass can register a parent-less TypeSymbol with
            // the right name before TypeResolver runs later.
            var arity = fnType.ParameterTypes.Count;
            var retName = fnType.ReturnType?.Name;
            var isVoid = retName == "Void";
            fnType.FullyQualifiedName = isVoid
                ? (arity == 0 ? "System.Action" : $"System.Action`{arity}")
                : $"System.Func`{arity + 1}";

            return (FunctionTypeNode)ApplyOptionalOrWeak(stream, fnType);
        }

        private void EmitTypeNotFoundError(string typeName, Metadata meta)
        {
            CompilerErrorFactory.TopLevelDefinitionError(
                typeName,
                meta
            );
        }
    }
}
