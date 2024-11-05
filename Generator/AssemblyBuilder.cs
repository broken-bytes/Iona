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
        private readonly ILEmitter emitter;
        private string currentNamespace = "";
        private AssemblyDefinition? assembly;
        private ModuleDefinition? currentModule;
        private TypeDefinition? currentType;
        private MethodDefinition currentMethod;

        internal AssemblyBuilder(SymbolTable table, ILEmitter emitter, AssemblyDefinition assembly)
        {
            this.table = table;
            this.emitter = emitter;
            this.assembly = assembly;
        }

        internal void Build(INode node)
        {
            if (node is FileNode file)
            {
                file.Accept(this);
            }
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
                        emitter.GetVariable(index);
                    }
                    else if (symbol is ParameterSymbol parameter)
                    {
                        var index = parameter.Parent.Symbols.OfType<ParameterSymbol>().ToList().FindIndex(symbol => symbol.Name == parameter.Name);

                        if (!currentMethod.IsStatic)
                        {
                            index++;
                        }

                        emitter.GetArg(index);
                    }
                    else if (symbol is PropertySymbol property)
                    {
                        // Get the property from the struct
                    }
                };
            }
            else if (node.Value is LiteralNode literal)
            {
                loadValue = () => emitter.GetLiteral(literal); // Load the literal value
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
                        emitter.GetThis();
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

            var il = currentMethod.Body.Processor;

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

            emitter.BinaryOperation(node.Operation);

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
            if (assembly == null || currentModule == null)
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

            var objcTypeRef = new TypeReference(
                "Object",
                "System",
                "System.Runtime",
                true
            );

            // TODO: Add type to assembly
            var clss = new TypeDefinition(currentNamespace, node.Name, typeAttributes, objcTypeRef);
            currentType = clss;

            currentModule.Types.Add(clss);

            // If the struct has a body, visit it
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

            //emitter.CreateObject(constructor);
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

            var il = method.Body.Processor;

            emitter.SetILProcessor(il);

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
            currentType.Methods.Add(method);

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            il.Emit(OpCode.Return);
        }

        public void Visit(ModuleNode node)
        {
            currentNamespace = node.Name;
            var module = new ModuleDefinition(node.Name);
            assembly.Modules.Add(module);

            currentModule = module;

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

            currentType.Methods.Add(method);
            currentMethod = method;

            emitter.SetILProcessor(method.Body.Processor);

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

            var type = (TypeReferenceNode)node.TypeNode;
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

            // Add the property to the current type
            var property = new PropertyDefinition(node.Name, PropertyAttributes.None, reference);
            currentType?.Properties.Add(property);

            // Add a backing field for the property
            var field = new FieldDefinition($"__{node.Name}__", FieldAttributes.Private, reference);
            currentType.Fields.Add(field);

            // WORKAROUND: Until Iona supports attributes, we hardcode the FieldOffset attribute if the type is (Int, Float etc.)
            if (currentType.Name is "Int" or "Int8" or "Int16" or "Int32" or "Int64")
            {
                AddFieldOffset(field, 0);
            }

            // Add the getter
            var getter = new MethodDefinition($"get_{node.Name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                reference);

            currentType.Methods.Add(getter);

            getter.HasThis = true;

            emitter.SetILProcessor(getter.Body.Processor);

            emitter.GetThis();
            emitter.GetField(field);

            emitter.Return();

            // Find (Void) in the type system of Iona (not C#s void)
            var typeRef = new TypeReference(
                "Void",
                "Iona.Builtins",
                "Iona.Builtins",
                false
            );
            // Add the setter
            var setter = new MethodDefinition($"set_{node.Name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeRef);

            setter.HasThis = true;

            setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, reference));

            emitter.SetILProcessor(setter.Body.Processor);

            emitter.GetThis();
            emitter.GetArg(1);
            emitter.SetField(field);
            emitter.Return();

            currentType.Methods.Add(setter);
        }

        public void Visit(ReturnNode node)
        {
            /*
            if (assembly == null || currentMethod == null)
            {
                return;
            }

            if (node.Value is IdentifierNode identifier)
            {
                EmitGetIdentifier(identifier);
            }
            else if (node.Value is PropAccessNode propAccess)
            {
                EmitGetPropAccess(propAccess);
            }
            else if (node.Value is BinaryExpressionNode bin)
            {
                bin.Accept(this);
            }
            else
            {
                return;
            }

            emitter.Return();
            */
        }

        public void Visit(StructNode node)
        {
            /*
            TypeAttributes typeAttributes = TypeAttributes.SequentialLayout | TypeAttributes.Sealed;

            switch (node.AccessLevel)
            {
                case AST.Types.AccessLevel.Public:
                    typeAttributes |= TypeAttributes.Public;
                    break;
                case AST.Types.AccessLevel.Private:
                    typeAttributes |= TypeAttributes.NotPublic;
                    break;
                default:
                    typeAttributes |= TypeAttributes.NotPublic;
                    break;
            }

            var valueTypeReference = new TypeReference(
                "System",
                "ValueType",
                assembly.MainModule,
                RuntimeAssembly
            );

            var strct = new TypeDefinition(currentNamespace, node.Name, typeAttributes, null);
            currentType = strct;

#if IONA_BOOTSTRAP
            switch (node.Name)
            {
                case "Bool":
                case "Byte":
                case "Char":
                    SetTypeLayout(strct, 1);
                    break;
                case "Double":
                    SetTypeLayout(strct, 8);
                    break;
                case "Float":
                    SetTypeLayout(strct, 4);
                    break;
                case "Int8":
                case "UInt8":
                    SetTypeLayout(strct, 1);
                    break;
                case "Int16":
                case "UInt16":
                    SetTypeLayout(strct, 2);
                    break;
                case "Int32":
                case "UInt32":
                    SetTypeLayout(strct, 4);
                    break;
                case "Int64":
                case "UInt64":
                    SetTypeLayout(strct, 8);
                    break;
                case "Int":
                case "UInt":
                    SetTypeLayout(strct, 4);
                    break;
            }
#endif

            assembly?.MainModule.Types.Add(strct);

            // If the struct has a body, visit it
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            */
        }

        public void Visit(TypeReferenceNode node)
        {

        }

        public void Visit(VariableNode node)
        {
            /*
            if (assembly == null || currentMethod == null)
            {
                return;
            }

            var type = (TypeReferenceNode)node.TypeNode;
            TypeReference? reference = null;

            if (type == null)
            {
                return;
            }

            reference = new TypeReference(
                NamespaceOf(type.FullyQualifiedName),
                type.Name,
                assembly.MainModule,
                assembly.MainModule
            );

            var variable = new VariableDefinition(reference);
            currentMethod.Body.Variables.Add(variable);

            if (node.Value is IdentifierNode identifier)
            {
                EmitGetIdentifier(identifier);
            }
            else if (node.Value is PropAccessNode propAccess)
            {
                EmitGetPropAccess(propAccess);
            }
            else if (node.Value is BinaryExpressionNode bin)
            {
                bin.Accept(this);
            }
            else if (node.Value is InitCallNode init)
            {
                init.Accept(this);
            }

            emitter.SetVariable(variable.Index);

            */
        }

        // ---- Helper methods ----
        private void SetTypeLayout(TypeDefinition type, int size)
        {
            /*
            var structLayoutTypeReference = new TypeReference(
                "System.Runtime.InteropServices",
                "StructLayoutAttribute",
                assembly.MainModule,
                RuntimeAssembly
            );

            var layoutKindTypeReference = new TypeReference(
                "System.Runtime.InteropServices",
                "LayoutKind",
                assembly.MainModule,
                RuntimeAssembly
            );

            // Create a MethodReference for the constructor taking a LayoutKind argument
            var structLayoutConstructor = new MethodReference(".ctor", assembly.MainModule.TypeSystem.Void, structLayoutTypeReference);
            structLayoutConstructor.HasThis = true; // It's an instance method on the attribute
            structLayoutConstructor.Parameters.Add(new ParameterDefinition(layoutKindTypeReference)); // Parameter type is LayoutKind

            // Import the constructor reference
            var importedStructLayoutConstructor = assembly.MainModule.ImportReference(structLayoutConstructor);

            // Create the custom attribute
            var structLayoutAttribute = new CustomAttribute(importedStructLayoutConstructor);

            // Add the LayoutKind.Sequential value as a constructor argument
            structLayoutAttribute.ConstructorArguments.Add(
                new CustomAttributeArgument(layoutKindTypeReference, (int)LayoutKind.Sequential)
            );

            // Add the Size property (named value)
            var sizeProperty = new Mono.Cecil.CustomAttributeNamedArgument(
                "Size",
                new CustomAttributeArgument(assembly.MainModule.TypeSystem.Int32, size)
            );
            structLayoutAttribute.Properties.Add(sizeProperty);

            // Add the custom attribute to the struct
            type.CustomAttributes.Add(structLayoutAttribute);

            */
        }

        private void AddFieldOffset(FieldDefinition field, int size)
        {
            /*
            var fieldOffsetTypeReference = new TypeReference(
                "System.Runtime.InteropServices",
                "FieldOffsetAttribute",
                assembly.MainModule,
                RuntimeAssembly
            );

            // Create a MethodReference for the constructor taking an int argument
            var fieldOffsetConstructor = new MethodReference(".ctor", assembly.MainModule.TypeSystem.Void, fieldOffsetTypeReference);
            fieldOffsetConstructor.HasThis = true; // It's an instance method on the attribute
            fieldOffsetConstructor.Parameters.Add(new ParameterDefinition(assembly.MainModule.TypeSystem.Int32)); // Parameter type is int

            // Import the constructor reference
            var importedFieldOffsetConstructor = assembly.MainModule.ImportReference(fieldOffsetConstructor);

            // Create the custom attribute
            var fieldOffsetAttribute = new CustomAttribute(importedFieldOffsetConstructor);

            // Add the offset value (0) as a constructor argument
            fieldOffsetAttribute.ConstructorArguments.Add(new CustomAttributeArgument(assembly.MainModule.TypeSystem.Int32, size));

            field.CustomAttributes.Add(fieldOffsetAttribute);
            */
        }

        private void EmitGetIdentifier(IdentifierNode node)
        {
            /*
            if (currentMethod == null)
            {
                return;
            }

            var il = currentMethod.Body.GetILProcessor();

            var symbol = table.FindBy(node);

            if (symbol is VariableSymbol variable)
            {
                if (variable.Parent == null)
                {
                    return;
                }

                var index = variable.Parent.Symbols.OfType<VariableSymbol>().ToList().FindIndex(symbol => symbol.Name == variable.Name);

                emitter.GetVariable(index);
            }
            else if (symbol is ParameterSymbol parameter)
            {
                if (parameter.Parent == null)
                {
                    return;
                }

                var index = parameter.Parent.Symbols.OfType<ParameterSymbol>().ToList().FindIndex(symbol => symbol.Name == parameter.Name);

                emitter.GetArg(index);
            }
            */
        }

        private void EmitSetIdentifier(IdentifierNode node)
        {
            /*
            if (currentMethod == null)
            {
                return;
            }

            var il = currentMethod.Body.GetILProcessor();

            var symbol = table.FindBy(node);

            if (symbol is VariableSymbol variable)
            {
                if (variable.Parent == null)
                {
                    return;
                }

                var index = variable.Parent.Symbols.OfType<VariableSymbol>().ToList().FindIndex(symbol => symbol.Name == variable.Name);

                emitter.GetVariable(index);
            }
            else if (symbol is PropertyNode prop)
            {
                EmitSetProperty(prop);
            }
            else if (symbol is MemberAccessNode member)
            {

            }
            */
        }

        void EmitSetProperty(PropertyNode prop)
        {
            /*
            if (currentMethod == null)
            {
                return;
            }

            // Load the this pointer
            emitter.GetThis();

            // Load the value

            // Get the property from the current type
            */
        }

        private void EmitGetPropAccess(PropAccessNode node)
        {
            /*
            if (currentMethod == null)
            {
                return;
            }

            var il = currentMethod.Body.GetILProcessor();

            if (node.Object is IdentifierNode target)
            {
                if (target.Value == "self")
                {
                    var symbol = table.FindBy(target);

                    if (symbol is TypeSymbol typeSymbol)
                    {
                        var memberIdentifier = ((IdentifierNode)node.Property).Value;

                        var prop = typeSymbol.Symbols.OfType<PropertySymbol>().FirstOrDefault(f => f.Name == memberIdentifier);

                        if (prop != null)
                        {
                            // Get the property from the struct
                        }
                    }
                }
            }
            */
        }

        private string NamespaceOf(string name)
        {
            return name.Substring(0, name.LastIndexOf('.'));
        }

        private PropertyDefinition? GetProperty(PropAccessNode node)
        {
            /*
            // Load the object of node
            // A prop may start with an identifier, or self

            TypeReference? objcType = null;
            TypeSymbol? typeSymbol = null;

            // We start with self
            if (node.Object is SelfNode self)
            {
                typeSymbol = table.FindTypeByFQN(self.ResultType.FullyQualifiedName) as TypeSymbol;
                objcType = currentType;
            }
            // When we don't start with self there are three cases:
            // 1. The object is a property of the current type
            // 2. The object is a parameter of the current method
            // 3. The object is a variable in the current method
            else
            {
                var symbol = table.FindBy(node.Object);

                if (symbol is PropertySymbol prop)
                {
                    // Get the property from the struct
                }
                else if (symbol is ParameterSymbol param)
                {
                    // Get the parameter from the method
                }
                else if (symbol is VariableSymbol variable)
                {
                    // In the current method, load the variable
                    // First find out the index
                    var index = variable.Parent.Symbols.OfType<VariableSymbol>().ToList().FindIndex(symbol => symbol.Name == variable.Name);
                    emitter.GetVariable(index);

                    var type = currentMethod.Body.Variables[index].VariableType;

                    typeSymbol = variable.Type;

                    objcType = type;
                }
            }

            if (objcType == null || typeSymbol == null)
            {
                return null;
            }

            if (node.Property is IdentifierNode identifier)
            {
                var propIndex = typeSymbol.Symbols.OfType<BlockSymbol>().First().Symbols.OfType<PropertySymbol>().ToList().FindIndex(symbol => symbol.Name == identifier.Value);

                // Get the property from the object
                var prop = objcType.Resolve().Properties[propIndex];

                return prop;
            }

            */
            return null;
        }

        private TypeReference? GetTypeReference(TypeSymbol type)
        {
            /*
            TypeReference? reference = null;
#if IONA_BOOTSTRAP
            switch (type.Name)
            {
                case "bool":
                    reference = new TypeReference(
                        "System",
                        "Boolean",
                        assembly.MainModule,
                        RuntimeAssembly
                    );
                    break;
                case "byte":
                    reference = new TypeReference(
                        "System",
                        "Byte",
                        assembly.MainModule,
                        RuntimeAssembly
                    );
                    break;
                case "decimal":
                    reference = new TypeReference(
                       "System",
                       "Decimal",
                       assembly.MainModule,
                       RuntimeAssembly
                   );
                    break;
                case "double":
                    reference = new TypeReference(
                       "System",
                       "Double",
                       assembly.MainModule,
                       RuntimeAssembly
                   );
                    break;
                case "float":
                    reference = new TypeReference(
                       "System",
                       "Float",
                       assembly.MainModule,
                       RuntimeAssembly
                   );
                    break;
                case "int":
                    reference = assembly.MainModule.TypeSystem.IntPtr;
                    break;
                case "long":
                    reference = new TypeReference(
                       "System",
                       "Int64",
                       assembly.MainModule,
                       RuntimeAssembly
                   );
                    break;
                case "nint":
                    reference = new TypeReference(
                        "System",
                        "IntPtr",
                        assembly.MainModule,
                        RuntimeAssembly
                    );
                    break;
                case "nuint":
                    reference = new TypeReference(
                       "System",
                       "UIntPtr",
                       assembly.MainModule,
                       RuntimeAssembly
                   );
                    break;
                case "sbyte":
                    reference = new TypeReference(
                       "System",
                       "SByte",
                       assembly.MainModule,
                       RuntimeAssembly
                   );
                    break;
                case "short":
                    reference = new TypeReference(
                       "System",
                       "Int16",
                       assembly.MainModule,
                       RuntimeAssembly
                   );
                    break;
                case "string":
                    reference = new TypeReference(
                       "System",
                       "String",
                       assembly.MainModule,
                       RuntimeAssembly
                   );
                    break;
                case "uint":
                    reference = new TypeReference(
                       "System",
                       "UInt32",
                       assembly.MainModule,
                       RuntimeAssembly
                   );
                    break;
                case "ulong":
                    reference = new TypeReference(
                       "System",
                       "UInt64",
                       assembly.MainModule,
                       RuntimeAssembly
                   );
                    break;
                case "ushort":
                    reference = new TypeReference(
                       "System",
                       "UInt16",
                       assembly.MainModule,
                       RuntimeAssembly
                   );
                    break;
                default:
                    reference = new TypeReference(
                        NamespaceOf(type.FullyQualifiedName),
                        type.Name,
                        assembly.MainModule,
                        assembly.MainModule
                    );
                    break;
            }
            // Check if the name of the type of the node is CIL type
#else
            reference = new TypeReference(
                NamespaceOf(type.FullyQualifiedName), 
                type.Name, 
                assembly.MainModule, 
                assembly.MainModule
            );
#endif

            return reference;
            */
            return null;
        }
    }
}
