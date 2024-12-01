using AST.Nodes;
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
            Param,
            ParamNext
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
            // Expressions start with an identifier, paren or a literal
            var token = stream.Peek();

            if (
                token.Family is TokenFamily.Literal ||
                token.Family is TokenFamily.Identifier ||
                token.Type is TokenType.ParenLeft ||
                token.Type is TokenType.Self
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
                        // TODO: Expression parsing should not break fucntion parsing even when bad input
                        break;
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

            int funcNestingLevel = 0;
            
            bool hasGenericClause = false;
            
            while (!stream.IsEmpty())
            {   
                var token = stream.Consume();
                if (token.Type is TokenType.Identifier or TokenType.Self or TokenType.Dot or TokenType.Comma|| token.Family is TokenFamily.Literal)
                {
                    output.Add(token);
                    continue;
                }
                
                // TODO: Function calls dont work nested right now as the first nested call will trigger the paren right for the outer one
                if (token.Type == TokenType.ParenLeft)
                {
                    // When we have an identifier followed by a parenthesis without any operator
                    // we have a function call and parse until the closing parenthesis
                    if (output[^1].Type is TokenType.Identifier or TokenType.Self || hasGenericClause)
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

                // We need to check if the operator is just a dot for property access
                if (token.Type is TokenType.Dot or TokenType.Scope)
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
                if (token.Type is TokenType.Identifier or TokenType.Self)
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
                    var literal = new LiteralNode(token.Value, type);

                    Utils.SetMeta(literal, token);

                    stream.Consume();

                    stack.Push(literal);
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

                    if (token.Family is TokenFamily.Literal || token.Type is TokenType.Identifier or TokenType.Self)
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

                    if (token.Type is TokenType.Identifier or TokenType.Self || token.Family is TokenFamily.Literal)
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

                    if (token.Type is TokenType.Identifier or TokenType.Self || token.Family is TokenFamily.Literal)
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

                    if (token.Type is TokenType.Identifier or TokenType.Self || token.Family is TokenFamily.Literal)
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

                    if (token.Type is TokenType.Identifier or TokenType.Self || token.Family is TokenFamily.Literal)
                    {
                        return ExpressionState.Operand;
                    }
                    
                    if (token.Type is TokenType.Dot)
                    {
                        return ExpressionState.MemberAccess;
                    }

                    if (token.Type is TokenType.Dot)
                    {
                        return ExpressionState.MemberAccess;
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
                    if (token.Type is TokenType.Identifier or TokenType.Self || token.Family is TokenFamily.Literal)
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
    }
}
