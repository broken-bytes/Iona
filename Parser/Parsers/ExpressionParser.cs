﻿using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using Parser.Parsers;
using Shared;
using System.Xml.Linq;

namespace Parser.Parsers
{
    public class ExpressionParser
    {
        private enum ExpressionState
        {
            Any,
            StartGroup,
            EndGroup,
            MemberAccess,
            ScopeResolution,
            Operand,
            Operator,
            Invalid,
            Finish,
            Skip,
            Param
        }
        
        FuncCallParser funcCallParser;
        MemberAccessParser memberAccessParser;
        ScopeResolutionParser scopeResolutionParser;
        TypeParser typeParser;
        private readonly IErrorCollector errorCollector;

        internal ExpressionParser(
            FuncCallParser funcCallParser,
            MemberAccessParser memberAccessParser,
            ScopeResolutionParser scopeResolutionParser,
            TypeParser typeParser,
            IErrorCollector errorCollector
        )
        {
            this.funcCallParser = funcCallParser;
            this.memberAccessParser = memberAccessParser;
            this.scopeResolutionParser = scopeResolutionParser;
            this.typeParser = typeParser;
            this.errorCollector = errorCollector;
        }

        public IExpressionNode Parse(TokenStream stream, INode? parent)
        {
            return ParseExpression(stream, parent);
        }

        public bool IsExpression(TokenStream stream)
        {
            // Expressions start with an identifier, operator, paren or a literal
            var token = stream.Peek();

            if (
                token.Family is TokenFamily.Literal ||
                token.Family is TokenFamily.Identifier ||
                token.Type is TokenType.ParenLeft ||
                token.Family is TokenFamily.Operator
            )
            {
                return true;
            }

            return false;
        }

        // ------------------- Helper methods -------------------
        private IExpressionNode? ParseExpression(TokenStream stream, INode? parent)
        {
            var tokens = new List<Token>();
            var token = stream.Peek();
            
            // Track if the last token was an operator. Only `(`, `identifier`, `literal` may follow after an operator 
            // Likewise, if the last token was not an operator, we end the expression if something other than an operator occurs 
            var nextState = NextState(ExpressionState.Any, token);
            while (!stream.IsEmpty())
            {
                if (nextState is ExpressionState.Finish)
                {
                    break;
                }
                switch (nextState)
                {
                    case ExpressionState.Invalid:
                    {
                        var meta = new Metadata
                        {
                            File = token.File,
                            ColumnStart = token.ColumnStart,
                            ColumnEnd = token.ColumnEnd,
                            LineStart = token.Line,
                            LineEnd = token.Line,
                        };

                        var error = CompilerErrorFactory.SyntaxError($"Unexpected token '{token.Value}'", meta);

                        errorCollector.Collect(error);

                        return null;
                    }
                    case ExpressionState.Skip:
                        stream.Consume();
                        break;
                    default:
                        tokens.Add(token);
                        break;
                }

                stream.Consume();
                token = stream.Peek();
                
                nextState = NextState(nextState, token);
            }

            var tokenStream = new TokenStream(tokens);

            var postfix = InfixToPostfix(tokenStream);
            var expression = BuildBinaryExpressionNode(postfix, parent);

            return (IExpressionNode)expression;
        }

        private BinaryOperation GetBinaryOperation(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Plus:
                    return BinaryOperation.Add;
                case TokenType.Minus:
                    return BinaryOperation.Subtract;
                case TokenType.Multiply:
                    return BinaryOperation.Multiply;
                case TokenType.Divide:
                    return BinaryOperation.Divide;
                case TokenType.Modulo:
                    return BinaryOperation.Mod;
                case TokenType.Equal:
                    return BinaryOperation.Equal;
                case TokenType.NotEqual:
                    return BinaryOperation.NotEqual;
                case TokenType.Greater:
                    return BinaryOperation.GreaterThan;
                case TokenType.Less:
                    return BinaryOperation.LessThan;
                case TokenType.GreaterEqual:
                    return BinaryOperation.GreaterThanOrEqual;
                case TokenType.LessEqual:
                    return BinaryOperation.LessThanOrEqual;
                default:
                    return BinaryOperation.Noop;
            }
        }

