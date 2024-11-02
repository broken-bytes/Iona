using AST.Nodes;
using AST.Types;
using AST.Visitors;
using System.Xml.Linq;

namespace ASTVisualizer
{
    internal class ASTVisualizer : IASTVisualizer,
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
        IInitCallVisitor,
        IInitVisitor,
        ILiteralVisitor,
        IMemberAccessVisitor,
        IModuleVisitor,
        IObjectLiteralVisitor,
        IOperatorVisitor,
        IPropAccessVisitor,
        IPropertyVisitor,
        IReturnVisitor,
        ISelfVisitor,
        IStructVisitor,
        ITypeReferenceVisitor,
        IUnaryExpressionVisitor,
        IVariableVisitor
    {
        private string _output = "";
        private string _lastNodeId = "";

        public string Visualize(INode node)
        {
            // Use the mermaid syntax for flowcharts from top to bottom
            _output = "flowchart TB\n";
            GetAndLogNode(node);

            return _output;
        }

        public void Visit(AssignmentNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            if (node.Target != null)
            {
                GetAndLogNode(node.Target);
                _lastNodeId = GetNodeId(node);
                GetAndLogNode(node.Value);
                _lastNodeId = GetNodeId(node);
            }
        }

        public void Visit(BinaryExpressionNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            MakeConnection(node, "Left");
            GetAndLogNode(node.Left);
            _lastNodeId = GetNodeId(node);
            MakeConnection(node, "Right");
            GetAndLogNode(node.Right);
            _lastNodeId = GetNodeId(node);
        }

        public void Visit(BlockNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            foreach (var child in node.Children)
            {
                GetAndLogNode(child);
                _lastNodeId = GetNodeId(node);
            }
        }

        public void Visit(ClassNode node)
        {

        }

        public void Visit(ContractNode node)
        {

        }

        public void Visit(ErrorNode node)
        {

        }

        public void Visit(FileNode node)
        {
            _output += GetNodeRepresentation(node);
            _lastNodeId = GetNodeId(node);

            foreach (var child in node.Children)
            {
                if (child is ModuleNode moduleNode)
                {
                    GetAndLogNode(moduleNode);
                }
            }
        }

        public void Visit(FuncCallNode node)
        {
            
        }

        public void Visit(FuncNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            _lastNodeId = GetNodeId(node);
        }

        public void Visit(IdentifierNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
        }

        public void Visit(ImportNode import)
        {

        }

        public void Visit(InitCallNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            if (node.Args != null)
            {
                foreach (var arg in node.Args)
                {
                    GetAndLogNode(arg.Value);
                    MakeConnection(node, "Arg");
                    _lastNodeId = GetNodeId(node);
                }
            }
        }

        public void Visit(InitNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            _lastNodeId = GetNodeId(node);
        }

        public void Visit(LiteralNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
        }

        public void Visit(MemberAccessNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            MakeConnection(node, "Target");
            GetAndLogNode(node.Left);
            MakeConnection(node, "Member");
            GetAndLogNode(node.Right);
            _lastNodeId = GetNodeId(node);
        }

        public void Visit(ModuleNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            foreach (var child in node.Children)
            {
                GetAndLogNode(child);
                _lastNodeId = GetNodeId(node);
            }
        }

        public void Visit(ObjectLiteralNode node)
        {

        }

        public void Visit(OperatorNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            _lastNodeId = GetNodeId(node);
        }

        public void Visit(PropAccessNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            MakeConnection(node, "Object");
            GetAndLogNode(node.Object);
            _lastNodeId = GetNodeId(node);
            MakeConnection(node, "Property");
            GetAndLogNode(node.Property);
            _lastNodeId = GetNodeId(node);
        }

        public void Visit(PropertyNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            if (node.TypeNode != null)
            {
                MakeConnection(node, "Type");
                GetAndLogNode(node.TypeNode);
                _lastNodeId = GetNodeId(node);
            }
        }

        public void Visit(ReturnNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            if (node.Value != null)
            {
                MakeConnection(node, "Value");
                GetAndLogNode(node.Value);
                _lastNodeId = GetNodeId(node);
            }
        }

        public void Visit(SelfNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
        }

        public void Visit(StructNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            _lastNodeId = GetNodeId(node);
        }

        public void Visit(TypeReferenceNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
        }

        public void Visit(UnaryExpressionNode node)
        {

        }

        public void Visit(VariableNode node)
        {
            _output += GetNodeRepresentation(node);
            _output += $"{_lastNodeId} --> {GetNodeId(node)}\n";
            _lastNodeId = GetNodeId(node);

            if (node.TypeNode != null)
            {
                MakeConnection(node, "Type");
                GetAndLogNode(node.TypeNode);
                _lastNodeId = GetNodeId(node);
            }

            if (node.Value != null)
            {
                MakeConnection(node, "Value");
                GetAndLogNode(node.Value);
                _lastNodeId = GetNodeId(node);
            }
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
                case InitCallNode initCallNode:
                    initCallNode.Accept(this);
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
                case PropAccessNode propAccessNode:
                    propAccessNode.Accept(this);
                    break;
                case PropertyNode propertyNode:
                    propertyNode.Accept(this);
                    break;
                case ReturnNode returnNode:
                    returnNode.Accept(this);
                    break;
                case SelfNode selfNode:
                    selfNode.Accept(this);
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

        private string GetNodeId(INode node)
        {
            var id = "unknown_";
            switch (node)
            {
                case AssignmentNode:
                    id = "assignment_";
                    break;
                case BinaryExpressionNode:
                    id = "binary_expression_";
                    break;
                case BlockNode:
                    id = "block_";
                    break;
                case ClassNode:
                    id = "class_";
                    break;
                case ContractNode:
                    id = "contract_";
                    break;
                case ErrorNode:
                    id = "error_";
                    break;
                case FileNode:
                    id = "file_";
                    break;
                case FuncNode:
                    id = "func_";
                    break;
                case IdentifierNode:
                    id = "identifier_";
                    break;
                case InitNode:
                    id = "init_";
                    break;
                case LiteralNode:
                    id = "literal_";
                    break;
                case MemberAccessNode:
                    id = "member_access_";
                    break;
                case ModuleNode:
                    id = "module_";
                    break;
                case OperatorNode:
                    id = "operator_";
                    break;
                case PropertyNode:
                    id = "property_";
                    break;
                case ReturnNode returnNode:
                    id = "return_";
                    break;
                case SelfNode:
                    id = "self_";
                    break;
                case StructNode:
                    id = "struct_";
                    break;
                case TypeReferenceNode:
                    id = "type_reference_";
                    break;
                case VariableNode:
                    id = "variable_";
                    break;
            }

            return $"{id}_{node.Meta.LineStart}_{node.Meta.ColumnStart}_{node.Meta.ColumnEnd}";
        }

        private void MakeConnection(INode root, string name)
        {
            var connectionId = $"{GetNodeId(root)}_{name}";
            _output += $"{connectionId}" + "{" + name + "}\n";
            _output += $"{GetNodeId(root)} --> {connectionId}\n";
            _lastNodeId = connectionId;
        }

        private string GetNodeRepresentation(INode node)
        {
            var id = GetNodeId(node);
            var name = "";
            var type = "";

            switch (node)
            {
                case AssignmentNode:
                    name = "Assignment";
                    type = "assignment";
                    break;
                case BinaryExpressionNode binary:
                    name = binary.Operation.ToString();
                    type = "binary expression";
                    break;
                case BlockNode:
                    name = "Block";
                    type = "block";
                    break;
                case ClassNode:
                    name = ((ClassNode)node).Name;
                    type = "class";
                    break;
                case ContractNode:
                    name = ((ContractNode)node).Name;
                    type = "contract";
                    break;
                case ErrorNode:
                    name = ((ErrorNode)node).Message;
                    type = "error";
                    break;
                case FileNode:
                    name = "File";
                    type = "file";
                    break;
                case FuncNode:
                    name = Path.GetFileName(((FuncNode)node).Name);
                    type = "func";
                    break;
                case IdentifierNode:
                    name = ((IdentifierNode)node).Value;
                    type = "identifier";
                    break;
                case InitNode:
                    name = ((InitNode)node).Name;
                    type = "init";
                    break;
                case LiteralNode:
                    name = ((LiteralNode)node).Value;
                    type = "literal";
                    break;
                case MemberAccessNode:
                    name = "Member Access";
                    type = "member access";
                    break;
                case ModuleNode:
                    name = ((ModuleNode)node).Name;
                    type = "module";
                    break;
                case OperatorNode:
                    name = ((OperatorNode)node).Op.ToString();
                    type = "operator";
                    break;
                case PropertyNode:
                    name = ((PropertyNode)node).Name;
                    type = "property";
                    break;
                case StructNode:
                    name = ((StructNode)node).Name;
                    type = "struct";
                    break;
                case TypeReferenceNode:
                    name = ((TypeReferenceNode)node).FullyQualifiedName;
                    type = "type reference";
                    break;
                case VariableNode:
                    name = ((VariableNode)node).Name;
                    type = "variable";
                    break;
                default:
                    name = node.GetType().Name;
                    break;
            }

            return $"{id}[{type}: {name}]\n";
        }
    }
}
