using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using Shared;

namespace Parser.Parsers
{
    internal class StatementParser
    {
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
                    moduleImport += stream.Peek().Value;
                    token = stream.Consume();
                }

                return new ImportNode(moduleImport, parent);
            }

            if (token.Type == TokenType.Return)
            {
                return ParseReturn(stream, parent);
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
            
            // We have an edge case here. When we find a bad token but an access modifier before it, skip the access token
            if (token.Type is TokenType.Public or TokenType.Internal or TokenType.Private)
            {
            }
            
            var error = CompilerErrorFactory.ExpectedMember(token.Value, meta);
            _errorCollector.Collect(error);
            
            stream.Panic(TokenFamily.Keyword);

            return null;
        }

        public bool IsStatement(TokenStream stream)
        {
            return 
                IsCompoundAssignment(stream) || 
                IsBasicAssignment(stream) || 
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
            var target = stream.Consume(TokenFamily.Identifier, TokenFamily.Keyword);
            var identifierNode = new IdentifierNode(target.Value);
            Utils.SetMeta(identifierNode, target);

            // Consume the compound operator
            var token = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);

            // Get the compound operation
            var compoundOperation = GetAssignmentType(token);

            if(compoundOperation == null)
            {
                var error = new ErrorNode(
                    "Invalid operator after identifier",
                    identifierNode,
                    parent
                );
                Utils.SetMeta(error, token);

                return error;
            }

            var value = expressionParser.Parse(stream, parent);

            var assignment = new AssignmentNode(compoundOperation.Value, identifierNode, value, parent);
            assignment.Value.Parent = assignment;
            assignment.Target.Parent = assignment;
            Utils.SetMeta(assignment, target);

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
