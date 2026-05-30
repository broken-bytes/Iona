//|--- StatementParser.cs --------------------------------------|
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

namespace Parser.Parsers
{
    internal class StatementParser
    {
        private readonly BlockParser blockParser;
        private readonly ClassParser classParser;
        private readonly ContractParser contractParser;
        private readonly ExpressionParser expressionParser;
        private readonly FuncParser funcParser;
        private readonly InitParser initParser;
        private readonly MemberAccessParser memberAccessParser;
        private readonly ModuleParser moduleParser;
        private readonly OperatorParser operatorParser;
        private readonly PropertyParser propertyParser;
        private readonly RecordParser recordParser;
        private readonly StructParser structParser;
        private readonly VariableParser variableParser;
        private readonly IErrorCollector _errorCollector;

        internal StatementParser(
            BlockParser blockParser,
            ClassParser classParser,
            ContractParser contractParser,
            ExpressionParser expressionParser,
            FuncParser funcParser,
            InitParser initParser,
            MemberAccessParser memberAccessParser,
            ModuleParser moduleParser,
            OperatorParser operatorParser,
            PropertyParser propertyParser,
            RecordParser recordParser,
            StructParser structParser,
            VariableParser variableParser,
            IErrorCollector errorCollector
        )
        {
            this.blockParser = blockParser;
            this.classParser = classParser;
            this.contractParser = contractParser;
            this.expressionParser = expressionParser;
            this.funcParser = funcParser;
            this.initParser = initParser;
            this.memberAccessParser = memberAccessParser;
            this.moduleParser = moduleParser;
            this.operatorParser = operatorParser;
            this.propertyParser = propertyParser;
            this.recordParser = recordParser;
            this.structParser = structParser;
            this.variableParser = variableParser;
            _errorCollector = errorCollector;
        }

        public INode? Parse(TokenStream stream, INode? parent)
        {
            var token = stream.Peek();

            while (token.Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                token = stream.Peek();
            }

            // `#over<T, …> [where …]` and other `#directive` markers attach to the next
            // declaration. Parse them upfront, then parse the declaration with the directive
            // info merged in.
            if (token.Type == TokenType.Hash)
            {
                return ParseDirectiveAndDeclaration(stream, parent);
            }

            if (IsCompoundAssignment(stream) || IsBasicAssignment(stream))
            {
                return ParseAssignment(stream, parent);
            }

            if (classParser.IsClass(stream))
            {
                return classParser.Parse(stream, parent);
            }

            if (contractParser.IsContract(stream))
            {
                return contractParser.Parse(stream, parent);
            }

            if (funcParser.IsFunc(stream))
            {
                return funcParser.Parse(stream, parent);
            }

            if (initParser.IsInit(stream))
            {
                return initParser.Parse(stream, parent);
            }

            if (moduleParser.IsModule(stream))
            {
                return moduleParser.Parse(stream, parent);
            }

            if (operatorParser.IsOperator(stream))
            {
                return operatorParser.Parse(stream, parent);
            }

            if (recordParser.IsRecord(stream))
            {
                return recordParser.Parse(stream, parent);
            }

            if (structParser.IsStruct(stream))
            {
                return structParser.Parse(stream, parent);
            }

            if (propertyParser.IsProperty(stream) || variableParser.IsVariable(stream))
            {
                if (parent != null && parent.Parent is ClassNode or ContractNode or RecordNode or StructNode)
                {
                    return propertyParser.Parse(stream, parent);
                }

                return variableParser.Parse(stream, parent);
            }

            if (token.Type == TokenType.Use)
            {
                token = stream.Consume();

                var moduleImport = "";
                
                token = stream.Peek();
                
                // While the next token is an identifier or dot, keep adding and peeking
                while (token.Type is TokenType.Identifier or TokenType.Dot)
                {
                    moduleImport += stream.Consume().Value;
                    token = stream.Peek();
                }

                return new ImportNode(moduleImport, parent);
            }

            if (token.Type == TokenType.Guard)
            {
                return ParseGuard(stream, parent);
            }

            if (token.Type == TokenType.If)
            {
                return ParseIf(stream, parent);
            }

            if (token.Type == TokenType.While)
            {
                return ParseWhile(stream, parent);
            }

            if (token.Type == TokenType.For)
            {
                return ParseFor(stream, parent);
            }

            if (token.Type == TokenType.Break)
            {
                return ParseBreak(stream, parent);
            }

            if (token.Type == TokenType.Continue)
            {
                return ParseContinue(stream, parent);
            }

            if (token.Type == TokenType.Return)
            {
                return ParseReturn(stream, parent);
            }


            if (token.Type is TokenType.Public or TokenType.Private or TokenType.Internal)
            {
                // Consume the current token (because it could be a keyword like public)
                stream.Consume();
                
                token = stream.Peek();
            }
            
            // Invalid token
            var meta = new Metadata
            {
                File = token.File,
                ColumnStart = token.ColumnStart,
                ColumnEnd = token.ColumnEnd,
                LineStart = token.Line,
                LineEnd = token.Line,
            };
            
            var error = CompilerErrorFactory.ExpectedMember(token.Value, meta);
            _errorCollector.Collect(error);
            
            // Panic until a new keyword is hit
            stream.Panic(TokenFamily.Keyword);

            return null;
        }

