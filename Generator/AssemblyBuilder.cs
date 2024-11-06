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
        private MethodDefinition currentMethod;
        private NamespaceDeclarationSyntax? currentNamespace;
        private List<MetadataReference> references = new();

        internal AssemblyBuilder(SymbolTable table)
        {
            this.table = table;
            
            // Load all the Standard Library assemblies per default
            var sdkPath = Environment.GetEnvironmentVariable("IONA_SDK_DIR");
            MetadataReference ionaBuiltinsReference = MetadataReference.CreateFromFile($"{sdkPath}/Iona.Builtins.dll");
            references.Add(ionaBuiltinsReference);
        }

        internal CompilationUnitSyntax Build(INode node)
        {
            if (node is FileNode file)
            {
                file.Accept(this);
            }

            return compilationUnit;
        }

        public void Visit(AssignmentNode node)
        {
            if (currentMethod == null)
            {
                return;
            }

            Action? loadValue = null;

            // First, handle the value to be assigned
            if (node.Value is IdentifierNode value)
            {
                // Check if the value is a variable, parameter or property
                loadValue = () =>
                {
                    var symbol = table.FindBy(value);

                    if (symbol is VariableSymbol variable)
                    {
                        var index = variable.Parent.Symbols.OfType<VariableSymbol>().ToList().FindIndex(symbol => symbol.Name == variable.Name);
                    }
                    else if (symbol is ParameterSymbol parameter)
                    {
                        var index = parameter.Parent.Symbols.OfType<ParameterSymbol>().ToList().FindIndex(symbol => symbol.Name == parameter.Name);

                        if (!currentMethod.IsStatic)
                        {
                            index++;
                        }

                    }
                    else if (symbol is PropertySymbol property)
                    {
                        // Get the property from the struct
                    }
                };
            }
            else if (node.Value is LiteralNode literal)
            {
            }
            else if (node.Value is PropAccessNode propAccess)
            {
                loadValue = () => EmitGetPropAccess(propAccess);
            }
            else if (node.Value is BinaryExpressionNode bin)
            {
                loadValue = () => bin.Accept(this); // Evaluate the binary expression
            }
            else if (node.Value is InitCallNode init)
            {
                loadValue = () => init.Accept(this); // Handle initialization
            }
            // Now handle the target where the value will be assigned
            if (node.Target is IdentifierNode target)
            {
                // TODO: Find out if the target is a property, variable or parameter
            }
            else if (node.Target is PropAccessNode propAccess)
            {
                var prop = GetProperty(propAccess);

                if (prop != null)
                {
                    if (propAccess.Object is SelfNode self)
                    {
                    }

                    loadValue?.Invoke();

                    //emitter.SetProperty(prop);
                }
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
            }
        }

        public void Visit(ClassNode node)
        {
            if (currentNamespace == null)
            {
                return;
            }

            TypeAttributes typeAttributes;

            switch (node.AccessLevel)
            {
                case AST.Types.AccessLevel.Public:
                    typeAttributes = TypeAttributes.Public;
                    break;
                case AST.Types.AccessLevel.Private:
                    typeAttributes = TypeAttributes.NotPublic;
                    break;
                default:
                    typeAttributes = TypeAttributes.NotPublic;
                    break;
            }
            
            var csClassNode = SyntaxFactory.ClassDeclaration(node.Name);
            
            currentType = csClassNode;

            // If the struct has a body, visit it
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            currentNamespace = currentNamespace.AddMembers(currentType);
        }

        public void Visit(FileNode node)
        {
            compilationUnit = SyntaxFactory.CompilationUnit();
            foreach (var child in node.Children)
            {
                if (child is ModuleNode module)
                {
                    module.Accept(this);
                }
            }
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
            if (assembly == null || currentType == null)
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
            
            // Add the parameters to the method
            foreach (var parameter in node.Parameters)
            {
                var type = (TypeReferenceNode)parameter.TypeNode;
                TypeReference? reference = null;

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

                var def = new ParameterDefinition(parameter.Name, ParameterAttributes.None, reference);
                method.Parameters.Add(def);
            }

            currentMethod = method;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
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

            currentMethod = method;
            
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
            if (assembly == null || currentType == null)
            {
                return;
            }
        }

        public void Visit(ReturnNode node)
        {
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
    }
}
