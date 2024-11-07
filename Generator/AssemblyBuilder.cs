using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Symbols;
using Symbols.Symbols;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq.Expressions;
using Mono.Cecil.Rocks;
using Generator.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        internal AssemblyBuilder(SymbolTable table)
        {
            this.table = table;
        }

        internal CompilationUnitSyntax? Build(INode node, ref CompilationUnitSyntax freeFunctionsUnit)
        {
            List<CompilationUnitSyntax> compilations = new();
            this.freeFunctionsUnit = freeFunctionsUnit;

            if (node is FileNode file)
            {
                file.Accept(this);

               
                freeFunctionsUnit = this.freeFunctionsUnit;

                return compilationUnit;
            }

            return null;
        }

        public void Visit(AssignmentNode node)
        {
            if (currentMethod == null)
            {
                return;
            }

            ExpressionSyntax? left = null;
            if (node.Target is PropAccessNode propLeft)
            {
                left = PropAccessToMemberAccess(propLeft);
            }
            else if (node.Target is IdentifierNode ident)
            {
                left = SyntaxFactory.IdentifierName(ident.Value);
            }

            ExpressionSyntax? right = null;
            if (node.Value is PropAccessNode propRight)
            {
                right = PropAccessToMemberAccess(propRight);
            }
            else if (node.Value is IdentifierNode ident)
            {
                right = SyntaxFactory.IdentifierName(ident.Value);
            }
            else if (node.Value is LiteralNode literal)
            {
                right = LiteralToLiteralExpression(literal);
            }

            if (left != null && right != null)
            {
                var expression = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right);
                var statement = SyntaxFactory.ExpressionStatement(expression);
                currentMethod = currentMethod.AddBodyStatements(statement);
            }
        }
        
        public void Visit(BinaryExpressionNode node)
        {
            if (currentMethod == null)
            {
                return;
            }
            
            if (node.Left is IdentifierNode left)
            {
                EmitGetIdentifier(left);
            }
            else if (node.Left is PropAccessNode propAccess)
            {
                EmitGetPropAccess(propAccess);
            }
            else
            {
                return;
            }
            
            if (node.Right is IdentifierNode right)
            {
                EmitGetIdentifier(right);
            }
            else if (node.Right is PropAccessNode propAccess)
            {
                EmitGetPropAccess(propAccess);
            }
        }

        public void Visit(BlockNode node)
        {
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
        }

        public void Visit(ClassNode node)
        {
            if (currentNamespace == null)
            {
                return;
            }
            
            currentType = SyntaxFactory.ClassDeclaration(node.Name);
            
            if (node.AccessLevel == AccessLevel.Public)
            {
                currentType = currentType.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            }
            else if (node.AccessLevel == AccessLevel.Private)
            {
                currentType = currentType.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            }
            else
            {
                currentType = currentType.AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
            }

            // If the struct has a body, visit it
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            currentNamespace = currentNamespace.AddMembers(currentType);

            currentType = null;
        }

        public void Visit(FileNode node)
        {
            compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(
                    SyntaxFactory.UsingDirective(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.IdentifierName("Iona"), 
                            SyntaxFactory.IdentifierName("Builtins")
                            )
                        )
                    );
            
            foreach (var child in node.Children)
            {
                if (child is ModuleNode module)
                {
                    module.Accept(this);
                }
            }
        }

        public void Visit(FuncNode node)
        {
            // Functions may only exist in types or namespaces
            if (currentType == null && currentNamespace == null)
            {
                return;
            }

            if (node.ReturnType == null)
            {
                return;
            }

            var returnType = GetBoxedName(node.ReturnType.FullyQualifiedName);

            currentMethod = SyntaxFactory.MethodDeclaration(returnType, node.Name);

            if (node.AccessLevel == AccessLevel.Public)
            {
                currentMethod = currentMethod.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            }
            else if (node.AccessLevel == AccessLevel.Private)
            {
                currentMethod = currentMethod.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            }
            else
            {
                currentMethod = currentMethod.AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
            }

            // Add the parameters to the method
            foreach (var parameter in node.Parameters)
            {
                var type = GetBoxedName(parameter.TypeNode.FullyQualifiedName);
                var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameter.Name))
                    .WithType(type);
                
                currentMethod = currentMethod.AddParameterListParameters(parameterSyntax);
            }
            
            if (node.Body != null)
            {
                currentMethod = currentMethod.WithBody(SyntaxFactory.Block());
                node.Body.Accept(this);
            }

            if (currentType != null)
            {
                currentType = currentType.AddMembers(currentMethod);
            }
            else if (currentNamespace != null)
            {
                // C# does not support free functions, so we create a global "__free__" static class for each namespace
                // First, check if the free functions unit contains a namespace named liked the current one
                var freeNamespace = freeFunctionsUnit.Members.OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault(ns => ns.Name == currentNamespace.Name);

                if (freeNamespace == null)
                {
                    freeNamespace = SyntaxFactory.NamespaceDeclaration(currentNamespace.Name);
                }
                else
                {
                    // Remove the namespace from the compialtion
                    freeFunctionsUnit.Members.Remove(freeNamespace);
                }
                
                var classDecl = freeNamespace.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault();

                if (classDecl == null)
                {
                    classDecl = SyntaxFactory.ClassDeclaration("__free__")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                }

                var doc = $@"
                /// <summary>
                /// This function is defined in {node.Meta.File} at line {node.Meta.LineStart}
                /// </summary>
                ";
                currentMethod = currentMethod.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                    .WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(doc));
                classDecl = classDecl.AddMembers(currentMethod);

                freeNamespace = freeNamespace.AddMembers(classDecl);

                freeFunctionsUnit = freeFunctionsUnit.AddMembers(freeNamespace);
            }

            currentMethod = null;
        }

        public void Visit(IdentifierNode node)
        {
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
            if (currentType == null)
            {
                return;
            }

            var typeRef = new TypeReference(
                "Void",
                "Iona.Builtins",
                "Iona.Builtins",
                false
            );

            var method = new MethodDefinition(
                ".ctor",
                MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, 
                typeRef
            );

            switch (node.AccessLevel)
            {
                case AST.Types.AccessLevel.Public:
                    method.Attributes |= MethodAttributes.Public;
                    break;
                case AST.Types.AccessLevel.Private:
                    method.Attributes |= MethodAttributes.Private;
                    break;
                default:
                    method.Attributes |= MethodAttributes.Private;
                    break;
            }

            currentMethod = SyntaxFactory.ConstructorDeclaration(currentType.Identifier.Text);

            if (node.AccessLevel == AST.Types.AccessLevel.Private)
            {
                currentMethod = currentMethod.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            }
            else if (node.AccessLevel == AST.Types.AccessLevel.Public)
            {
                currentMethod = currentMethod.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            }
            else
            {
                currentMethod = currentMethod.AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
            }
            
            // Add the parameters to the method
            foreach (var parameter in node.Parameters)
            {
                var type = GetBoxedName(parameter.TypeNode.FullyQualifiedName);
                var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameter.Name))
                    .WithType(type);
                
                currentMethod = currentMethod.AddParameterListParameters(parameterSyntax);
            }
            
            if (node.Body != null)
            {
                currentMethod = currentMethod.WithBody(SyntaxFactory.Block());
                node.Body.Accept(this);
            }
            
            // Create the constructor
            currentType = currentType.AddMembers(
                currentMethod
            );
        }

        public void Visit(ModuleNode node)
        {
            var csModule = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(node.Name));
            currentNamespace = csModule;
            
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
            
            compilationUnit = compilationUnit.AddMembers(currentNamespace);
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

        }

        public void Visit(PropertyNode node)
        {
            if (currentType == null)
            {
                return;
            }
            
            // Get the type of the prop
            var type = GetBoxedName(node.TypeNode.FullyQualifiedName);
            
            // We have two cases here:
            // - The property is an actual property, that is it has get/set Methods
            // - The property has no get or set, making it essentially a field
            var isField = node.Set is null && node.Get is null;

            if (isField)
            {
                var fieldDeclaration = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(type)
                        .AddVariables(SyntaxFactory.VariableDeclarator($"{node.Name}")));

                if (node.AccessLevel != AccessLevel.Public)
                {
                    fieldDeclaration = fieldDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                } else if (node.AccessLevel == AccessLevel.Private)
                {
                    fieldDeclaration = fieldDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                }
                else
                {
                    fieldDeclaration = fieldDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                }
                
                currentType = currentType.AddMembers(fieldDeclaration);
            }
        }

        public void Visit(ReturnNode node)
        {
            if (currentMethod == null)
            {
                return;
            }

            ExpressionSyntax? value = null;
            if (node.Value is PropAccessNode prop)
            {
                value = PropAccessToMemberAccess(prop);
            }
            else if (node.Value is IdentifierNode ident)
            {
                value = SyntaxFactory.IdentifierName(ident.Value);
            }
            else if (node.Value is LiteralNode literal)
            {
                value = LiteralToLiteralExpression(literal);
            }
            else if (node.Value is BinaryExpressionNode binary)
            {
                value = BinaryExpressionToBinaryExpressionSyntax(binary);
            }

            if (value != null)
            {
                var returnSyntax = SyntaxFactory.ReturnStatement(value);

                currentMethod = currentMethod.AddBodyStatements(returnSyntax);
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

        private static NameSyntax ResolveQualifiedName(string name)
        {
            var fqnSplit = name.Split('.').ToList();
            
            NameSyntax nameSyntax = SyntaxFactory.IdentifierName(fqnSplit[0]);

            fqnSplit.RemoveAt(0);
            
            while (fqnSplit.Count > 0)
            {
                nameSyntax = SyntaxFactory.QualifiedName(nameSyntax, SyntaxFactory.IdentifierName(fqnSplit[0]));

                if (fqnSplit.Count > 0)
                {
                    fqnSplit.RemoveAt(0);
                }
            }

            return nameSyntax;
        }

        /// <summary>
        /// Used to get the boxed type. This automatically converts Iona's Builtin Types like `Int` to the C# primitive `int` etc.
        /// </summary>
        /// <param name="name">The Full name of the type</param>
        /// <returns></returns>
        private static NameSyntax GetBoxedName(string fullName)
        {
            switch (fullName)
            {
                case "Iona.Builtins.Bool":
                    return SyntaxFactory.IdentifierName("bool");
                case "Iona.Builtins.Double":
                    return SyntaxFactory.IdentifierName("double");
                case "Iona.Builtins.Float":
                    return SyntaxFactory.IdentifierName("float");
                case "Iona.Builtins.Int8":
                    return SyntaxFactory.IdentifierName("sbyte");
                case "Iona.Builtins.Int16":
                    return SyntaxFactory.IdentifierName("short");
                case "Iona.Builtins.Int32":
                    return SyntaxFactory.IdentifierName("int");
                case "Iona.Builtins.Int64":
                    return SyntaxFactory.IdentifierName("long");
                case "Iona.Builtins.Int":
                    return SyntaxFactory.IdentifierName("nint");
                case "Iona.Builtins.UInt8":
                    return SyntaxFactory.IdentifierName("byte");
                case "Iona.Builtins.UInt16":
                    return SyntaxFactory.IdentifierName("ushort");
                case "Iona.Builtins.UInt32":
                    return SyntaxFactory.IdentifierName("uint");
                case "Iona.Builtins.UInt64":
                    return SyntaxFactory.IdentifierName("ulong");
                case "Iona.Builtins.UInt":
                    return SyntaxFactory.IdentifierName("nuint");
                case "Iona.Builtins.String":
                    return SyntaxFactory.IdentifierName("string");
                case "Iona.Builtins.Void":
                    return SyntaxFactory.IdentifierName("void");
            }
            
            return ResolveQualifiedName(fullName);
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