        public bool IsStatement(TokenStream stream)
        {
            return
                IsCompoundAssignment(stream) ||
                IsBasicAssignment(stream) ||
                IsGuardStatement(stream) ||
                IsIfStatement(stream) ||
                IsWhileStatement(stream) ||
                IsForStatement(stream) ||
                IsBreakStatement(stream) ||
                IsContinueStatement(stream) ||
                IsReturnStatement(stream) ||
                IsVariable(stream) ||
                IsProperty(stream);
        }

        // ------------------- Helper methods -------------------
        private INode? ParseDirectiveAndDeclaration(TokenStream stream, INode? parent)
        {
            // Collect a stack of `#`-prefixed adornments attached to one declaration:
            // either generics (`#over<…>`) or user attributes (`#name(args)`). They can
            // appear in any order and interleave with linebreaks.
            List<GenericArgument> generics = new();
            List<AttributeNode> attributes = new();

            while (!stream.IsEmpty() && stream.Peek().Type == TokenType.Hash)
            {
                stream.Consume(TokenType.Hash, TokenFamily.Special);
                var nameTok = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                var directive = nameTok.Value;

                if (directive == "over")
                {
                    generics = ParseOverDirective(stream, parent);
                }
                else
                {
                    // Treat any other `#name(...)` as a user attribute. C#-style mapping is
                    // applied in codegen — `#range(...)` looks up `RangeAttribute` first.
                    attributes.Add(ParseAttributeBody(stream, parent, directive, nameTok));
                }

                while (!stream.IsEmpty() && stream.Peek().Type == TokenType.Linebreak)
                {
                    stream.Consume(TokenType.Linebreak, TokenFamily.Special);
                }
            }

            var inner = Parse(stream, parent);
            if (inner == null) { return null; }
            AttachGenericsToDeclaration(inner, generics);
            AttachAttributesToDeclaration(inner, attributes);
            return inner;
        }

        // `#name(arg1: val1, arg2: val2, …)` or bare `#name`. Args reuse the FuncCallArg shape
        // so codegen can match them against an attribute constructor's parameters by name.
        private AttributeNode ParseAttributeBody(TokenStream stream, INode? parent, string name, Token nameTok)
        {
            var attr = new AttributeNode(name, parent);
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
                // Named: `name: value`. Positional fallback uses an empty name.
                if (stream.Count() >= 2
                    && stream.Peek().Type == TokenType.Identifier
                    && stream.Peek(2)[1].Type == TokenType.Colon)
                {
                    argName = stream.Consume(TokenType.Identifier, TokenFamily.Identifier).Value;
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);
                }
                // Slice tokens for one arg expression — stop at the top-level `,` or `)` so
                // the expression parser sees a bounded stream and doesn't blow past the `)`.
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

        private static void AttachAttributesToDeclaration(INode inner, List<AttributeNode> attrs)
        {
            if (attrs.Count == 0) { return; }
            switch (inner)
            {
                case ClassNode c: c.Attributes.AddRange(attrs); break;
                case RecordNode r: r.Attributes.AddRange(attrs); break;
                case StructNode s: s.Attributes.AddRange(attrs); break;
                case ContractNode k: k.Attributes.AddRange(attrs); break;
                case FuncNode f: f.Attributes.AddRange(attrs); break;
                case PropertyNode p: p.Attributes.AddRange(attrs); break;
            }
            foreach (var a in attrs) { a.Parent = inner; }
        }

