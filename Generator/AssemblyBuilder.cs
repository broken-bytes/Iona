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
        IFuncVisitor,
        ILiteralVisitor,
        IModuleVisitor,
        IOperatorVisitor,
        IPropAccessVisitor,
        IPropertyVisitor,
        IReturnVisitor,
        IStructVisitor,
        ITypeReferenceVisitor,
        IVariableVisitor
    {
        private readonly SymbolTable table;
        private CompilationUnitSyntax compilationUnit;
        private AssemblyDefinition? assembly;
        private TypeDeclarationSyntax? currentType;
        private BaseMethodDeclarationSyntax? currentMethod;
        private NamespaceDeclarationSyntax? currentNamespace;
        private List<MetadataReference> references = new();
        private CompilationUnitSyntax freeFunctionsUnit;

        private StringBuilder source = new StringBuilder();

        internal AssemblyBuilder(SymbolTable table)
        {
            this.table = table;
        }

        internal CompilationUnitSyntax? Build(INode node)
        {
            if (node is FileNode file)
            {
                file.Accept(this);

                return CSharpSyntaxTree.ParseText(source.ToString()).GetRoot() as CompilationUnitSyntax;
            }

            return null;
        }

        public void Visit(AssignmentNode node)
        {
            if (node.Target is PropAccessNode propLeft)
            {
                propLeft.Accept(this);
            }
            else if (node.Target is IdentifierNode ident)
            {
                ident.Accept(this);
            }

            switch (node.AssignmentType)
            {
                case AssignmentType.Assign:
                    source.Append(" = ");
                    break;
                case AssignmentType.AddAssign:
                    source.Append(" += ");
                    break;
            }

            if (node.Value is PropAccessNode propRight)
            {
                propRight.Accept(this);
            }
            else if (node.Value is IdentifierNode ident)
            {
               ident.Accept(this);
            }
            else if (node.Value is LiteralNode literal)
            {
                literal.Accept(this);
            }

            source.Append(";\n");
        }
        
        public void Visit(BinaryExpressionNode node)
        {
            if (currentMethod == null)
            {
                return;
            }
            
            if (node.Left is IdentifierNode left)
            {
                left.Accept(this);
            }
            else if (node.Left is PropAccessNode propAccess)
            {
                propAccess.Accept(this);
            }
            else
            {
                return;
            }
            
            if (node.Right is IdentifierNode right)
            {
                right.Accept(this);
            }
            else if (node.Right is PropAccessNode propAccess)
            {
                propAccess.Accept(this);
            }
        }

        public void Visit(BlockNode node)
        {
            source.Append("{\n");
            foreach (var child in node.Children)
            {
                if (child is PropertyNode property)
                {
                    property.Accept(this);
                }
                else if (child is InitNode init)
                {
                    init.Accept(this);
                }
                else if (child is BlockNode block)
                {
                    block.Accept(this);
                }
                else if (child is AssignmentNode assignment)
                {
                    assignment.Accept(this);
                }
                else if (child is OperatorNode op)
                {
                    op.Accept(this);
                }
                else if (child is BinaryExpressionNode binary)
                {
                    binary.Accept(this);
                }
                else if (child is ReturnNode ret)
                {
                    ret.Accept(this);
                }
                else if (child is VariableNode variable)
                {
                    variable.Accept(this);
                }
                else if (child is FuncNode func)
                {
                    func.Accept(this);
                }
            }
            
            source.Append("}");
        }

        public void Visit(ClassNode node)
        {
            if (node.AccessLevel == AccessLevel.Public)
            {
                source.Append("public ");
            }
            else if (node.AccessLevel == AccessLevel.Private)
            {
                source.Append("private ");
            }
            else
            {
                source.Append("internal ");
            }
            
            source.Append($"class {node.Name}");

            if (node.BaseType != null || node.Contracts.Count > 0)
            {
                source.Append(":");
            }
            
            if (node.BaseType != null)
            {
                var typeSymbol = table.FindTypeByFQN(node.BaseType.FullyQualifiedName);
                if (typeSymbol != null)
                {
                    var type = GetBoxedName(typeSymbol.FullyQualifiedName);

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

        public void Visit(FuncNode node)
        {
            bool isFree = node?.Parent?.Parent is not ITypeNode;
            var returnType = GetBoxedName(node.ReturnType.FullyQualifiedName);
            
            // The function is not part of a type and thus a free function
            if (isFree)
            {
                // C# does not support free functions, so we create a global "__free__" static class for each namespace
                // First, check if the free functions unit contains a namespace named liked the current one

                source.Append("public static partial class Module {\n");
                
                source.Append(@"
                /// <summary>
                /// This function is defined in {node.Meta.File} at line {node.Meta.LineStart}
                /// </summary>
                ");
                source.Append("\nstatic ");
            }
            
            if (node.AccessLevel == AccessLevel.Public)
            {
                source.Append("public ");
            }
            else if (node.AccessLevel == AccessLevel.Private)
            {
                source.Append("private ");
            }
            else
            {
                source.Append("internal ");
            }
            
            source.Append(returnType);
            
            source.Append(" ");
            source.Append(Utils.IonaToCSharpName(node.Name));
            source.Append("(");

            // Add the parameters to the method
            foreach (var parameter in node.Parameters)
            {
                var paramType = GetBoxedName(parameter.TypeNode.FullyQualifiedName);
                source.Append($"{paramType} {parameter.Name}");
            }
            source.Append(")");
            
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            if (isFree)
            {
                source.Append("}");
            }
        }

        public void Visit(IdentifierNode node)
        {
            source.Append(node.Value);
        }

        public void Visit(InitCallNode node)
        {
            if (assembly == null || currentMethod == null)
            {
                return;
            }

            var type = (TypeReferenceNode)node.ResultType;

            if (type == null)
            {
                return;
            }

            var module = ((INode)type).Module;


            var init = table.FindTypeByFQN(type.FullyQualifiedName)
                .Symbols
                .OfType<BlockSymbol>()
                .First()
                .Symbols
                .OfType<InitSymbol>().Where(init => { 
                    var parameters = init.Symbols.OfType<ParameterSymbol>().ToList();

                    if (parameters.Count != node.Args.Count)
                    {
                        return false;
                    }

                    for (int i = 0; i < parameters.Count; i++)
                    {
                        if (parameters[i].Type.FullyQualifiedName != node.Args[i].Value.ResultType?.FullyQualifiedName)
                        {
                            return false;
                        }
                    }

                    return true;
                });
        }

        public void Visit(InitNode node)
        {
            if (node.AccessLevel == AST.Types.AccessLevel.Private)
            {
                source.Append("private ");
            }
            else if (node.AccessLevel == AST.Types.AccessLevel.Public)
            {
                source.Append("public ");
            }
            else
            {
                source.Append("internal ");
            }

            var type = node.Parent.Parent as ITypeNode;
            
            source.Append($"{type.Name}(");
            
            // Add the parameters to the method
            foreach (var parameter in node.Parameters)
            {
                var paramType = GetBoxedName(parameter.TypeNode.FullyQualifiedName);
                source.Append($"{paramType} {parameter.Name}");
            }
            source.Append(")");
            
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(LiteralNode node)
        {
            source.Append(node.Value);
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
            
            source.Append("}");
        }

        public void Visit(OperatorNode node)
        {
            if (currentType == null)
            {
                return;
            }

            // Get the type of the operator and generate the CIL operator overload
            var opType = node.Op;
            // Get the return type of the operator
            var returnType = (TypeReferenceNode)node.ReturnType;

            // Get the reference to the return type
            TypeReference? reference = null;

            if (returnType == null)
            {
                return;
            }

            reference = new TypeReference(
                returnType.Name,
                NamespaceOf(returnType.FullyQualifiedName),
                returnType.Assembly,
                returnType.TypeKind == Kind.Class || returnType.TypeKind == Kind.Contract
            );

            var boolRef = new TypeReference(
                "Bool",
                "Iona.Builtins",
                "Iona.Builtins",
                false
            );

            MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.Static;

            MethodDefinition? method = null;

            if (opType == OperatorType.Add)
            {
                method = new MethodDefinition("op_Addition", methodAttributes, reference);
            }
            else if (opType == OperatorType.Subtract)
            {
                method = new MethodDefinition("op_Subtraction", methodAttributes, reference);
            }
            else if (opType == OperatorType.Multiply)
            {
                method = new MethodDefinition("op_Multiply", methodAttributes, reference);
            }
            else if (opType == OperatorType.Divide)
            {
                method = new MethodDefinition("op_Division", methodAttributes, reference);
            }
            else if (opType == OperatorType.Modulo)
            {
                method = new MethodDefinition("op_Modulus", methodAttributes, reference);
            }
            else if (opType == OperatorType.Equal)
            {
                method = new MethodDefinition("op_Equality", methodAttributes, boolRef);
            }
            else if (opType == OperatorType.NotEqual)
            {
                method = new MethodDefinition("op_Inequality", methodAttributes, boolRef);
            }
            else if (opType == OperatorType.GreaterThan)
            {
                method = new MethodDefinition("op_GreaterThan", methodAttributes, boolRef);
            }
            else if (opType == OperatorType.GreaterThanOrEqual)
            {
                method = new MethodDefinition("op_GreaterThanOrEqual", methodAttributes, boolRef);
            }
            else if (opType == OperatorType.LessThan)
            {
                method = new MethodDefinition("op_LessThan", methodAttributes, boolRef);
            }
            else if (opType == OperatorType.LessThanOrEqual)
            {
                method = new MethodDefinition("op_LessThanOrEqual", methodAttributes, boolRef);
            }

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
                    NamespaceOf(type.FullyQualifiedName),
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
            source.Append(".");
            
            if (node.Property is IdentifierNode identifier)
            {
                identifier.Accept(this);
            } 
            else if (node.Property is PropAccessNode property)
            {
                property.Accept(this);
            }
        }

        public void Visit(PropertyNode node)
        {
            // Get the type of the prop
            var type = GetBoxedName(node.TypeNode.FullyQualifiedName);
            
            if (node.AccessLevel == AccessLevel.Public)
            {
                source.Append("public ");
            }
            else if (node.AccessLevel == AccessLevel.Private)
            {
                source.Append("private ");
            }
            else
            {
                source.Append("internal ");
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
                
                if (node.Value is PropAccessNode prop)
                {
                    prop.Accept(this);
                } 
                else if (node.Value is IdentifierNode ident)
                {
                    ident.Accept(this);
                }
                else if (node.Value is LiteralNode literal)
                {
                    literal.Accept(this);
                }
                
                source.Append(";");
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

        public void Visit(StructNode node)
        {
        }

        public void Visit(TypeReferenceNode node)
        {

        }

        public void Visit(VariableNode node)
        {
        }

        // ---- Helper methods ----
        private void SetTypeLayout(TypeDefinition type, int size)
        {
        }

        private void AddFieldOffset(FieldDefinition field, int size)
        {
        }

        private void EmitGetIdentifier(IdentifierNode node)
        {
        }

        private void EmitSetIdentifier(IdentifierNode node)
        {
        }

        void EmitSetProperty(PropertyNode prop)
        {

        }

        private void EmitGetPropAccess(PropAccessNode node)
        {
        }

        private string NamespaceOf(string name)
        {
            return name.Substring(0, name.LastIndexOf('.'));
        }

        private PropertyDefinition? GetProperty(PropAccessNode node)
        {
            return null;
        }

        private TypeReference? GetTypeReference(TypeSymbol type)
        {
            return null;
        }

        /// <summary>
        /// Used to get the boxed type. This automatically converts Iona's Builtin Types like `Int` to the C# primitive `int` etc.
        /// </summary>
        /// <param name="name">The Full name of the type</param>
        /// <returns></returns>
        private static string GetBoxedName(string fullName)
        {
            switch (fullName)
            {
                case "Iona.Builtins.Bool":
                    return "bool";
                case "Iona.Builtins.Double":
                    return "double";
                case "Iona.Builtins.Float":
                    return "float";
                case "Iona.Builtins.Int8":
                    return "sbyte";
                case "Iona.Builtins.Int16":
                    return "short";
                case "Iona.Builtins.Int32":
                    return "int";
                case "Iona.Builtins.Int64":
                    return "long";
                case "Iona.Builtins.Int":
                    return "nint";
                case "Iona.Builtins.UInt8":
                    return "byte";
                case "Iona.Builtins.UInt16":
                    return "ushort";
                case "Iona.Builtins.UInt32":
                    return "uint";
                case "Iona.Builtins.UInt64":
                    return "ulong";
                case "Iona.Builtins.UInt":
                    return "nuint";
                case "Iona.Builtins.String":
                    return "string";
                case "Iona.Builtins.Void":
                    return "void";
            }

            return fullName;
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
    }
}
