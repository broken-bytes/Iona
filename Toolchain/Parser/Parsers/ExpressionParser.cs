//|--- ExpressionParser.cs -------------------------------------|
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
            Param,
            ParamNext
        }
        
        FuncCallParser funcCallParser;
        MemberAccessParser memberAccessParser;
        ScopeResolutionParser scopeResolutionParser;
        TypeParser typeParser;
        private readonly IErrorCollector errorCollector;
        private readonly ILexer lexer;

        internal ExpressionParser(
            FuncCallParser funcCallParser,
            MemberAccessParser memberAccessParser,
            ScopeResolutionParser scopeResolutionParser,
            TypeParser typeParser,
            ILexer lexer,
            IErrorCollector errorCollector
        )
        {
            this.funcCallParser = funcCallParser;
            this.memberAccessParser = memberAccessParser;
            this.scopeResolutionParser = scopeResolutionParser;
            this.typeParser = typeParser;
            this.lexer = lexer;
            this.errorCollector = errorCollector;
        }

        public IExpressionNode Parse(TokenStream stream, INode? parent)
        {
            return ParseExpression(stream, parent);
        }

        public bool IsExpression(TokenStream stream)
        {
            // Expressions start with an identifier, paren or a literal
            var token = stream.Peek();

            if (
                token.Family is TokenFamily.Literal ||
                token.Family is TokenFamily.Identifier ||
                token.Type is TokenType.ParenLeft ||
                token.Type is TokenType.Self ||
                token.Type is TokenType.Super ||
                token.Type is TokenType.Await
            )
            {
                return true;
            }

            // Some operators may also start an expression
            if (token.Family is TokenFamily.Operator)
            {
                switch (token.Type)
                {
                    case TokenType.Increment:
                        return true;
                    case TokenType.Decrement:
                        return true;
                    case TokenType.Not:
                        return true;
                }
            }

            return false;
        }

        // ------------------- Helper methods -------------------
        private IExpressionNode? ParseExpression(TokenStream stream, INode? parent)
        {
            // Handle 'await' as a unary prefix — consume it and wrap the inner expression
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Await)
            {
                var awaitToken = stream.Consume(TokenType.Await, TokenFamily.Keyword);
                var innerExpr = ParseExpression(stream, parent);
                var awaitNode = new AwaitExpressionNode((IExpressionNode)innerExpr!, parent);
                awaitNode.Meta = new Shared.Metadata
                {
                    File = awaitToken.File,
                    LineStart = awaitToken.Line,
                    LineEnd = innerExpr?.Meta.LineEnd ?? awaitToken.Line,
                    ColumnStart = awaitToken.ColumnStart,
                    ColumnEnd = innerExpr?.Meta.ColumnEnd ?? awaitToken.ColumnEnd
                };
                if (innerExpr != null) innerExpr.Parent = awaitNode;
                return awaitNode;
            }

            var tokens = new List<Token>();
            var token = stream.Peek();
            
            // Track if the last token was an operator. Only `(`, `identifier`, `literal` may follow after an operator 
            // Likewise, if the last token was not an operator, we end the expression if something other than an operator occurs 
            var nextState = NextState(ExpressionState.Any, token);
            while (!stream.IsEmpty())
            {
                if (nextState is ExpressionState.Finish or ExpressionState.Invalid)
                {
                    // Array access: when state machine stops at `[`, collect bracket-delimited tokens
                    if (nextState == ExpressionState.Invalid && token.Type == TokenType.BracketLeft)
                    {
                        tokens.Add(token);
                        stream.Consume();
                        int bracketDepth = 1;
                        while (!stream.IsEmpty() && bracketDepth > 0)
                        {
                            token = stream.Peek();
                            tokens.Add(token);
                            stream.Consume();
                            if (token.Type == TokenType.BracketLeft) bracketDepth++;
                            if (token.Type == TokenType.BracketRight) bracketDepth--;
                        }
                        if (stream.IsEmpty()) break;
                        token = stream.Peek();
                        nextState = NextState(ExpressionState.Operand, token);
                        continue;
                    }
                    break;
                }
                switch (nextState)
                {
                    case ExpressionState.Skip:
                        stream.Consume();
                        break;
                    default:
                        tokens.Add(token);
                        break;
                }

                stream.Consume();
                token = stream.Peek();

                if (stream.IsEmpty())
                {
                    break;
                }

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
                case TokenType.ArrowRight:
                    return BinaryOperation.GreaterThan;
                case TokenType.ArrowLeft:
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
                case TokenType.UnaryMinus:
                    return UnaryOperation.Negation;
                case TokenType.UnaryNot:
                case TokenType.Not:
                    return UnaryOperation.Not;
                default:
                    return UnaryOperation.Noop;
            }
        }

        private int TokenPrecedence(Token token)
        {
            switch (token.Type)
            {
                case TokenType.UnaryMinus:
                case TokenType.UnaryNot:
                    return 4;
                case TokenType.Multiply:
                case TokenType.Divide:
                    return 3;
                case TokenType.Plus:
                case TokenType.Minus:
                    return 2;
                default:
                    return 1;
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

            int funcNestingLevel = 0;

            bool hasGenericClause = false;

            Token? prev = null;

            while (!stream.IsEmpty())
            {
                var token = stream.Consume();

                // A `-`/`!` in operand position is a prefix unary operator (vs binary `-` or
                // postfix force-unwrap `x!`). Re-tag it so tree building pops a single operand.
                var afterOperand = prev.HasValue && (
                    prev.Value.Type is TokenType.Identifier or TokenType.Self or TokenType.Super
                        or TokenType.ParenRight or TokenType.BracketRight
                    || prev.Value.Family is TokenFamily.Literal);
                prev = token;

                if (!afterOperand && token.Type is TokenType.Minus or TokenType.Not)
                {
                    var unary = token;
                    unary.Type = token.Type == TokenType.Minus ? TokenType.UnaryMinus : TokenType.UnaryNot;
                    stack.Push(unary);
                    prev = unary;
                    continue;
                }

                if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super or TokenType.Dot or TokenType.SoftUnwrap or TokenType.Comma|| token.Family is TokenFamily.Literal)
                {
                    output.Add(token);
                    continue;
                }
                
                // TODO: Function calls dont work nested right now as the first nested call will trigger the paren right for the outer one
                if (token.Type == TokenType.ParenLeft)
                {
                    // When we have an identifier followed by a parenthesis without any operator
                    // we have a function call and parse until the closing parenthesis
                    if (output.Any() && (output[^1].Type is TokenType.Identifier or TokenType.Self or TokenType.Super || hasGenericClause))
                    {
                        funcNestingLevel++;
                        hasGenericClause = false;
                        output.Add(token);

                        token = stream.Peek();
                    }
                    else
                    {
                        stack.Push(token);
                    }
                    continue;
                }
                
                if (token.Type == TokenType.ParenRight)
                {
                    if (funcNestingLevel > 0)
                    {
                        funcNestingLevel--;
                        
                        output.Add(token);

                        token = stream.Peek();
                        continue;
                    }
                    
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
                    continue;
                }
                if (token.Type is TokenType.ArrowLeft)
                {
                    var genericClause = stream
                        .SkipWhile(t => t.Family is not TokenFamily.Operator and TokenFamily.Grouping)
                        .TakeWhile(t => t.Type is not TokenType.ParenRight)
                        .ToList();
                    
                    // Check if between `<` and `>` come only identifiers or commas
                    if (genericClause.Last().Type is TokenType.ParenLeft &&
                        genericClause.SkipLast(1).Last().Type is TokenType.ArrowRight)
                    {
                        hasGenericClause = true;
                        
                        output.Add(token);

                        token = stream.Peek();
                        
                        while (token.Type is not TokenType.ArrowRight)
                        {
                            output.Add(token);
                            token = stream.Consume();
                            
                            token = stream.Peek();
                        }

                        stream.Consume();
                        output.Add(token);
                        
                        continue;
                    }
                }

                // Force unwrap (!) passes through to tree building
                if (token.Type is TokenType.Not or TokenType.HardUnwrap)
                {
                    output.Add(token);
                    continue;
                }

                // We need to check if the operator is just a dot for property access
                if (token.Type is TokenType.Dot or TokenType.Scope)
                {
                    output.Add(token);
                    continue;
                }

                // Array access brackets pass through to tree building
                if (token.Type is TokenType.BracketLeft or TokenType.BracketRight)
                {
                    output.Add(token);
                    continue;
                }

                // These only occur within a function call so we always add them
                if (token.Type is TokenType.Colon or TokenType.Comma)
                {
                    output.Add(token);
                    continue;
                }

                // Pop operators of higher-or-equal precedence (left-associative); unary
                // operators on the stack are highest precedence and get flushed first.
                while (stack.Count > 0
                       && stack.Peek().Type != TokenType.ParenLeft
                       && TokenPrecedence(stack.Peek()) >= TokenPrecedence(token))
                {
                    output.Add(stack.Pop());
                }

                stack.Push(token);
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
                if (token.Type is TokenType.BracketLeft)
                {
                    // Array access: pop array operand, parse index sub-expression until `]`
                    stream.Consume(); // consume '['
                    var arrayNode = (IExpressionNode)stack.Pop();

                    // Collect tokens until matching ']'
                    var indexTokens = new List<Token>();
                    int depth = 1;
                    while (!stream.IsEmpty() && depth > 0)
                    {
                        var t = stream.Peek();
                        if (t.Type == TokenType.BracketLeft) depth++;
                        if (t.Type == TokenType.BracketRight) depth--;
                        if (depth > 0)
                        {
                            indexTokens.Add(t);
                        }
                        stream.Consume();
                    }

                    var indexStream = new TokenStream(indexTokens);
                    var indexExpr = (IExpressionNode)BuildBinaryExpressionNode(indexStream, parent);

                    var accessNode = new ArrayAccessNode(arrayNode, indexExpr, parent);
                    arrayNode.Parent = accessNode;
                    indexExpr.Parent = accessNode;
                    stack.Push(accessNode);
                }
                else if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super)
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
                        case TokenType.Double:
                            type = LiteralType.Double;
                            break;
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
                    IExpressionNode literal;
                    if (token.Type == TokenType.String && ContainsInterpolation(token.Value))
                    {
                        literal = BuildInterpolatedString(token, parent);
                    }
                    else
                    {
                        var lit = new LiteralNode(token.Value, type);
                        Utils.SetMeta(lit, token);
                        literal = lit;
                    }

                    stream.Consume();

                    stack.Push(literal);
                }
                else if (token.Type is TokenType.UnaryMinus or TokenType.UnaryNot)
                {
                    if (!stack.Any())
                    {
                        stream.Consume();
                        continue;
                    }

                    var operand = stack.Pop();
                    var unaryNode = new UnaryExpressionNode(operand, GetUnaryOperation(token), null, parent);
                    operand.Parent = unaryNode;
                    Utils.SetMeta(unaryNode, token);
                    stack.Push(unaryNode);
                    stream.Consume();
                }
                else // The token is an operator
                {
                    if (!stack.Any())
                    {
                        var meta = new Metadata
                        {
                            ColumnStart = token.ColumnStart,
                            ColumnEnd = token.ColumnEnd,
                            LineStart = token.Line,
                            LineEnd = token.Line,
                            File = token.File
                        };
                        var syntaxError = CompilerErrorFactory.SyntaxError($"Unexpected token `{token.Value}` in expression", meta);
                        errorCollector.Collect(syntaxError);
                        stream.Panic(TokenType.Linebreak);
                        return null;
                    }
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

                // Check for force unwrap postfix (!) and optional chaining (?.)
                while (!stream.IsEmpty() && stack.Any())
                {
                    var next = stream.Peek();
                    if (next.Type is TokenType.Not or TokenType.HardUnwrap)
                    {
                        stream.Consume();
                        var expr = (IExpressionNode)stack.Pop();
                        var forceUnwrap = new ForceUnwrapNode(expr, parent);
                        expr.Parent = forceUnwrap;
                        forceUnwrap.Meta = expr.Meta;
                        stack.Push(forceUnwrap);
                    }
                    else if (next.Type == TokenType.SoftUnwrap && !stream.IsEmpty())
                    {
                        // Optional chaining: consume ? then expect .member
                        stream.Consume(); // consume ?
                        if (stream.IsEmpty() || stream.Peek().Type != TokenType.Dot) break;
                        stream.Consume(); // consume .
                        if (stream.IsEmpty()) break;
                        var memberToken = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                        var obj = (IExpressionNode)stack.Pop();
                        var memberNode = new IdentifierNode(memberToken.Value, parent);
                        Utils.SetMeta(memberNode, memberToken);
                        var propAccess = new PropAccessNode(obj, memberNode, parent) { IsOptionalChain = true };
                        obj.Parent = propAccess;
                        memberNode.Parent = propAccess;
                        propAccess.Meta = obj.Meta;
                        stack.Push(propAccess);
                    }
                    else if (next.Type == TokenType.Dot)
                    {
                        // Regular member access on result (e.g., foo!.name)
                        stream.Consume(); // consume .
                        if (stream.IsEmpty()) break;
                        var memberToken = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                        var obj = (IExpressionNode)stack.Pop();
                        var memberNode = new IdentifierNode(memberToken.Value, parent);
                        Utils.SetMeta(memberNode, memberToken);
                        var propAccess = new PropAccessNode(obj, memberNode, parent);
                        obj.Parent = propAccess;
                        memberNode.Parent = propAccess;
                        propAccess.Meta = obj.Meta;
                        stack.Push(propAccess);
                    }
                    else
                    {
                        break;
                    }
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

            // Postfix operators continue the expression after any value-producing state, so a
            // variable, property, and function-call result all unwrap (`!`) and chain (`.`/`?.`)
            // identically. (`!` is lexed as Not standalone, HardUnwrap before `.`.)
            if (currentState is ExpressionState.Operand or ExpressionState.EndGroup)
            {
                if (token.Type is TokenType.Not or TokenType.HardUnwrap)
                {
                    return ExpressionState.Operand;
                }

                if (token.Type is TokenType.Dot or TokenType.SoftUnwrap)
                {
                    return ExpressionState.MemberAccess;
                }
            }

            // Prefix unary operators (`-x`, `!x`) appear where an operand is expected. Keep
            // collecting (an operand must follow) so the value isn't dropped at the boundary.
            if (token.Type is TokenType.Minus or TokenType.Not
                && currentState is ExpressionState.Any or ExpressionState.Operator
                    or ExpressionState.StartGroup or ExpressionState.ParamNext or ExpressionState.Param)
            {
                return ExpressionState.Operator;
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

                    if (token.Family is TokenFamily.Literal || token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super)
                    {
                        return ExpressionState.Operand;
                    }

                    if (token.Type is TokenType.Dot)
                    {
                        return ExpressionState.MemberAccess;
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
                        // Any operator that is NOT a binary or unary operator ends the expression (=, +=, etc.)
                        return ExpressionState.Finish;
                    }

                    if (token.Type is TokenType.Comma)
                    {
                        return ExpressionState.ParamNext;
                    }

                    return ExpressionState.Invalid;
                }
                case ExpressionState.ParamNext:
                {
                    if (token.Type is TokenType.ParenLeft)
                    {
                        return ExpressionState.StartGroup;
                    }

                    if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super || token.Family is TokenFamily.Literal)
                    {
                        return ExpressionState.Operand;
                    }
                    
                    if (token.Type is TokenType.Dot)
                    {
                        return ExpressionState.MemberAccess;
                    }

                    return ExpressionState.Invalid;
                }
                case ExpressionState.Operator:
                {
                    if (token.Type is TokenType.ParenLeft)
                    {
                        return ExpressionState.StartGroup;
                    }

                    if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super || token.Family is TokenFamily.Literal)
                    {
                        return ExpressionState.Operand;
                    }
                    
                    if (token.Type is TokenType.Dot)
                    {
                        return ExpressionState.MemberAccess;
                    }

                    return ExpressionState.Invalid;
                }

                case ExpressionState.StartGroup:
                {
                    if (token.Type is TokenType.ParenLeft)
                    {
                        return ExpressionState.StartGroup;
                    }

                    if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super || token.Family is TokenFamily.Literal)
                    {
                        return ExpressionState.Operand;
                    }
                    
                    if (token.Type is TokenType.Dot)
                    {
                        return ExpressionState.MemberAccess;
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

                    if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super || token.Family is TokenFamily.Literal)
                    {
                        return ExpressionState.Operand;
                    }

                    if (token.Type is TokenType.Comma)
                    {
                        return ExpressionState.ParamNext;
                    }
                    
                    return ExpressionState.Invalid;
                }
                case ExpressionState.MemberAccess:
                {
                    if (token.Type is TokenType.Identifier)
                    {
                        return ExpressionState.Operand;
                    }

                    // After `?` in optional chaining, expect `.`
                    if (token.Type is TokenType.Dot)
                    {
                        return ExpressionState.MemberAccess;
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
                {
                    if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super || token.Family is TokenFamily.Literal)
                    {
                        return ExpressionState.Operand;
                    }

                    if (token.Type is TokenType.Dot)
                    {
                        return ExpressionState.MemberAccess;
                    }

                    if (token.Type is TokenType.Colon)
                    {
                        return ExpressionState.Param;
                    }

                    return ExpressionState.Invalid;
                }
            }

            return ExpressionState.Invalid;
        }

        // The raw token value includes the surrounding quotes. `\$` is an escape, not a trigger.
        private static bool ContainsInterpolation(string raw)
        {
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] == '\\' && i + 1 < raw.Length)
                {
                    i++;
                    continue;
                }
                if (raw[i] == '$' && i + 1 < raw.Length && raw[i + 1] == '{')
                {
                    return true;
                }
            }
            return false;
        }


        // Split a raw `"... ${expr} ..."` token into alternating literal-text and parsed-expression
        // segments. Expression segments are re-lexed via the injected lexer and parsed through this
        // same ExpressionParser, so nested member access and calls inside `${...}` work.
        private InterpolatedStringNode BuildInterpolatedString(Token token, INode? parent)
        {
            var node = new InterpolatedStringNode(parent);
            Utils.SetMeta(node, token);

            var raw = token.Value;
            // Strip surrounding quotes.
            if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
            {
                raw = raw.Substring(1, raw.Length - 2);
            }

            var textBuilder = new System.Text.StringBuilder();
            int i = 0;
            while (i < raw.Length)
            {
                // Escapes survive into the inner expression untouched (the sub-lexer handles them
                // there), but for the text segments we collapse `\$`/`\\`/`\"` here.
                if (raw[i] == '\\' && i + 1 < raw.Length)
                {
                    textBuilder.Append(raw[i]);
                    textBuilder.Append(raw[i + 1]);
                    i += 2;
                    continue;
                }

                if (i + 1 < raw.Length && raw[i] == '$' && raw[i + 1] == '{')
                {
                    if (textBuilder.Length > 0)
                    {
                        var litText = new LiteralNode($"\"{textBuilder}\"", LiteralType.String, node);
                        Utils.SetMeta(litText, token);
                        node.Segments.Add(litText);
                        textBuilder.Clear();
                    }

                    int start = i + 2;
                    int j = ScanInterpolationEnd(raw, start);
                    if (j < 0)
                    {
                        textBuilder.Append(raw, i, raw.Length - i);
                        i = raw.Length;
                        continue;
                    }

                    var exprText = raw.Substring(start, j - start);
                    var innerExpr = ParseInterpolationSegment(exprText, token, node);
                    if (innerExpr != null)
                    {
                        node.Segments.Add(innerExpr);
                    }
                    i = j + 1;
                    continue;
                }

                textBuilder.Append(raw[i]);
                i++;
            }

            if (textBuilder.Length > 0)
            {
                var litText = new LiteralNode($"\"{textBuilder}\"", LiteralType.String, node);
                Utils.SetMeta(litText, token);
                node.Segments.Add(litText);
            }

            return node;
        }

        // Mirrors the lexer's mutually-recursive scanner so an interpolation body containing
        // a string literal (which may itself contain `${...}`) doesn't close at a `}` that
        // happens to live inside that nested string.
        private static int ScanInterpolationEnd(string s, int start)
        {
            int i = start;
            int depth = 1;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length) { i += 2; continue; }
                if (c == '"')
                {
                    int end = ScanNestedStringEnd(s, i + 1);
                    if (end < 0) { return -1; }
                    i = end + 1;
                    continue;
                }
                if (c == '{') { depth++; }
                else if (c == '}') { depth--; if (depth == 0) { return i; } }
                i++;
            }
            return -1;
        }

        private static int ScanNestedStringEnd(string s, int start)
        {
            int i = start;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length) { i += 2; continue; }
                if (c == '"') { return i; }
                if (c == '$' && i + 1 < s.Length && s[i + 1] == '{')
                {
                    int after = ScanInterpolationEnd(s, i + 2);
                    if (after < 0) { return -1; }
                    i = after + 1;
                    continue;
                }
                i++;
            }
            return -1;
        }

        private IExpressionNode? ParseInterpolationSegment(string exprText, Token sourceToken, INode parent)
        {
            var tokens = lexer.Tokenize(exprText, sourceToken.File);
            if (!IsExpression(tokens))
            {
                return null;
            }
            return ParseExpression(tokens, parent);
        }
    }
}
