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

            if (structParser.IsStruct(stream))
            {
                return structParser.Parse(stream, parent);
            }

            if (propertyParser.IsProperty(stream) || variableParser.IsVariable(stream))
            {
                if (parent != null && parent.Parent is ClassNode or ContractNode or StructNode)
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

            // Parse the return value
            var expression = (IExpressionNode)expressionParser.Parse(stream, parent);
            returnNode.Value = expression;

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

        /// <summary>
        /// Parses: if condition { block } [else if condition { block }]* [else { block }]
        /// </summary>
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

        /// <summary>
        /// Parses: while condition { block }
        /// </summary>
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

        /// <summary>
        /// Parses: for identifier in expression { block }
        /// </summary>
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

        /// <summary>
        /// Parses the iterable part of a for loop.
        /// Handles range expressions (start...end) and regular expressions.
        /// </summary>
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
