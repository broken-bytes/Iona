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
                    case TokenType.Scope:
                        // `::name` — function reference. The infix form (`Console::writeLine`)
                        // is handled inside the operand path, not at expression start.
                        return true;
                }
            }

            return false;
        }

        // ------------------- Helper methods -------------------
        private IExpressionNode? ParseExpression(TokenStream stream, INode? parent)
        {
            // Rust-style lambda `|args| body` / `|args| -> R { … }` at operand position.
            // The infix `|` (bitwise OR) never appears at the start of an expression, so this
            // is unambiguous here.
            if (!stream.IsEmpty()
                && (stream.Peek().Type == TokenType.Or
                    || (stream.Peek().Type == TokenType.BitOr && LooksLikeLambdaStart(stream))))
            {
                return ParseLambda(stream, parent);
            }

            // Rust-style IIFE: `(|args| -> body)(call-args)`. Detected by `(` immediately
            // followed by a `|`-led lambda start. We parse the lambda, consume the closing `)`,
            // then if a `(` follows, parse the call args and wrap in InvokeExpressionNode.
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.ParenLeft
                && LooksLikeIIFEStart(stream))
            {
                return ParseIIFE(stream, parent);
            }

            // `typeof(TypeName)` — `typeof` is not a reserved keyword in Iona, so we recognise
            // it as an identifier with a `(` follow-up. Yields a literal-shaped TypeOf node the
            // attribute folder turns into a `System.Type` CustomAttributeArgument.
            if (!stream.IsEmpty()
                && stream.Peek().Type == TokenType.Identifier
                && stream.Peek().Value == "typeof"
                && stream.Count() >= 2
                && stream.Peek(2)[1].Type == TokenType.ParenLeft)
            {
                stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                stream.Consume(TokenType.ParenLeft, TokenFamily.Operator);
                var operand = typeParser.Parse(stream, parent);
                stream.Consume(TokenType.ParenRight, TokenFamily.Operator);
                return new TypeOfExpressionNode(operand, parent);
            }

            // Collection literal in operand position: `[1, 2, 3]` is a list, `["a": 1]` is a map,
            // `[]` empty list, `[:]` empty map. Distinguishes from `xs[0]` array access because
            // that case only happens *after* an operand has been parsed (the state machine
            // routes it through a different branch).
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.BracketLeft
                && LooksLikeCollectionLiteralStart(stream))
            {
                return ParseCollectionLiteral(stream, parent);
            }

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
                    // Lambda block: state machine doesn't know `|`, slurp the whole `|…| (-> R)? body`
                    // as one opaque run so the FuncCallParser sees it as a single arg value.
                    if (token.Type == TokenType.BitOr && LooksLikeLambdaStart(stream))
                    {
                        SlurpLambdaTokens(stream, tokens);
                        if (stream.IsEmpty()) { break; }
                        token = stream.Peek();
                        nextState = NextState(ExpressionState.Operand, token);
                        continue;
                    }

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

                // Inside a function-call's parens, FuncCallParser will re-parse each argument
                // value through ExpressionParser.Parse — so operator-precedence reordering at
                // this level would just scramble the per-arg sub-streams. Pass tokens through.
                if (funcNestingLevel > 0)
                {
                    if (token.Type == TokenType.ParenRight)
                    {
                        funcNestingLevel--;
                        output.Add(token);
                        prev = token;
                        continue;
                    }
                    if (token.Type == TokenType.ParenLeft)
                    {
                        // Nested call: enter another funcNesting level.
                        if (output.Count > 0
                            && output[^1].Type is TokenType.Identifier or TokenType.FunctionRef)
                        {
                            funcNestingLevel++;
                        }
                    }
                    output.Add(token);
                    prev = token;
                    continue;
                }

                // A `-`/`!` in operand position is a prefix unary operator (vs binary `-` or
                // postfix force-unwrap `x!`). Re-tag it so tree building pops a single operand.
                var afterOperand = prev.HasValue && (
                    prev.Value.Type is TokenType.Identifier or TokenType.Self or TokenType.Super or TokenType.FunctionRef or TokenType.Scope
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

                // `::name` in operand position is a function reference, not infix scope
                // resolution. Collapse the pair into a single FunctionRef marker token whose
                // Value carries the name; the tree builder turns it into a FunctionReferenceNode.
                if (!afterOperand && token.Type == TokenType.Scope && !stream.IsEmpty()
                    && stream.Peek().Type == TokenType.Identifier)
                {
                    var nameTok = stream.Consume();
                    var refTok = token;
                    refTok.Type = TokenType.FunctionRef;
                    refTok.Value = nameTok.Value;
                    refTok.ColumnEnd = nameTok.ColumnEnd;
                    output.Add(refTok);
                    prev = refTok;
                    continue;
                }

                if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super or TokenType.FunctionRef or TokenType.Scope or TokenType.Dot or TokenType.SoftUnwrap or TokenType.Comma|| token.Family is TokenFamily.Literal)
                {
                    output.Add(token);
                    continue;
                }
                
                // TODO: Function calls dont work nested right now as the first nested call will trigger the paren right for the outer one
                if (token.Type == TokenType.ParenLeft)
                {
                    // When we have an identifier followed by a parenthesis without any operator
                    // we have a function call and parse until the closing parenthesis
                    if (output.Any() && (output[^1].Type is TokenType.Identifier or TokenType.Self or TokenType.Super or TokenType.FunctionRef or TokenType.Scope || hasGenericClause))
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
                    // `Name<Type1, Type2>(...)` call site: scan the lookahead for a `>`
                    // followed immediately by `(` while the in-between is only identifiers,
                    // dots, and commas. If found, this `<` isn't a less-than operator; it's a
                    // generic-arg opener.
                    var lookahead = stream.ToList();
                    bool isGenericCall = false;
                    int closingIdx = -1;
                    for (int i = 0; i < lookahead.Count; i++)
                    {
                        var lk = lookahead[i].Type;
                        if (lk is TokenType.Identifier or TokenType.Dot or TokenType.Comma)
                        {
                            continue;
                        }
                        if (lk == TokenType.ArrowRight)
                        {
                            closingIdx = i;
                            if (i + 1 < lookahead.Count && lookahead[i + 1].Type == TokenType.ParenLeft)
                            {
                                isGenericCall = true;
                            }
                        }
                        break;
                    }

                    if (isGenericCall)
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
                else if (token.Type == TokenType.FunctionRef)
                {
                    var refNode = new FunctionReferenceNode(token.Value, null, parent);
                    Utils.SetMeta(refNode, token);
                    stream.Consume();
                    stack.Push(refNode);
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

                    if (token.Family is TokenFamily.Literal || token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super or TokenType.FunctionRef or TokenType.Scope)
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

                    if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super or TokenType.FunctionRef or TokenType.Scope || token.Family is TokenFamily.Literal)
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

                    if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super or TokenType.FunctionRef or TokenType.Scope || token.Family is TokenFamily.Literal)
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

                    if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super or TokenType.FunctionRef or TokenType.Scope || token.Family is TokenFamily.Literal)
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

                    if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super or TokenType.FunctionRef or TokenType.Scope || token.Family is TokenFamily.Literal)
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
                    // `::` in operand position starts a function-reference; treat it like the
                    // ScopeResolution start so the following identifier becomes the operand.
                    if (token.Type is TokenType.Scope)
                    {
                        return ExpressionState.ScopeResolution;
                    }

                    if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Super or TokenType.FunctionRef || token.Family is TokenFamily.Literal)
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

        // Slurp the entire lambda — `|params|`, optional `-> RetType`, then either a `{ block }`
        // (with brace-balancing) or a bare expression up to a comma/paren at top-level. Pushes
        // every consumed token into `tokens` so a later ParseLambda call sees them.
        private static void SlurpLambdaTokens(TokenStream stream, List<Token> tokens)
        {
            // Opening `|`
            tokens.Add(stream.Peek()); stream.Consume();

            // Params until matching `|`. A comma inside the param list is *not* a top-level
            // arg separator — keep going until we see the closing pipe.
            while (!stream.IsEmpty() && stream.Peek().Type != TokenType.BitOr)
            {
                tokens.Add(stream.Peek()); stream.Consume();
            }
            if (stream.IsEmpty()) { return; }
            tokens.Add(stream.Peek()); stream.Consume(); // closing `|`

            // Optional `-> ReturnType`
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Arrow)
            {
                tokens.Add(stream.Peek()); stream.Consume();
                // ReturnType token(s) until the body starts. The type may be a single identifier
                // or include `.`, `<>` etc.; we conservatively stop at `{` or any token that
                // can't start a type.
                while (!stream.IsEmpty())
                {
                    var t = stream.Peek();
                    if (t.Type is TokenType.CurlyLeft or TokenType.Comma or TokenType.ParenRight)
                    {
                        break;
                    }
                    tokens.Add(t); stream.Consume();
                }
            }

            // Body
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.CurlyLeft)
            {
                int depth = 0;
                while (!stream.IsEmpty())
                {
                    var t = stream.Peek();
                    tokens.Add(t); stream.Consume();
                    if (t.Type == TokenType.CurlyLeft) { depth++; }
                    else if (t.Type == TokenType.CurlyRight)
                    {
                        depth--;
                        if (depth == 0) { return; }
                    }
                }
            }
            else
            {
                // Single-expression body: consume until comma/paren-right at top-level.
                int parenDepth = 0;
                while (!stream.IsEmpty())
                {
                    var t = stream.Peek();
                    if (parenDepth == 0 && (t.Type is TokenType.Comma or TokenType.ParenRight))
                    {
                        return;
                    }
                    if (t.Type == TokenType.ParenLeft) { parenDepth++; }
                    else if (t.Type == TokenType.ParenRight) { parenDepth--; }
                    tokens.Add(t); stream.Consume();
                }
            }
        }

        // After `->`, decide whether the leading tokens form a return type. Type-shape tokens
        // are followed by `{` for the body; if no `{` appears in the lookahead window before a
        // non-type token, the body is a bare expression and we should not consume a type.
        private static bool ReturnTypePrecedesBlock(TokenStream stream)
        {
            var window = stream.Peek(Math.Min(stream.Count(), 24));
            // The first token must itself plausibly start a type — Identifier (e.g. `Int32`),
            // `[` (array type), or `(` (function type). Anything else (literal, operator, `|`)
            // means we're already looking at the body.
            if (window.Count == 0) { return false; }
            var first = window[0];
            if (first.Type is not (TokenType.Identifier or TokenType.BracketLeft or TokenType.ParenLeft))
            {
                return false;
            }
            foreach (var t in window)
            {
                if (t.Type == TokenType.CurlyLeft) { return true; }
                if (t.Type is TokenType.Identifier
                    or TokenType.Dot
                    or TokenType.BracketLeft
                    or TokenType.BracketRight
                    or TokenType.ParenLeft
                    or TokenType.ParenRight
                    or TokenType.Comma
                    or TokenType.Arrow
                    or TokenType.SoftUnwrap
                    or TokenType.Not
                    or TokenType.HardUnwrap
                    or TokenType.ArrowLeft
                    or TokenType.ArrowRight)
                {
                    continue;
                }
                return false;
            }
            return false;
        }

        // Heuristic: look at tokens after the leading `|` to decide whether this is a lambda.
        // A lambda's param list is `|_|` (explicit "no params") or `identifier (: Type)?
        // (, …)?` followed by `|`. The fused `||` token (logical OR) is intentionally NOT a
        // lambda opener — Iona favours the explicit `|_|` to avoid the dual-meaning footgun.
        // Rust-style IIFE: `(`, immediately followed by a lambda-leading `|`, with the lambda
        // body fitting inside the parens. Cheap discriminator — we just need to distinguish
        // from ordinary grouping `(expr)`, where the contents start with an operand token, not
        // a pipe. False positives are impossible because `|` is never a unary-prefix operator.
        private static bool LooksLikeIIFEStart(TokenStream stream)
        {
            if (stream.Count() < 3) { return false; }
            var window = stream.Peek(2);
            return window[0].Type == TokenType.ParenLeft
                && (window[1].Type == TokenType.BitOr || window[1].Type == TokenType.Or);
        }

        // `(lambda)(arg1: …, arg2: …)`. We consume the wrapping parens, hand the lambda body
        // to ParseLambda, then if a `(` follows, parse call args into an InvokeExpressionNode.
        // If no `(` follows we just hand back the parenthesised lambda.
        private IExpressionNode ParseIIFE(TokenStream stream, INode? parent)
        {
            var openParen = stream.Consume(TokenType.ParenLeft, TokenFamily.Operator);

            // Slice tokens up to the matching `)` so the lambda body's ParseExpression sees a
            // bounded stream and doesn't keep consuming past the IIFE wrapper. Track nested
            // parens so a body containing `(x + y)` doesn't terminate early.
            var bodyTokens = new List<Token>();
            int depth = 1;
            while (!stream.IsEmpty() && depth > 0)
            {
                var t = stream.Peek();
                if (t.Type == TokenType.ParenLeft) { depth++; }
                else if (t.Type == TokenType.ParenRight)
                {
                    depth--;
                    if (depth == 0) { break; }
                }
                bodyTokens.Add(t);
                stream.Consume();
            }
            var lambda = ParseLambda(new TokenStream(bodyTokens), parent);
            stream.Consume(TokenType.ParenRight, TokenFamily.Operator);

            if (stream.IsEmpty() || stream.Peek().Type != TokenType.ParenLeft)
            {
                return lambda;
            }

            stream.Consume(TokenType.ParenLeft, TokenFamily.Operator);
            var invoke = new InvokeExpressionNode(lambda, parent);
            Utils.SetStart(invoke, openParen);
            lambda.Parent = invoke;

            // Rust-style IIFE args are positional — the lambda parameter list gives the names.
            // We accept either form here: bare `5` or `n: 5`. When bare, we synthesise the name
            // from the lambda parameter at the same index so downstream code that keys on
            // arg.Name (codegen arg ordering) still works.
            int argIndex = 0;
            while (!stream.IsEmpty() && stream.Peek().Type != TokenType.ParenRight)
            {
                string argName;
                if (stream.Count() >= 2
                    && stream.Peek().Type == TokenType.Identifier
                    && stream.Peek(2)[1].Type == TokenType.Colon)
                {
                    argName = stream.Consume(TokenType.Identifier, TokenFamily.Identifier).Value;
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);
                }
                else
                {
                    argName = argIndex < lambda.Parameters.Count
                        ? lambda.Parameters[argIndex].Name
                        : $"_arg{argIndex}";
                }
                var argExprStream = SliceUntilArgBoundary(stream);
                var argExpr = ParseExpression(argExprStream, invoke);
                invoke.Args.Add(new FuncCallArg(argName, (IExpressionNode)argExpr!));
                argIndex++;
                if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenFamily.Operator);
                }
            }
            var closeParen = stream.Consume(TokenType.ParenRight, TokenFamily.Operator);
            Utils.SetEnd(invoke, closeParen);

            return invoke;
        }

        // Pull tokens for a single call-arg expression — stop at the outer `,` or `)`. Tracks
        // nested parens/brackets/braces so commas inside nested expressions don't end the arg.
        private static TokenStream SliceUntilArgBoundary(TokenStream stream)
        {
            var tokens = new List<Token>();
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
                tokens.Add(t);
                stream.Consume();
            }
            return new TokenStream(tokens);
        }

        private static bool LooksLikeLambdaStart(TokenStream stream)
        {
            var available = Math.Min(stream.Count(), 32);
            if (available < 2) { return false; }
            var window = stream.Peek(available);
            // window[0] is the leading `|`. Scan until matching `|` while only allowing
            // identifiers, `,`, `:` and the type-name tokens.
            for (int i = 1; i < window.Count; i++)
            {
                var t = window[i];
                if (t.Type == TokenType.BitOr) { return true; }
                if (t.Type is TokenType.Identifier
                    or TokenType.Colon
                    or TokenType.Comma
                    or TokenType.Dot
                    or TokenType.SoftUnwrap
                    or TokenType.Not
                    or TokenType.HardUnwrap
                    or TokenType.BracketLeft
                    or TokenType.BracketRight
                    or TokenType.ParenLeft
                    or TokenType.ParenRight
                    or TokenType.Arrow
                    or TokenType.Linebreak)
                {
                    continue;
                }
                return false;
            }
            return false;
        }

        private LambdaNode ParseLambda(TokenStream stream, INode? parent)
        {
            var lambda = new LambdaNode(parent);

            // Rust-aligned arity spelling: `||` (lexed as a single `Or` token) is the empty
            // param list, `|_|` is one discarded parameter, `|x|` / `|x, y|` are named params.
            if (stream.Peek().Type == TokenType.Or)
            {
                var orTok = stream.Consume(TokenType.Or, TokenFamily.Operator);
                Utils.SetStart(lambda, orTok);
            }
            else
            {
                var openPipe = stream.Consume(TokenType.BitOr, TokenFamily.Operator);
                Utils.SetStart(lambda, openPipe);

                // Parse zero-or-more params until the closing `|`. `_` is an ordinary
                // parameter name now — Rust semantics: it binds, but the binding is
                // conventionally ignored in the body.
                while (stream.Peek().Type != TokenType.BitOr)
                {
                    var paramName = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                    TypeReferenceNode? paramType = null;
                    if (stream.Peek().Type == TokenType.Colon)
                    {
                        stream.Consume(TokenType.Colon, TokenFamily.Operator);
                        paramType = typeParser.Parse(stream, lambda);
                    }
                    // Untyped lambda param: placeholder TypeNode the resolver replaces with the
                    // inferred type from the call-site function-type slot.
                    paramType ??= new TypeReferenceNode("_inferred_", lambda)
                    {
                        FullyQualifiedName = "_inferred_"
                    };
                    lambda.Parameters.Add(new ParameterNode(paramName.Value, paramType, lambda));

                    if (stream.Peek().Type == TokenType.Comma)
                    {
                        stream.Consume(TokenType.Comma, TokenFamily.Operator);
                    }
                }
                stream.Consume(TokenType.BitOr, TokenFamily.Operator);
            }

            // `->` is an optional "here-comes-the-body" marker. After consuming it, the next
            // tokens are EITHER a return-type-then-block (`-> Int32 { … }`) OR the body itself
            // (`-> "Foo"`, `-> { … }`). Disambiguate by peeking for a `{` ahead through tokens
            // that could form a type — if we find one, treat the leading run as the return type.
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Arrow)
            {
                stream.Consume(TokenType.Arrow, TokenFamily.Operator);
                if (!stream.IsEmpty()
                    && stream.Peek().Type != TokenType.CurlyLeft
                    && ReturnTypePrecedesBlock(stream))
                {
                    lambda.ReturnType = typeParser.Parse(stream, lambda);
                }
            }

            // Body: `{ … }` block, or a bare expression (implicit-return).
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.CurlyLeft)
            {
                var blockParser = new BlockParser();
                blockParser.Setup(this, memberAccessParser, statementParserFor(lambda));
                lambda.Body = blockParser.Parse(stream, lambda);
            }
            else
            {
                lambda.Body = ParseExpression(stream, lambda);
            }

            return lambda;
        }

        // BlockParser needs a StatementParser. The expression parser doesn't hold one directly;
        // wire it lazily via the field set by ParserFactory through a setter (added there).
        private StatementParser statementParserFor(INode? _) => _statementParser
            ?? throw new InvalidOperationException("StatementParser not wired into ExpressionParser");

        private StatementParser? _statementParser;
        internal void SetStatementParser(StatementParser sp) { _statementParser = sp; }

        // Collection literals only fire when `[` starts an expression — never `xs[0]` access.
        // We don't need fancy lookahead here; the operand-position guard at the call site is
        // enough disambiguation. We still verify the closing `]` exists in the window so a
        // malformed run doesn't lock us into a wrong path.
        private static bool LooksLikeCollectionLiteralStart(TokenStream stream)
        {
            var available = Math.Min(stream.Count(), 64);
            if (available < 2) { return false; }
            var window = stream.Peek(available);
            int depth = 0;
            for (int i = 0; i < window.Count; i++)
            {
                if (window[i].Type == TokenType.BracketLeft) { depth++; }
                else if (window[i].Type == TokenType.BracketRight)
                {
                    depth--;
                    if (depth == 0) { return true; }
                }
            }
            return false;
        }

        private IExpressionNode ParseCollectionLiteral(TokenStream stream, INode? parent)
        {
            var open = stream.Consume(TokenType.BracketLeft, TokenFamily.Grouping);

            // `[:]` — empty map shorthand.
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Colon)
            {
                stream.Consume(TokenType.Colon, TokenFamily.Operator);
                var endE = stream.Consume(TokenType.BracketRight, TokenFamily.Grouping);
                var emptyMap = new MapLiteralNode(parent);
                Utils.SetStart(emptyMap, open);
                Utils.SetEnd(emptyMap, endE);
                return emptyMap;
            }

            // `[]` — empty list.
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.BracketRight)
            {
                var endE = stream.Consume(TokenType.BracketRight, TokenFamily.Grouping);
                var emptyList = new ArrayLiteralNode(parent);
                Utils.SetStart(emptyList, open);
                Utils.SetEnd(emptyList, endE);
                return emptyList;
            }

            // Parse the first sub-expression. If a top-level `:` follows it (still inside the
            // bracket), it's a map; otherwise it's a list.
            var firstStream = ExtractUntilCommaColonOrClose(stream);
            var firstExpr = ParseExpression(firstStream, parent);

            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Colon)
            {
                // Map literal — first entry's key is firstExpr.
                stream.Consume(TokenType.Colon, TokenFamily.Operator);
                var map = new MapLiteralNode(parent);
                Utils.SetStart(map, open);

                var firstValueStream = ExtractUntilCommaColonOrClose(stream);
                var firstValue = ParseExpression(firstValueStream, map);
                map.Keys.Add(firstExpr!);
                map.Values.Add(firstValue!);
                if (firstExpr != null) { firstExpr.Parent = map; }

                while (!stream.IsEmpty() && stream.Peek().Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenFamily.Operator);
                    var keyStream = ExtractUntilCommaColonOrClose(stream);
                    var key = ParseExpression(keyStream, map);
                    if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Colon)
                    {
                        stream.Consume(TokenType.Colon, TokenFamily.Operator);
                    }
                    var valStream = ExtractUntilCommaColonOrClose(stream);
                    var value = ParseExpression(valStream, map);
                    map.Keys.Add(key!);
                    map.Values.Add(value!);
                }
                var endTok = stream.Consume(TokenType.BracketRight, TokenFamily.Grouping);
                Utils.SetEnd(map, endTok);
                return map;
            }

            // List literal — first item is firstExpr.
            var list = new ArrayLiteralNode(parent);
            Utils.SetStart(list, open);
            list.Values.Add(firstExpr!);
            if (firstExpr != null) { firstExpr.Parent = list; }

            while (!stream.IsEmpty() && stream.Peek().Type == TokenType.Comma)
            {
                stream.Consume(TokenType.Comma, TokenFamily.Operator);
                var itemStream = ExtractUntilCommaColonOrClose(stream);
                var item = ParseExpression(itemStream, list);
                if (item != null) { list.Values.Add(item); }
            }
            var endTok2 = stream.Consume(TokenType.BracketRight, TokenFamily.Grouping);
            Utils.SetEnd(list, endTok2);
            return list;
        }

        // Pull tokens out of the stream up to (but not including) a top-level `,`, `:`, or `]`,
        // honouring nested parens/brackets/braces so an inner `(1, 2)` doesn't terminate early.
        private static TokenStream ExtractUntilCommaColonOrClose(TokenStream stream)
        {
            var tokens = new List<Token>();
            int paren = 0, bracket = 0, brace = 0;
            while (!stream.IsEmpty())
            {
                var t = stream.Peek();
                if (paren == 0 && bracket == 0 && brace == 0)
                {
                    if (t.Type is TokenType.Comma or TokenType.Colon or TokenType.BracketRight)
                    {
                        break;
                    }
                }
                if (t.Type == TokenType.ParenLeft) { paren++; }
                else if (t.Type == TokenType.ParenRight) { paren--; }
                else if (t.Type == TokenType.BracketLeft) { bracket++; }
                else if (t.Type == TokenType.BracketRight) { bracket--; }
                else if (t.Type == TokenType.CurlyLeft) { brace++; }
                else if (t.Type == TokenType.CurlyRight) { brace--; }
                tokens.Add(t);
                stream.Consume();
            }
            return new TokenStream(tokens);
        }
    }
}