        private UnaryOperation GetUnaryOperation(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Increment:
                    return UnaryOperation.Increment;
                case TokenType.Decrement:
                    return UnaryOperation.Decrement;
                case TokenType.Not:
                    return UnaryOperation.Not;
                default:
                    return UnaryOperation.Noop;
            }
        }

        private bool IsBinaryOperator(Token token)
        {
            // If the token is not an operator, it cannot be a binary operator
            if (token.Family != TokenFamily.Operator)
            {
                return false;
            }

            // Check what token it is
            switch (token.Value)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                case "%":
                case "==":
                case "!=":
                case ">":
                case "<":
                case ">=":
                case "<=":
                case "&&":
                case "||":
                    return true;
                default:
                    return false;
            }
        }

        private bool IsFunctionCall(TokenStream stream)
        {
            return funcCallParser.IsFuncCall(stream);
        }

        private bool IsMemberAccess(TokenStream stream)
        {
            return memberAccessParser.IsMemberAccess(stream);
        }
        
        private bool IsScopeResolution(TokenStream stream)
        {
            return scopeResolutionParser.IsScopeResolution(stream);
        }

        private bool IsObjectLiteral(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (
                tokens[0].Type == TokenType.Identifier &&
                tokens[1].Type == TokenType.CurlyLeft
            )
            {
                return true;
            }

            return false;
        }

        private bool IsUnaryExpression(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (
                (tokens[0].Family is TokenFamily.Identifier or TokenFamily.Literal) &&
                IsUnaryOperator(tokens[1])
            )
            {
                return true;
            }

            return false;
        }

        private bool IsUnaryOperator(Token token)
        {
            // If the token is not an operator, it cannot be a unary operator
            if (token.Family != TokenFamily.Operator)
            {
                return false;
            }

            // Check what token it is
            switch (token.Value)
            {
                case "++":
                case "--":
                case "!":
                    return true;
                default:
                    return false;
            }
        }

        private int Precedence(BinaryOperation op)
        {
            if (op == BinaryOperation.Add || op == BinaryOperation.Subtract)
            {
                return 1;
            }

            if (op == BinaryOperation.Multiply || op == BinaryOperation.Divide)
            {
                return 2;
            }

            return 0;
        }

        private TokenStream InfixToPostfix(TokenStream stream)
        {
            var output = new List<Token>();
            var stack = new Stack<Token>();

            while (!stream.IsEmpty())
            {
                var token = stream.Consume();
                if (token.Family is TokenFamily.Identifier or TokenFamily.Literal)
                {
                    output.Add(token);
                }
                else if (token.Type == TokenType.ParenLeft)
                {
                    // When we have an identifier followed by a parenthesis without any operator
                    // we have a function call and parse until the closing parenthesis
                    if (output[^1].Type is TokenType.Identifier)
                    {
                        output.Add(token);

                        token = stream.Peek();
                        while (token.Type != TokenType.ParenRight)
                        {
                            if (stream.IsEmpty())
                            {
                                errorCollector.Collect(
                                    CompilerErrorFactory.SyntaxError("Unexpected end of file",
                                        new Metadata
                                        {
                                            ColumnStart = token.ColumnStart,
                                            ColumnEnd = token.ColumnEnd,
                                            LineStart = token.Line,
                                            LineEnd = token.Line,
                                            File = token.File
                                        }
                                    )
                                );
                                break;
                            }

                            output.Add(token);

                            stream.Consume();
                            token = stream.Peek();
                        }

                        output.Add(token);

                        stream.Consume();

                        continue;
                    }
                    stack.Push(token);
                }
                else if (token.Type == TokenType.ParenRight)
                {
                    while (stack.Count > 0 && stack.Peek().Type != TokenType.ParenLeft)
                    {
                        output.Add(stack.Pop());
                    }
                    
                    // No opening paren that matches closing one
                    if (stack.Count == 0)
                    {
                        // TODO: Generate an error
                        break;
                    }
                    
                    stack.Pop(); // Pop the '('
                }
                else // The token is an operator
                {
                    // We need to check if the operator is just a dot for property access
                    if (token.Type is TokenType.Dot or TokenType.Scope)
                    {
                        output.Add(token);
                        continue;
                    }

                    BinaryOperation? op = null;

                    // Check if there is an operator on the stack
                    if (stack.Count > 0)
                    {
                        op = GetBinaryOperation(stack.Peek());
                    }

                    var otherOp = GetBinaryOperation(token);

                    if (op is BinaryOperation binary)
                    {
                        while (stack.Count > 0 && (Precedence(binary) >= Precedence(otherOp)))
                        {
                            output.Add(stack.Pop());
                        }
                    }
                    stack.Push(token);
                }
            }

            while (stack.Count > 0)
            {
                output.Add(stack.Pop());
            }

            return new TokenStream(output);
        }

        private INode BuildBinaryExpressionNode(TokenStream stream, INode? parent)
        {
            var stack = new Stack<INode>();
            var token = stream.Peek();
            while (!stream.IsEmpty())
            {
                if (token.Family == TokenFamily.Identifier)
                {
                    // We don't consume from the stream here, as the member access parser does that
                    if (IsMemberAccess(stream))
                    {
                        var memberAccess = memberAccessParser.Parse(stream, parent);
                        stack.Push(memberAccess);
                    }
                    else if (IsScopeResolution(stream))
                    {
                        var scopeResolution = scopeResolutionParser.Parse(stream, parent);
                        stack.Push(scopeResolution);
                    }
                    else
                    {
                        // We need to check if the identifier is a function call
                        if (IsFunctionCall(stream))
                        {
                            var funcCall = funcCallParser.Parse(stream, parent);
                            stack.Push(funcCall);
                        }
                        else
                        {
                            var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                            var identifierNode = new IdentifierNode(identifier.Value, parent);
                            Utils.SetMeta(identifierNode, identifier);
                            stack.Push(identifierNode);
                        }
                    }
                }
                else if (token.Family == TokenFamily.Literal)
                {
                    LiteralType type = LiteralType.Unknown;
                    switch (token.Type)
                    {
                        case TokenType.Integer:
                            type = LiteralType.Integer;
                            break;
                        case TokenType.Float:
                            type = LiteralType.Float;
                            break;
                        case TokenType.String:
                            type = LiteralType.String;
                            break;
                        case TokenType.Boolean:
                            type = LiteralType.Boolean;
                            break;
                        case TokenType.NullLiteral:
                            type = LiteralType.Null;
                            break;
                    }
                    var literal = new LiteralNode(token.Value, type);

                    Utils.SetMeta(literal, token);

                    stream.Consume();

                    stack.Push(literal);
                }
                else // The token is an operator
                {
                    var right = stack.Pop();
                    var left = stack.Pop();

                    var operation = GetBinaryOperation(token);
                    var node = new BinaryExpressionNode((IExpressionNode)left, (IExpressionNode)right, operation, null, parent);
                    left.Parent = node;
                    right.Parent = node;

                    Utils.SetMeta(node, left, right);

                    stack.Push(node);

                    stream.Consume();
                }

                if (stream.IsEmpty())
                {
                    break;
                }

                token = stream.Peek();
            }

            var upperMost = stack.Pop();
            upperMost.Parent = parent;

            return upperMost;
        }
        
        private ExpressionState NextState(ExpressionState currentState, Token token)
        {
            if (token.Type == TokenType.Linebreak)
            {
                // If linebreak is not following operator, end expression
                if (currentState is ExpressionState.Operator or ExpressionState.Any)
                {
                    return ExpressionState.Skip;
                }
                
                return ExpressionState.Finish;
            }
            switch (currentState)
            {
                case ExpressionState.Any:
                {
                    if (IsBinaryOperator(token))
                    {
                        return ExpressionState.Operator;
                    }

                    if (token.Type is TokenType.Dot)
                    {
                        return ExpressionState.MemberAccess;
                    }

                    if (token.Type is TokenType.Scope)
                    {
                        return ExpressionState.ScopeResolution;
                    }

                    if (token.Family is TokenFamily.Literal || token.Type is TokenType.Identifier)
                    {
                        return ExpressionState.Operand;
                    }

                    if (token.Type is TokenType.ParenLeft)
                    {
                        return ExpressionState.StartGroup;
                    }

                    return ExpressionState.Invalid;
                }
                case ExpressionState.Operand:
                {
                    if (token.Type is TokenType.ParenLeft)
                    {
                        return ExpressionState.StartGroup;
                    }

                    if (token.Type is TokenType.ParenRight)
                    {
                        return ExpressionState.EndGroup;
                    }

                    if (token.Type is TokenType.Dot)
                    {
                        return ExpressionState.MemberAccess;
                    }

                    if (token.Type is TokenType.Scope)
                    {
                        return ExpressionState.ScopeResolution;
                    }

                    if (IsBinaryOperator(token))
                    {
                        return ExpressionState.Operator;
                    }
                    
                    if (token.Type is TokenType.Colon)
                    {
                        return ExpressionState.Param;
                    }
                    
                    if (token.Family is TokenFamily.Operator)
                    {
                        // Any operator that is NOT a binaru or unary operator ends the expression (=, +=, etc.)
                        return ExpressionState.Finish;
                    }
                    
                    return ExpressionState.Invalid;
                }
                case ExpressionState.Operator:
                {
                    if (token.Type is TokenType.ParenLeft)
                    {
                        return ExpressionState.StartGroup;
                    }

                    if (token.Type is TokenType.Identifier || token.Family is TokenFamily.Literal)
                    {
                        return ExpressionState.Operand;
                    }

                    return ExpressionState.Invalid;
                }

                case ExpressionState.StartGroup:
                {
                    if (token.Type is TokenType.ParenLeft)
                    {
                        return ExpressionState.StartGroup;
                    }

                    if (token.Type is TokenType.Identifier || token.Family is TokenFamily.Literal)
                    {
                        return ExpressionState.Operand;
                    }

                    if (token.Type is TokenType.ParenRight)
                    {
                        return ExpressionState.EndGroup;
                    }

                    if (IsBinaryOperator(token))
                    {
                        return ExpressionState.Operator;
                    }
                    
                    return ExpressionState.Invalid;
                }
                case ExpressionState.EndGroup:
                {
                    if (token.Type is TokenType.ParenRight)
                    {
                        return ExpressionState.EndGroup;
                    }

                    if (token.Type is TokenType.Identifier || token.Family is TokenFamily.Literal)
                    {
                        return ExpressionState.Operand;
                    }

                    if (token.Type is TokenType.Dot)
                    {
                        return ExpressionState.MemberAccess;
                    }
                    
                    return ExpressionState.Invalid;
                }
                case ExpressionState.MemberAccess:
                {
                    if (token.Type is TokenType.Identifier)
                    {
                        return ExpressionState.Operand;
                    }

                    return ExpressionState.Invalid;
                }
                case ExpressionState.ScopeResolution:
                {
                    if (token.Type is TokenType.Identifier)
                    {
                        return ExpressionState.Operand;
                    }
                    
                    return ExpressionState.Invalid;
                }
                case ExpressionState.Param:
                    if (token.Type is TokenType.Identifier)
                    {
                        return ExpressionState.Operand;
                    }

                    if (token.Type is TokenType.Colon)
                    {
                        return ExpressionState.Param;
                    }
                    
                    return ExpressionState.Invalid;
            }

            return ExpressionState.Invalid;
        }
    }
}
