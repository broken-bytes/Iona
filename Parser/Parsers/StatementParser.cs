using AST.Nodes;
using AST.Types;
using Lexer.Tokens;

namespace Parser.Parsers
{
    internal class StatementParser
    {
        private readonly ClassParser classParser;
        private readonly ContractParser contractParser;
        private readonly ExpressionParser expressionParser;
        private readonly FuncParser funcParser;
        private readonly InitParser initParser;
        private readonly ModuleParser moduleParser;
        private readonly PropertyParser propertyParser;
        private readonly StructParser structParser;
        private readonly VariableParser variableParser;

        internal StatementParser(
            ClassParser classParser,
            ContractParser contractParser,
            ExpressionParser expressionParser,
            FuncParser funcParser,
            InitParser initParser,
            ModuleParser moduleParser,
            PropertyParser propertyParser,
            StructParser structParser,
            VariableParser variableParser
        )
        {
            this.classParser = classParser;
            this.contractParser = contractParser;
            this.expressionParser = expressionParser;
            this.funcParser = funcParser;
            this.initParser = initParser;
            this.moduleParser = moduleParser;
            this.propertyParser = propertyParser;
            this.structParser = structParser;
            this.variableParser = variableParser;
        }

        public INode Parse(TokenStream stream, INode? parent)
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

            return ParseReturn(stream, parent);
        }

        public bool IsStatement(TokenStream stream)
        {
            return IsCompoundAssignment(stream) || IsBasicAssignment(stream) || IsReturnStatement(stream);
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
            var target = stream.Consume(TokenFamily.Identifier, TokenFamily.Keyword);
            var identifierNode = new IdentifierNode(target.Value);
            var value = expressionParser.Parse(stream, parent);

            return new AssignmentNode(AST.Types.AssignmentType.Assign, identifierNode, value, parent);
        }

        private INode ParseCompoundAssignment(TokenStream stream, INode? parent)
        {
            var target = stream.Consume(TokenFamily.Identifier, TokenFamily.Keyword);
            var identifierNode = new IdentifierNode(target.Value);

            // Consume the compound operator
            var token = stream.Consume(TokenFamily.Operator, TokenFamily.Keyword);

            // Get the compound operation
            var compoundOperation = GetAssignmentType(token);

            if(compoundOperation == null)
            {
                return new ErrorNode(token.Line, token.ColumnStart, token.ColumnEnd, token.File, "Invalid operator after identifier");
            }

            var value = expressionParser.Parse(stream, parent);

            return new AssignmentNode(compoundOperation.Value, identifierNode, value, parent);
        }

        private INode ParseReturn(TokenStream stream, INode? parent)
        {

            stream.Consume(TokenType.Return, TokenFamily.Keyword);
            // Parse the return value
            var expression = (IExpressionNode)expressionParser.Parse(stream, parent);
            var returnNode = new ReturnNode(expression);

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

        private bool IsReturnStatement(TokenStream stream)
        {
            return stream.Peek().Type == TokenType.Return;
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
