using System.Diagnostics.CodeAnalysis;
using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Symbols;
using Symbols.Symbols;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq.Expressions;
using System.Text;
using Mono.Cecil.Rocks;
using Generator.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shared;

namespace Generator
{
    internal class AssemblyBuilder :
        IAssignmentVisitor,
        IBinaryExpressionVisitor,
        IBlockVisitor,
        IClassVisitor,
        IIdentifierVisitor,
        IInitCallVisitor,
        IInitVisitor,
        IFileVisitor,
        IFuncCallVisitor,
        IFuncVisitor,
        ILiteralVisitor,
        IModuleVisitor,
        IOperatorVisitor,
        IPropAccessVisitor,
        IPropertyVisitor,
        IReturnVisitor,
        IScopeResolutionVisitor,
        IStructVisitor,
        ITypeReferenceVisitor,
        IVariableVisitor
    {
        private readonly SymbolTable _table;
        private List<MetadataReference> references = new();
        private CompilationUnitSyntax freeFunctionsUnit;

        private StringBuilder source = new StringBuilder();

        internal AssemblyBuilder(SymbolTable table)
        {
            this._table = table;
        }

        internal CompilationUnitSyntax? Build(INode node)
        {
            source.Clear();
            if (node is FileNode file)
            {
                file.Accept(this);

                return CSharpSyntaxTree.ParseText(source.ToString()).GetRoot() as CompilationUnitSyntax;
            }

            return null;
        }

        public void Visit(AssignmentNode node)
        {
            HandleNode(node.Target);

            source.Append(node.AssignmentType.AssignmentString());

            HandleNode(node.Value);
        }

        public void Visit(BinaryExpressionNode node)
        {
            HandleNode(node.Left);
            
            source.Append(node.Operation.CSharpOperator());
            
            HandleNode(node.Right);
        }

        public void Visit(BlockNode node)
        {
            source.Append("{\n");
            foreach (var child in node.Children)
            {
                HandleNode(child);
            }

            source.Append("}");
        }

        public void Visit(ClassNode node)
        {
            source.Append(AccessLevelString(node.AccessLevel));
            source.Append($"class {node.Name}");

            if (node.BaseType != null || node.Contracts.Count > 0)
            {
                source.Append(":");
            }

            if (node.BaseType != null)
            {
                var typeSymbol = _table.FindTypeByFQN(node.BaseType.FullyQualifiedName);
                if (typeSymbol != null)
                {
                    var type = Shared.Utils.GetBoxedName(typeSymbol.FullyQualifiedName);

                    source.Append(type);
                }
            }

            // If the class has a body, visit it
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(FileNode node)
        {
            foreach (var child in node.Children)
            {
                if (child is ModuleNode module)
                {
                    module.Accept(this);
                }
                else if (child is ImportNode import)
                {
                    source.Append($"using {import.Name};\n");
                }
            }
        }

        public void Visit(FuncCallNode node)
        {
            // First, print the name of the function
            source.Append(Shared.Utils.IonaToCSharpName(node.Target.ILValue));
            if (node.GenericArgs.Any())
            {
                source.Append('<');
                foreach (var arg in node.GenericArgs)
                {
                    source.Append(arg.Name);
                    source.Append(", ");
                }
                source.Remove(source.Length - 2, 2);

                source.Append('>');
            }
            
            source.Append('(');

            if (node.Args.Any())
            {
                foreach (var arg in node.Args)
                {
                    HandleNode(arg.Value);
                    source.Append(", ");
                }

                source.Remove(source.Length - 2, 2);
            }

            source.Append(")");
        }

        public void Visit(FuncNode node)
        {
            if (node.ReturnType is not TypeReferenceNode type)
            {
                return;
            }
            
            var isFree = node.Parent?.Parent is not ITypeNode;
            var returnType = Shared.Utils.GetBoxedName(type.FullyQualifiedName);
            
            // The function is not part of a type and thus a free function
            if (isFree)
            {
                // C# does not support free functions, so we create a global "__free__" static class for each namespace
                // First, check if the free functions unit contains a namespace named liked the current one

                source.Append("public static partial class Module {\n");
                
                source.Append($"""
                /// <summary>
                /// This function is defined in {node.Meta.File} at line {node.Meta.LineStart}
                /// </summary>
                """);
                source.Append("\nstatic ");
            }

            source.Append(AccessLevelString(node.AccessLevel));
            
            source.Append(returnType);
            
            source.Append(' ');
            source.Append(Shared.Utils.IonaToCSharpName(node.Name));
            source.Append('(');

            // Add the parameters to the method
            foreach (var parameter in node.Parameters)
            {
                var paramType = Shared.Utils.GetBoxedName(parameter.TypeNode.FullyQualifiedName);
                source.Append($"{paramType} {parameter.Name}");
            }
            source.Append(')');

            node.Body?.Accept(this);

            if (isFree)
            {
                source.Append('}');
            }
        }

        public void Visit(IdentifierNode node)
        {
            source.Append(node.ILValue);
        }

        public void Visit(InitCallNode node)
        {
            source.Append($"new {node.TypeFullName}(");
            foreach (var arg in node.Args)
            {
                source.Append($"{arg.Value}, ");
            }
            
            source.Remove(source.Length - 2, 2);
            source.Append(")");
        }

        public void Visit(InitNode node)
        {
            source.Append(AccessLevelString(node.AccessLevel));

            if (node?.Parent?.Parent is not ITypeNode type)
            {
                return;
            }
            
            source.Append($"{type.Name}(");
            
            // Add the parameters to the method
            foreach (var parameter in node?.Parameters ?? [])
            {
                var paramType = Shared.Utils.GetBoxedName(parameter.TypeNode.FullyQualifiedName);
                source.Append($"{paramType} {parameter.Name}");
            }
            source.Append(')');

            node?.Body?.Accept(this);
        }

        public void Visit(LiteralNode node)
        {
            source.Append(node);
        }

        public void Visit(ModuleNode node)
        {
            source.Append($"namespace {node.Name}");
            source.AppendLine("{");
            foreach (var child in node.Children)
            {
                if (child is StructNode str)
                {
                    str.Accept(this);
                }
                else if (child is ClassNode clss)
                {
                    clss.Accept(this);
                }
                else if (child is FuncNode func)
                {
                    func.Accept(this);
                }
            }
            
            source.Append('}');
        }

        public void Visit(OperatorNode node)
        {
            // Get the type of the operator and generate the CIL operator overload
            var opType = node.Op;
            // Get the return type of the operator

            if (node.ReturnType is not TypeReferenceNode returnType)
            {
                return;
            }

            // Get the reference to the return type
            TypeReference? reference = null;

            reference = new TypeReference(
                returnType.Name,
                "",
                returnType.Assembly,
                returnType.TypeKind == Kind.Class || returnType.TypeKind == Kind.Contract
            );

            var boolRef = new TypeReference(
                "Bool",
                "Iona.Builtins",
                "Iona.Builtins",
                false
            );

            var methodAttributes = MethodAttributes.Public | MethodAttributes.Static;

            MethodDefinition? method = opType switch
            {
                OperatorType.Add => new MethodDefinition("op_Addition", methodAttributes, reference),
                OperatorType.Subtract => new MethodDefinition("op_Subtraction", methodAttributes, reference),
                OperatorType.Multiply => new MethodDefinition("op_Multiply", methodAttributes, reference),
                OperatorType.Divide => new MethodDefinition("op_Division", methodAttributes, reference),
                OperatorType.Modulo => new MethodDefinition("op_Modulus", methodAttributes, reference),
                OperatorType.Equal => new MethodDefinition("op_Equality", methodAttributes, boolRef),
                OperatorType.NotEqual => new MethodDefinition("op_Inequality", methodAttributes, boolRef),
                OperatorType.GreaterThan => new MethodDefinition("op_GreaterThan", methodAttributes, boolRef),
                OperatorType.GreaterThanOrEqual => new MethodDefinition("op_GreaterThanOrEqual", methodAttributes,
                    boolRef),
                OperatorType.LessThan => new MethodDefinition("op_LessThan", methodAttributes, boolRef),
                OperatorType.LessThanOrEqual => new MethodDefinition("op_LessThanOrEqual", methodAttributes, boolRef),
                _ => null
            };

            if (method == null)
            {
                return;
            }

            // Add the parameters to the method
            foreach (var param in node.Parameters)
            {
                var type = (TypeReferenceNode)param.TypeNode;
                TypeReference? paramReference = null;

                if (type == null)
                {
                    return;
                }

                reference = new TypeReference(
                    type.Name,
                    "",
                    type.Assembly,
                    type.TypeKind == Kind.Class || type.TypeKind == Kind.Contract
                );

                var def = new ParameterDefinition(param.Name, ParameterAttributes.None, paramReference);
                method.Parameters.Add(def);
            }

            
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(PropAccessNode node)
        {
            if (node.Object is SelfNode self)
            {
                source.Append("this");
            }
            else
            {
                source.Append(node.Object.ToString());
            }
            source.Append('.');
            
            if (node.Property is IdentifierNode identifier)
            {
                identifier.Accept(this);
            } 
            else if (node.Property is PropAccessNode property)
            {
                property.Accept(this);
            }
            else if (node.Property is FuncCallNode funcCall)
            {
                funcCall.Accept(this);
            }
        }

        public void Visit(PropertyNode node)
        {
            // Get the type of the prop
            var type = Shared.Utils.GetBoxedName(node.TypeNode.FullyQualifiedName);
            
            switch (node.AccessLevel)
            {
                case AccessLevel.Public:
                    source.Append("public ");
                    break;
                case AccessLevel.Private:
                    source.Append("private ");
                    break;
                default:
                    source.Append("internal ");
                    break;
            }
            
            // We have two cases here:
            // - The property is an actual property, that is it has get/set Methods
            // - The property has no get or set, making it essentially a field
            var isField = node.Set is null && node.Get is null;
            
            if (isField)
            {
                source.Append($"{type} {node.Name}");

                if (node.Value is not null)
                {
                    source.Append(" = ");
                }
                
                switch (node.Value)
                {
                    case PropAccessNode prop:
                        prop.Accept(this);
                        break;
                    case IdentifierNode ident:
                        ident.Accept(this);
                        break;
                    case LiteralNode literal:
                        literal.Accept(this);
                        break;
                }
            }
        }
        
        public void Visit(ReturnNode node)
        {
            source.Append("return ");

            if (node.Value != null)
            {
                if (node.Value is PropAccessNode prop)
                {
                    prop.Accept(this);
                }
                else if (node.Value is LiteralNode literal)
                {
                    literal.Accept(this);
                }
                else if (node.Value is BinaryExpressionNode binary)
                {
                    binary.Accept(this);
                }
            }
        }

        public void Visit(ScopeResolutionNode node)
        {
            source.Append(node.Scope.ToString());
            source.Append(".");
            
            if (node.Property is IdentifierNode identifier)
            {
                identifier.Accept(this);
            } 
            else if (node.Property is PropAccessNode property)
            {
                property.Accept(this);
            }
            else if (node.Property is FuncCallNode funcCall)
            {
                funcCall.Accept(this);
            }
            else if (node.Property is EnumCaseAccessNode caseAccess)
            {
                caseAccess.Case.Accept(this);
            }
        }

        public void Visit(StructNode node)
        {
        }

        public void Visit(TypeReferenceNode node)
        {

        }

        public void Visit(VariableNode node)
        {
            // TODO: Emit correct mutability level
            source.Append("var ");
            source.Append(node.Name);

            if (node.Value is not null)
            {
                source.Append(" = ");
                HandleNode(node.Value);
            }
        }
        
        private MemberAccessExpressionSyntax PropAccessToMemberAccess(PropAccessNode node)
        {
            return PropAccessToMemberAccessRecursive(node);
        }

        private MemberAccessExpressionSyntax PropAccessToMemberAccessRecursive(PropAccessNode node)
        {
            // Resolve the Object
            ExpressionSyntax objectExpression;

            if (node.Object is IdentifierNode identifier)
            {
                // If Object is an Identifier, create an IdentifierNameSyntax
                objectExpression = SyntaxFactory.IdentifierName(identifier.Value);
            }
            else if (node.Object is SelfNode) // Assuming SelfNode represents 'this'
            {
                objectExpression = SyntaxFactory.ThisExpression();
            }
            else
            {
                throw new InvalidOperationException("Unsupported object type.");
            }

            // Create the MemberAccessExpression for the Property
            if (node.Property is PropAccessNode propertyAccess)
            {
                // If Property is another PropAccessNode, resolve it recursively
                var propertyExpression = PropAccessToMemberAccessRecursive(propertyAccess);
                return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, objectExpression, propertyExpression.Name);
            }
            else if (node.Property is IdentifierNode propertyIdentifier)
            {
                // If Property is an Identifier, create a MemberAccessExpression
                return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, objectExpression, SyntaxFactory.IdentifierName(propertyIdentifier.Value));
            }
            else
            {
                throw new InvalidOperationException("Unsupported property type.");
            }
        }

        private LiteralExpressionSyntax? LiteralToLiteralExpression(LiteralNode node)
        {
            switch (node.LiteralType)
            {
                case LiteralType.Boolean:
                    return node.Value == "true" ? 
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression) :
                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                    break;
                case LiteralType.Char:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression)
                        .Update(SyntaxFactory.Literal(node.Value));
                case LiteralType.Double:
                case LiteralType.Float:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression, 
                        SyntaxFactory.Literal(double.Parse(node.Value))
                        );
                case LiteralType.Integer:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression)
                        .Update(SyntaxFactory.Literal(int.Parse(node.Value)));
                case LiteralType.Null:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
                case LiteralType.String:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression)
                        .Update(SyntaxFactory.Literal(node.Value));
            }

            return null;
        }

        private BinaryExpressionSyntax? BinaryExpressionToBinaryExpressionSyntax(BinaryExpressionNode node)
        {
            ExpressionSyntax? left = null;
            if (node.Left is IdentifierNode leftIdentifier)
            {
                left = SyntaxFactory.IdentifierName(leftIdentifier.Value);
            }
            else if (node.Left is PropAccessNode leftProp)
            {
                left = PropAccessToMemberAccess(leftProp);
            }
            else if (node.Left is LiteralNode leftLiteral)
            {
                left = LiteralToLiteralExpression(leftLiteral);
            }
            
            ExpressionSyntax? right = null;
            if (node.Left is IdentifierNode rightIdentifier)
            {
                right = SyntaxFactory.IdentifierName(rightIdentifier.Value);
            }
            else if (node.Left is PropAccessNode rightProp)
            {
                right = PropAccessToMemberAccess(rightProp);
            }
            else if (node.Left is LiteralNode rightLiteral)
            {
                right = LiteralToLiteralExpression(rightLiteral);
            }

            SyntaxKind kind = SyntaxKind.None;
            if (node.Operation == BinaryOperation.Add)
            {
                kind = SyntaxKind.AddExpression;
            }
            else if (node.Operation == BinaryOperation.Subtract)
            {
                kind = SyntaxKind.SubtractExpression;
            }
            else if (node.Operation == BinaryOperation.Multiply)
            {
                kind = SyntaxKind.MultiplyExpression;
            }
            else if (node.Operation == BinaryOperation.Divide)
            {
                kind = SyntaxKind.DivideExpression;
            }
            else if (node.Operation == BinaryOperation.Mod)
            {
                kind = SyntaxKind.ModuloExpression;
            }
            
            if (left != null && right != null)
            {
                return SyntaxFactory.BinaryExpression(kind, left, right);
            }
            
            return null;
        }

        private static string AccessLevelString(AccessLevel accessLevel)
        {
            return accessLevel switch
            {
                AccessLevel.Public => "public ",
                AccessLevel.Private => "private ",
                _ => "internal "
            };
        }

        private void HandleNode(INode node)
        {
            if (node.Status == INode.ResolutionStatus.Failed)
            {
                return;
            }
            
            switch (node)
            {
                case AssignmentNode assignment:
                    assignment.Accept(this);
                    break;
                case BinaryExpressionNode binary:
                    binary.Accept(this);
                    break;
                case BlockNode block:
                    block.Accept(this);
                    break;
                case FuncCallNode funcCall:
                    funcCall.Accept(this);
                    break;
                case FuncNode func:
                    func.Accept(this);
                    break;
                case IdentifierNode identifier:
                    identifier.Accept(this);
                    break;
                case InitCallNode initCall:
                    initCall.Accept(this);
                    break;
                case InitNode init:
                    init.Accept(this);
                    break;
                case LiteralNode literalNode:
                    literalNode.Accept(this);
                    break;
                case OperatorNode op:
                    op.Accept(this);
                    break;
                case PropAccessNode propAccess:
                    propAccess.Accept(this);
                    break;
                case PropertyNode property:
                    property.Accept(this);
                    break;
                case ReturnNode ret:
                    ret.Accept(this);
                    break;
                case ScopeResolutionNode scope:
                    scope.Accept(this);
                    break;
                case VariableNode variable:
                    variable.Accept(this);
                    break;
            }

            if (node.Parent is BlockNode blockNode && node is not FuncNode or OperatorNode or BlockNode)
            {
                source.Append(";\n");
            }
        }
    }
}
