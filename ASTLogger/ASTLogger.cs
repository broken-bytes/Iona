﻿using AST.Nodes;
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
        IFuncVisitor,
        IIdentifierVisitor,
        IInitVisitor,
        ILiteralVisitor,
        IModuleVisitor,
        IPropertyVisitor,
        IStructVisitor,
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
            Log("ASSIGNMENT:");
            Log("- Target:");

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
            Log("BINARY EXPRESSION:");
            Log($"- Operator: {node.Operation}");
            Log($"- Left: {node.Left}");
            Log($"- Right: {node.Right}");
            Spacer();
        }

        public void Visit(BlockNode node)
        {
            Log("BLOCK:");

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
            Log("CLASS:");
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
            Log("CONTRACT:");
            Log($"- Name: {node.Name}");
            Log($"- Access Level: {node.AccessLevel}");

            _indentLevel++;

            if (node.Body != null)
            {
                foreach (var child in node.Body.Children)
                {
                    GetAndLogNode(child);
                }
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(ErrorNode node)
        {
            Log("ERROR:");
            Log($"- Message: {node.Message}");
            Spacer();
        }

        public void Visit(FileNode node)
        {
            Log("FILE:");
            Log($"- Name: {node.Name}");

            _indentLevel++;

            foreach (var child in node.Children)
            {
                GetAndLogNode(child);
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(FuncNode node)
        {
            Log("FUNCTION:");
            Log($"- Name: {node.Name}");
            if (node.Parameters.Count > 0)
            {
                Log("- Parameters:");
                foreach (var parameter in node.Parameters)
                {
                    Log($"  - {parameter}");
                }
            }
            Log($"- Return Type: {node.ReturnType}");
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
            Log("IDENTIFIER:");
            Log($"- Name: {node.Name}");
            Spacer();
        }

        public void Visit(InitNode node)
        {
            Log("INIT:");
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

        public void Visit(LiteralNode node)
        {
            Log("LITERAL:");
            Log($"- Value: {node.Value}");
            Spacer();
        }

        public void Visit(ModuleNode node)
        {
            Log("MODULE:");
            Log($"- Name: {node.Name}");

            _indentLevel++;

            foreach (var child in node.Children)
            {
                GetAndLogNode(child);
            }

            _indentLevel--;

            Spacer();
        }

        public void Visit(PropertyNode node)
        {
            Log("PROPERTY:");
            Log($"- Name: {node.Name}");
            Log($"- Access Level: {node.AccessLevel}");

            if (node.Value != null)
            {
                Log("- Value: ");

                _indentLevel++;
                GetAndLogNode(node.Value);
                _indentLevel--;
            }

            Spacer();
        }

        public void Visit(StructNode node)
        {
            Log("STRUCT:");
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

        public void Visit(UnaryExpressionNode node)
        {
            Log("UNARY EXPRESSION:");
            Log($"- Operator: {node.Operation}");
            Log($"- Operand: {node.Operand}");
            Spacer();
        }

        public void Visit(VariableNode node)
        {
            Log("VARIABLE:");
            Log($"- Name: {node.Name}");
            Log($"- Value: {node.Value}");
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
                case FuncNode funcNode:
                    funcNode.Accept(this);
                    break;
                case IdentifierNode identifierNode:
                    identifierNode.Accept(this);
                    break;
                case InitNode initNode:
                    initNode.Accept(this);
                    break;
                case LiteralNode literalNode:
                    literalNode.Accept(this);
                    break;
                case ModuleNode moduleNode:
                    moduleNode.Accept(this);
                    break;
                case PropertyNode propertyNode:
                    propertyNode.Accept(this);
                    break;
                case StructNode structNode:
                    structNode.Accept(this);
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
    }
}
