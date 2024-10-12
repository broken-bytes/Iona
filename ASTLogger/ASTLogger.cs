using AST.Nodes;
using AST.Visitors;
using System.Text;

namespace ASTLogger
{
    internal class ASTLogger : IASTLogger,
        IAssignmentVisitor,
        IBlockVisitor,
        IBinaryExpressionVisitor,
        IClassVisitor,
        IContractVisitor,
        IErrorVisitor,
        IFileVisitor,
        IFuncCallVisitor,
        IFuncVisitor,
        IIdentifierVisitor,
        IImportVisitor,
        IInitVisitor,
        ILiteralVisitor,
        IMemberAccessVisitor,
        IModuleVisitor,
        IObjectLiteralVisitor,
        IOperatorVisitor,
        IPropertyVisitor,
        IReturnVisitor,
        IStructVisitor,
        ITypeReferenceVisitor,
        IUnaryExpressionVisitor,
        IVariableVisitor
    {
        private int _indentLevel = 0;

        public void Log(INode node)
        {
            GetAndLogNode(node);
        }

        public void Visit(AssignmentNode node)
        {
            Log("> ASSIGNMENT:");
            Log($" - Type: {node.AssignmentType}");
            Log("- Target:");
            LogMeta(node);

            _indentLevel++;
            GetAndLogNode(node.Target);
            _indentLevel--;

            Log("- Value:");
            _indentLevel++;
            GetAndLogNode(node.Value);
            _indentLevel--;

            Spacer();
        }

        public void Visit(BinaryExpressionNode node)
        {
            Log("> BINARY EXPRESSION:");
            LogMeta(node);
            Log($"- Operator: {node.Operation}");
            Log($"- Left:");
            _indentLevel++;
            GetAndLogNode(node.Left);
            _indentLevel--;
            Log($"- Right:");
            _indentLevel++;
            GetAndLogNode(node.Right);
            _indentLevel--;
            Spacer();
        }

        public void Visit(BlockNode node)
        {
            Log("> BLOCK:");
            LogMeta(node);

            _indentLevel++;

            foreach (var child in node.Children)
            {
                GetAndLogNode(child);
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(ClassNode node)
        {
            Log("> CLASS:");
            LogMeta(node);
            Log($"- Name: {node.Name}");
            Log($"- Access Level: {node.AccessLevel}");

            _indentLevel++;

            if (node.Body != null)
            {
                GetAndLogNode(node.Body);
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(ContractNode node)
        {
            Log("> CONTRACT:");
            LogMeta(node);
            Log($"- Name: {node.Name}");
            Log($"- Access Level: {node.AccessLevel}");

            _indentLevel++;

            if (node.Body != null)
            {
                GetAndLogNode(node.Body);
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(ErrorNode node)
        {
            Log("> ERROR:");
            LogMeta(node);
            Log($"- Message: {node.Message}");
            Spacer();
        }

        public void Visit(FileNode node)
        {
            Log("> FILE:");
            LogMeta(node);
            Log($"- Name: {node.Name}");

            _indentLevel++;

            foreach (var child in node.Children)
            {
                GetAndLogNode(child);
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(FuncCallNode node)
        {
            Log("> FUNCTION CALL:");
            LogMeta(node);
            Log($"- Name: {((IdentifierNode)node.Target).Name}");

            if (node.Args.Count > 0)
            {
                Log("- Arguments:");

                _indentLevel++;
                foreach (var argument in node.Args)
                {
                    Log($"Name: {argument.Name}");
                    Log("Value: ");
                    _indentLevel++;
                    GetAndLogNode(argument.Value);
                    _indentLevel--;
                }
                _indentLevel--;
            }

            Spacer();
        }

        public void Visit(FuncNode node)
        {
            Log("> FUNCTION:");
            LogMeta(node);
            Log($"- Name: {node.Name}");
            if (node.Parameters.Count > 0)
            {
                Log("- Parameters:");
                _indentLevel++;
                foreach (var parameter in node.Parameters)
                {
                    Log($"- Name: {parameter.Name}");
                    Log("- Type:");
                    _indentLevel++;
                    GetAndLogNode(parameter.Type);
                    _indentLevel--;
                }
                _indentLevel--;
            }
            if (node.ReturnType != null)
            {
                Log("- Return Type:");
                _indentLevel++;
                GetAndLogNode(node.ReturnType);
                _indentLevel--;
            }
            Log($"- Mutating: {node.IsMutable}");

            // If the parent is a block node, then we are inside a class, struct, or contract
            if (node.Parent is BlockNode)
            {
                Log($"- Access Level: {node.AccessLevel}");
            }

            _indentLevel++;

            if (node.Body != null)
            {
                GetAndLogNode(node.Body);
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(IdentifierNode node)
        {
            Log("> IDENTIFIER:");
            LogMeta(node);

            Log($"- Name: {node.Name}");
            Spacer();
        }

        public void Visit(ImportNode import)
        {
            Log("> IMPORT:");
            LogMeta(import);

            Log($"- Name: {import.Name}");
            Spacer();
        }

        public void Visit(InitNode node)
        {
            Log("> INIT:");
            LogMeta(node);

            Log($"- Name: {node.Name}");
            if (node.Parameters.Count > 0)
            {
                Log("- Parameters:");
                _indentLevel++;
                foreach (var parameter in node.Parameters)
                {
                    Log($"- Name: {parameter.Name}");
                    Log("- Type:");
                    _indentLevel++;
                    GetAndLogNode(parameter.Type);
                    _indentLevel--;
                }
                _indentLevel--;
            }
            Log($"- Access Level: {node.AccessLevel}");

            _indentLevel++;

            if (node.Body != null)
            {
                GetAndLogNode(node.Body);
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(LiteralNode node)
        {
            Log("> LITERAL:");
            LogMeta(node);

            Log($"- Value: {node.Value}");
            Spacer();
        }

        public void Visit(MemberAccessNode node)
        {
            Log("> MEMBER ACCESS:");
            LogMeta(node);

            Log("- Target:");

            _indentLevel++;
            GetAndLogNode(node.Target);
            _indentLevel--;

            Log("- Member:");

            _indentLevel++;
            GetAndLogNode(node.Member);
            _indentLevel--;

            Spacer();
        }

        public void Visit(ModuleNode node)
        {
            Log("> MODULE:");
            LogMeta(node);

            Log($"- Name: {node.Name}");

            _indentLevel++;

            foreach (var child in node.Children)
            {
                GetAndLogNode(child);
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(ObjectLiteralNode node)
        {
            Log("> OBJECT LITERAL:");
            LogMeta(node);

            Log($"- Type: {node.Type}");

            _indentLevel++;

            foreach (var property in node.Arguments)
            {
                Log($"- {property.Name}:");
                _indentLevel++;
                GetAndLogNode(property.Value);
                _indentLevel--;
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(OperatorNode node)
        {
            Log("> OPERATOR:");
            LogMeta(node);

            Log($"- Operator: {node.Op}");
            Log($"- Access Level: {node.AccessLevel}");
            Log($"- Static: {node.IsStatic}");

            _indentLevel++;

            if (node.Body != null)
            {
                GetAndLogNode(node.Body);
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(PropertyNode node)
        {
            Log("> PROPERTY:");
            LogMeta(node);

            Log($"- Name: {node.Name}");
            Log($"- Access Level: {node.AccessLevel}");
            if (node.TypeNode != null)
            {
                Log("- Type: ");
                _indentLevel++;
                GetAndLogNode(node.TypeNode);
                _indentLevel--;
            }
            if (node.Value != null)
            {
                Log("- Value: ");

                _indentLevel++;
                GetAndLogNode(node.Value);
                _indentLevel--;
            }

            Spacer();
        }

        public void Visit(ReturnNode node)
        {
            Log("> RETURN:");
            LogMeta(node);

            if (node.Value != null)
            {
                Log("- Value:");
                _indentLevel++;
                GetAndLogNode(node.Value);
                _indentLevel--;
            }

            Spacer();
        }

        public void Visit(StructNode node)
        {
            Log("> STRUCT:");
            LogMeta(node);

            Log($"- Name: {node.Name}");
            Log($"- Access Level: {node.AccessLevel}");
            if (node.Contracts.Count > 0)
            {
                Log($"- Contracts");
                _indentLevel++;
                foreach (var contract in node.Contracts)
                {
                    GetAndLogNode(contract);
                }
                _indentLevel--;
            }

            _indentLevel++;

            if (node.Body != null)
            {
                GetAndLogNode(node.Body);
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(TypeReferenceNode node)
        {
            Log("> TYPE REFERENCE:");
            LogMeta(node);

            Log($"- Name: {node.Name}");
            Spacer();
        }

        public void Visit(UnaryExpressionNode node)
        {
            Log("> UNARY EXPRESSION:");
            LogMeta(node);

            Log($"- Operator: {node.Operation}");
            Log($"- Operand: {node.Operand}");
            Spacer();
        }

        public void Visit(VariableNode node)
        {
            Log("> VARIABLE:");
            LogMeta(node);

            Log($"- Name: {node.Name}");
            if (node.Value != null)
            {
                Log("- Value:");
                _indentLevel++;
                GetAndLogNode(node.Value);
                _indentLevel--;
            }
            Log($"- Type:");
            _indentLevel++;
            if (node.TypeNode != null)
            {
                GetAndLogNode(node.TypeNode);
            }
            else
            {
                Log("Unknown");
            }
            _indentLevel--;
            Spacer();
        }

        private void GetAndLogNode(INode node)
        {
            switch (node)
            {
                case AssignmentNode assignmentNode:
                    assignmentNode.Accept(this);
                    break;
                case BinaryExpressionNode binaryExpressionNode:
                    binaryExpressionNode.Accept(this);
                    break;
                case BlockNode blockNode:
                    blockNode.Accept(this);
                    break;
                case ClassNode classNode:
                    classNode.Accept(this);
                    break;
                case ContractNode contractNode:
                    contractNode.Accept(this);
                    break;
                case ErrorNode errorNode:
                    errorNode.Accept(this);
                    break;
                case FileNode fileNode:
                    fileNode.Accept(this);
                    break;
                case FuncCallNode funcCallNode:
                    funcCallNode.Accept(this);
                    break;
                case FuncNode funcNode:
                    funcNode.Accept(this);
                    break;
                case IdentifierNode identifierNode:
                    identifierNode.Accept(this);
                    break;
                case ImportNode importNode:
                    importNode.Accept(this);
                    break;
                case InitNode initNode:
                    initNode.Accept(this);
                    break;
                case LiteralNode literalNode:
                    literalNode.Accept(this);
                    break;
                case MemberAccessNode memberAccessNode:
                    memberAccessNode.Accept(this);
                    break;
                case ModuleNode moduleNode:
                    moduleNode.Accept(this);
                    break;
                case ObjectLiteralNode objectLiteralNode:
                    objectLiteralNode.Accept(this);
                    break;
                case OperatorNode operatorNode:
                    operatorNode.Accept(this);
                    break;
                case PropertyNode propertyNode:
                    propertyNode.Accept(this);
                    break;
                case ReturnNode returnNode:
                    returnNode.Accept(this);
                    break;
                case StructNode structNode:
                    structNode.Accept(this);
                    break;
                case TypeReferenceNode typeReferenceNode:
                    typeReferenceNode.Accept(this);
                    break;
                case UnaryExpressionNode unaryExpressionNode:
                    unaryExpressionNode.Accept(this);
                    break;
                case VariableNode variableNode:
                    variableNode.Accept(this);
                    break;
            }
        }

        private void Log(string message)
        {
            var builder = new StringBuilder();

            builder.Append("| ");

            for (int i = 0; i < _indentLevel; i++)
            {
                builder.Append(" | ");
            }

            builder.Append(message);

            Console.WriteLine(builder.ToString());
        }

        private void Spacer()
        {
            var builder = new StringBuilder();

            builder.Append("| ");

            for (int i = 0; i < _indentLevel - 1; i++)
            {
                builder.Append(" | ");
            }

            Console.WriteLine(builder.ToString());
        }

        private void LogMeta(INode node)
        {
            var meta = node.Meta;
            Log($"- Location: L[{meta.LineStart}-{meta.LineEnd}] C[{meta.ColumnStart}-{meta.ColumnEnd}]");
        }
    }
}