        private List<GenericArgument> ParseOverDirective(TokenStream stream, INode? parent)
        {
            var list = new List<GenericArgument>();
            // `<` is lexed as TokenType.ArrowLeft in this codebase.
            stream.Consume(TokenType.ArrowLeft, TokenFamily.Operator);
            while (stream.Peek().Type != TokenType.ArrowRight)
            {
                var nameTok = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                var arg = new GenericArgument(nameTok.Value, parent);
                // Inline constraint(s): `T: Numeric` or `T: Numeric & Comparable & Hashable`.
                if (stream.Peek().Type == TokenType.Colon)
                {
                    stream.Consume(TokenType.Colon, TokenFamily.Operator);
                    ParseConstraintList(stream, arg);
                }
                list.Add(arg);
                if (stream.Peek().Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenFamily.Operator);
                }
            }
            stream.Consume(TokenType.ArrowRight, TokenFamily.Operator);

            // Optional trailing/next-line `where T: A, S: B, …` clause. The where can sit on
            // the same line or, more commonly, on its own line for readability.
            while (!stream.IsEmpty() && stream.Peek().Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Special);
                // We may be looking at the declaration; only continue if `where` follows.
                if (stream.Peek().Type != TokenType.Identifier
                    || stream.Peek().Value != "where")
                {
                    break;
                }
            }
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Identifier
                && stream.Peek().Value == "where")
            {
                stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
                ParseWhereClause(stream, list);
            }

            return list;
        }

        private void ParseWhereClause(TokenStream stream, List<GenericArgument> generics)
        {
            // `where T: Constraint, S: Constraint, …`
            while (true)
            {
                var paramName = stream.Consume(TokenType.Identifier, TokenFamily.Identifier).Value;
                stream.Consume(TokenType.Colon, TokenFamily.Operator);
                var target = generics.FirstOrDefault(g => g.Name == paramName);
                if (target != null)
                {
                    ParseConstraintList(stream, target);
                }
                else
                {
                    // Discard the constraint stream to keep alignment when the parameter name
                    // is unknown — the resolver will flag it later. We still need to walk past
                    // any `& Other` bounds so the comma loop below stays in sync.
                    ParseSimpleTypeRef(stream, null);
                    while (!stream.IsEmpty() && stream.Peek().Type == TokenType.BitAnd)
                    {
                        stream.Consume(TokenType.BitAnd, TokenFamily.Operator);
                        ParseSimpleTypeRef(stream, null);
                    }
                }
                if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Comma)
                {
                    stream.Consume(TokenType.Comma, TokenFamily.Operator);
                    continue;
                }
                break;
            }
        }

        // Parse `A` or `A & B & C` — intersection-of-contracts shape. Each bound is appended
        // to the GenericArgument's Constraints list; codegen / resolver semantics treat them
        // as a logical AND.
        private static void ParseConstraintList(TokenStream stream, GenericArgument target)
        {
            target.Constraints.Add(ParseSimpleTypeRef(stream, target));
            while (!stream.IsEmpty() && stream.Peek().Type == TokenType.BitAnd)
            {
                stream.Consume(TokenType.BitAnd, TokenFamily.Operator);
                target.Constraints.Add(ParseSimpleTypeRef(stream, target));
            }
        }

        private static TypeReferenceNode ParseSimpleTypeRef(TokenStream stream, INode? parent)
        {
            var tok = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);
            var node = new TypeReferenceNode(tok.Value, parent)
            {
                FullyQualifiedName = tok.Value
            };
            Utils.SetMeta(node, tok);
            return node;
        }

        private static void SkipUnknownDirective(TokenStream stream)
        {
            // Conservative: if the next token is `<` or `(`, consume the matching close. Done.
            if (stream.IsEmpty()) { return; }
            var open = stream.Peek().Type;
            if (open is not (TokenType.ArrowLeft or TokenType.ParenLeft)) { return; }
            var close = open == TokenType.ArrowLeft ? TokenType.ArrowRight : TokenType.ParenRight;
            stream.Consume();
            int depth = 1;
            while (!stream.IsEmpty() && depth > 0)
            {
                var t = stream.Peek();
                stream.Consume();
                if (t.Type == open) { depth++; }
                else if (t.Type == close) { depth--; }
            }
        }

        private static void AttachGenericsToDeclaration(INode node, List<GenericArgument> generics)
        {
            if (generics.Count == 0) { return; }
            switch (node)
            {
                case ClassNode cn: cn.GenericArguments = generics; break;
                case FuncNode fn: fn.GenericArguments = generics; break;
                case ContractNode con: con.GenericArguments = generics; break;
                case RecordNode rn: rn.GenericArguments = generics; break;
                case StructNode sn: sn.GenericArguments = generics; break;
                // Other declaration kinds silently drop the generics for now.
            }
        }

        private INode ParseAssignment(TokenStream stream, INode? parent)
        {
            if (IsCompoundAssignment(stream))
            {
                return ParseCompoundAssignment(stream, parent);
            }

            return ParseBasicAssignment(stream, parent);
        }

        private INode ParseBasicAssignment(TokenStream stream, INode? parent)
        {
            var target = expressionParser.Parse(stream, parent);

            // Consume the assign operator
            var token = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);

            if (token.Type != TokenType.Assign)
            {
                var error = new ErrorNode(
                    "Invalid operator after identifier",
                    target,
                    parent
                );
                Utils.SetMeta(error, token);

                return error;
            }

            var value = expressionParser.Parse(stream, null);

            var assign = new AssignmentNode(AssignmentType.Assign, target, value, parent);
            target.Parent = assign;
            value.Parent = assign;

            // The meta for assign ranges from the start of the target to the end of the value, so we just combine them
            Utils.SetMeta(assign, target, value);

            return assign;
        }

        private INode ParseCompoundAssignment(TokenStream stream, INode? parent)
        {
            var target = expressionParser.Parse(stream, parent);
            
            // Consume the compound operator
            var token = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);

            // Get the compound operation
            var compoundOperation = GetAssignmentType(token);

            if(compoundOperation == null)
            {
                var error = new ErrorNode(
                    "Invalid operator after identifier",
                    target,
                    parent
                );
                Utils.SetMeta(error, token);

                return error;
            }

            var value = expressionParser.Parse(stream, parent);

            var assignment = new AssignmentNode(compoundOperation.Value, target, value, parent);
            assignment.Value.Parent = assignment;
            assignment.Target.Parent = assignment;
            Utils.SetMeta(assignment, target, value);


            return assignment;
        }

        private INode ParseReturn(TokenStream stream, INode? parent)
        {
            var token = stream.Consume(TokenType.Return, TokenFamily.Keyword);

            var returnNode = new ReturnNode(parent);
            Utils.SetMeta(returnNode, token);

            // Check if this is a void return (next meaningful token is } or linebreak followed by })
            var next = stream.Peek();
            if (next.Type != TokenType.CurlyRight && next.Type != TokenType.Linebreak)
            {
                var expression = (IExpressionNode)expressionParser.Parse(stream, parent);
                returnNode.Value = expression;
            }

            return returnNode;
        }

        private bool IsBasicAssignment(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (
                (tokens[0].Family == TokenFamily.Identifier) &&
                tokens[1].Type == TokenType.Assign
            )
            {
                return true;
            }

            // An assigment may also start with a member access, so we need to check for that
            if (memberAccessParser.IsMemberAccess(stream))
            {
                // Check if the next token is an assign operator (=)
                var op = memberAccessParser.PeekTokenAfterMemberAccess(stream);

                if (op.Type == TokenType.Assign)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCompoundAssignment(TokenStream stream)
        {
            var tokens = stream.Peek(2);

            if (
                (tokens[0].Family == TokenFamily.Identifier) &&
                IsCompoundOperator(tokens[1])
            )
            {
                return true;
            }
            
            // An assigment may also start with a member access, so we need to check for that
            if (memberAccessParser.IsMemberAccess(stream))
            {
                // Check if the next token is an assign operator (=)
                var op = memberAccessParser.PeekTokenAfterMemberAccess(stream);

                if (IsCompoundOperator(op))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCompoundOperator(Token token)
        {
            // If the token is not an operator, it cannot be a compound operator
            if (token.Family != TokenFamily.Operator)
            {
                return false;
            }

            // Check what token it is
            switch (token.Value)
            {
                case "+=":
                case "-=":
                case "*=":
                case "/=":
                case "%=":
                    return true;
                default:
                    return false;
            }
        }

        private INode ParseGuard(TokenStream stream, INode? parent)
        {
            var guardToken = stream.Consume(TokenType.Guard, TokenFamily.Keyword);

            // Check if this is a binding guard (guard var/let name = expr else { ... })
            var next = stream.Peek();
            if (next.Type is TokenType.Var or TokenType.Let)
            {
                return ParseGuardBinding(stream, guardToken, parent);
            }

            // Condition guard: guard condition else { block }
            var condition = (IExpressionNode)expressionParser.Parse(stream, parent);

            // Skip linebreaks before 'else'
            while (stream.Peek().Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
            }

            // Consume the required 'else' keyword
            stream.Consume(TokenType.Else, TokenFamily.Keyword);

            // Skip linebreaks before block
            while (stream.Peek().Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
            }

            // Parse the else body block
            var body = (BlockNode)blockParser.Parse(stream, parent);

            var guardNode = new GuardNode(condition, body, parent);
            body.Parent = guardNode;
            condition.Parent = guardNode;
            Utils.SetMeta(guardNode, guardToken);

            return guardNode;
        }

        private INode ParseGuardBinding(TokenStream stream, Token guardToken, INode? parent)
        {
            var varLetToken = stream.Consume();
            bool isMutable = varLetToken.Type == TokenType.Var;

            var identifier = stream.Consume(TokenType.Identifier, TokenFamily.Identifier);

            // Consume '='
            stream.Consume(TokenType.Assign, TokenFamily.Keyword);

            // Parse the expression to unwrap
            var expression = (IExpressionNode)expressionParser.Parse(stream, parent);

            // Skip linebreaks before 'else'
            while (stream.Peek().Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
            }

            // Consume the required 'else' keyword
            stream.Consume(TokenType.Else, TokenFamily.Keyword);

            // Skip linebreaks before block
            while (stream.Peek().Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
            }

            // Parse the else body block
            var body = (BlockNode)blockParser.Parse(stream, parent);

            var guardNode = new GuardNode(identifier.Value, isMutable, expression, body, parent);
            body.Parent = guardNode;
            expression.Parent = guardNode;
            Utils.SetMeta(guardNode, guardToken);

            return guardNode;
        }

        private INode ParseIf(TokenStream stream, INode? parent)
        {
            var ifToken = stream.Consume(TokenType.If, TokenFamily.Keyword);

            // Parse the condition expression
            var condition = (IExpressionNode)expressionParser.Parse(stream, parent);

            // Skip linebreaks before block
            while (stream.Peek().Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
            }

            // Parse the body block
            var body = (BlockNode)blockParser.Parse(stream, parent);

            var ifNode = new IfNode(condition, body, parent);
            body.Parent = ifNode;
            condition.Parent = ifNode;
            Utils.SetMeta(ifNode, ifToken);

            // Parse else if / else clauses
            while (true)
            {
                // Skip linebreaks
                while (!stream.IsEmpty() && stream.Peek().Type == TokenType.Linebreak)
                {
                    stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                }

                if (stream.IsEmpty() || stream.Peek().Type != TokenType.Else)
                {
                    break;
                }

                stream.Consume(TokenType.Else, TokenFamily.Keyword);

                // Check if this is "else if" or plain "else"
                if (!stream.IsEmpty() && stream.Peek().Type == TokenType.If)
                {
                    stream.Consume(TokenType.If, TokenFamily.Keyword);

                    var elseIfCondition = (IExpressionNode)expressionParser.Parse(stream, parent);

                    while (!stream.IsEmpty() && stream.Peek().Type == TokenType.Linebreak)
                    {
                        stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                    }

                    var elseIfBody = (BlockNode)blockParser.Parse(stream, parent);
                    elseIfBody.Parent = ifNode;
                    elseIfCondition.Parent = ifNode;

                    ifNode.ElseClauses.Add(new ElseClause(elseIfCondition, elseIfBody));
                }
                else
                {
                    // Plain else
                    while (!stream.IsEmpty() && stream.Peek().Type == TokenType.Linebreak)
                    {
                        stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
                    }

                    var elseBody = (BlockNode)blockParser.Parse(stream, parent);
                    elseBody.Parent = ifNode;

                    ifNode.ElseClauses.Add(new ElseClause(null, elseBody));
                    break; // else is always the last clause
                }
            }

            return ifNode;
        }

        private INode ParseWhile(TokenStream stream, INode? parent)
        {
            var whileToken = stream.Consume(TokenType.While, TokenFamily.Keyword);

            // Parse the condition expression
            var condition = (IExpressionNode)expressionParser.Parse(stream, parent);

            // Skip linebreaks before block
            while (stream.Peek().Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
            }

            // Parse the body block
            var body = (BlockNode)blockParser.Parse(stream, parent);

            var whileNode = new WhileNode(condition, body, parent);
            body.Parent = whileNode;
            condition.Parent = whileNode;
            Utils.SetMeta(whileNode, whileToken);

            return whileNode;
        }

        private INode ParseFor(TokenStream stream, INode? parent)
        {
            var forToken = stream.Consume(TokenType.For, TokenFamily.Keyword);

            // Parse iterator variable name (identifier or "_")
            var iteratorToken = stream.Consume();
            var iteratorName = iteratorToken.Value;

            // Consume 'in' keyword
            stream.Consume(TokenType.In, TokenFamily.Keyword);

            // Parse the iterable expression — could be a range (0...3) or a collection
            var iterable = ParseIterableExpression(stream, parent);

            // Skip linebreaks before block
            while (stream.Peek().Type == TokenType.Linebreak)
            {
                stream.Consume(TokenType.Linebreak, TokenFamily.Keyword);
            }

            // Parse the body block
            var body = (BlockNode)blockParser.Parse(stream, parent);

            var forNode = new ForNode(iteratorName, iterable, body, parent);
            body.Parent = forNode;
            iterable.Parent = forNode;
            Utils.SetMeta(forNode, forToken);

            return forNode;
        }

        private IExpressionNode ParseIterableExpression(TokenStream stream, INode? parent)
        {
            var start = (IExpressionNode)expressionParser.Parse(stream, parent);

            // Check if this is a range expression
            if (!stream.IsEmpty() && stream.Peek().Type == TokenType.Range)
            {
                stream.Consume(TokenType.Range, TokenFamily.Keyword);
                var end = (IExpressionNode)expressionParser.Parse(stream, parent);

                var range = new RangeExpressionNode(start, end, parent);
                Utils.SetMeta(range, start, end);
                return range;
            }

            return start;
        }

        private INode ParseBreak(TokenStream stream, INode? parent)
        {
            var token = stream.Consume(TokenType.Break, TokenFamily.Keyword);
            var breakNode = new BreakNode(parent);
            Utils.SetMeta(breakNode, token);
            return breakNode;
        }

        private INode ParseContinue(TokenStream stream, INode? parent)
        {
            var token = stream.Consume(TokenType.Continue, TokenFamily.Keyword);
            var continueNode = new ContinueNode(parent);
            Utils.SetMeta(continueNode, token);
            return continueNode;
        }

        private bool IsGuardStatement(TokenStream stream)
        {
            return stream.Peek().Type == TokenType.Guard;
        }

        private bool IsIfStatement(TokenStream stream)
        {
            return stream.Peek().Type == TokenType.If;
        }

        private bool IsWhileStatement(TokenStream stream)
        {
            return stream.Peek().Type == TokenType.While;
        }

        private bool IsForStatement(TokenStream stream)
        {
            return stream.Peek().Type == TokenType.For;
        }

        private bool IsBreakStatement(TokenStream stream)
        {
            return stream.Peek().Type == TokenType.Break;
        }

        private bool IsContinueStatement(TokenStream stream)
        {
            return stream.Peek().Type == TokenType.Continue;
        }

        private bool IsProperty(TokenStream stream)
        {
            return propertyParser.IsProperty(stream);
        }

        private bool IsReturnStatement(TokenStream stream)
        {
            return stream.Peek().Type == TokenType.Return;
        }

        private bool IsVariable(TokenStream stream)
        {
            return variableParser.IsVariable(stream);
        }

        private AssignmentType? GetAssignmentType(Token token)
        {
            switch (token.Type)
            {
                case TokenType.PlusAssign:
                    return AssignmentType.AddAssign;
                case TokenType.MinusAssign:
                    return AssignmentType.SubAssign;
                case TokenType.MultiplyAssign:
                    return AssignmentType.MulAssign;
                case TokenType.DivideAssign:
                    return AssignmentType.DivAssign;
                case TokenType.ModAssign:
                    return AssignmentType.ModAssign;
            }

            return null;
        }
    }
}
