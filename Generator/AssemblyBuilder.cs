using AST.Nodes;
using AST.Types;
using AST.Visitors;
using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Symbols;
using Symbols.Symbols;
using System.Reflection;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using System.Runtime.InteropServices;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using System.Linq.Expressions;

namespace Generator
{
    public class AssemblyBuilder :
        IAssignmentVisitor,
        IBinaryExpressionVisitor,
        IBlockVisitor,
        IIdentifierVisitor,
        IInitVisitor,
        IFileVisitor,
        IModuleVisitor,
        IOperatorVisitor,
        IPropertyVisitor,
        IReturnVisitor,
        IStructVisitor,
        ITypeReferenceVisitor
    {

        private readonly AssemblyDefinition assembly;
        private readonly SymbolTable table;
        private string currentNamespace = "";
        private TypeDefinition? currentType;
        private MethodDefinition? currentMethod;

        public AssemblyBuilder(AssemblyDefinition assembly, SymbolTable table)
        {
            this.assembly = assembly;
            this.table = table;
        }

        public void Build(INode node)
        {
            if (node is FileNode file)
            {
                file.Accept(this);
            }
        }

        public void Visit(AssignmentNode node)
        {
            if (assembly == null || currentMethod == null)
            {
                return;
            }

            var il = currentMethod.Body.GetILProcessor();

            if (!currentMethod.IsStatic && !currentMethod.IsConstructor)
            {
                // Load the 'this' reference (the instance the method is called on)
                il.Emit(OpCodes.Ldarg_0);
            }

            if (node.Target is IdentifierNode left)
            {

            }
            else if (node.Target is MemberAccessNode memberAccess)
            {
                if (node.Value is IdentifierNode right)
                {
                    // Check if the target is `self`(this)
                    if (memberAccess.Target is IdentifierNode target && target.Name == "self" && memberAccess.Member is IdentifierNode member)
                    {
                        var property = currentType.Properties.FirstOrDefault(p => p.Name == member.Name);

                        // load the current instance (this)
                        il.Emit(OpCodes.Ldarg_0);

                        // We need to check if the right hand side is a parameter or a local variable or a property
                        var symbol = table.FindBy(right);

                        // We traverse the hierarchy of the symbols in reverse order so we go one level up every time we don't find the symbol
                        ISymbol? currentSymbol = symbol;
                        ISymbol? foundSymbol = currentSymbol.LookupSymbol(right.Name);

                        while (foundSymbol == null)
                        {
                            currentSymbol = currentSymbol?.Parent;
                            foundSymbol = currentSymbol?.LookupSymbol(right.Name);
                        }

                        // There are three cases:
                        // 1. The symbol is a parameter
                        // 2. The symbol is a local variable
                        // 3. The symbol is a property

                        if (foundSymbol is ParameterSymbol parameter)
                        {
                            var index = parameter.Parent.Symbols.OfType<ParameterSymbol>().ToList().FindIndex(symbol => symbol.Name == parameter.Name);

                            // Increase the index by 1 if we are inside a method not a free function
                            if (!currentMethod.IsStatic)
                            {
                                index += 1;
                            }

                            il.Emit(OpCodes.Ldarg, index);

                            il.Emit(OpCodes.Call, property.SetMethod);

                        }
                        else if (foundSymbol is VariableSymbol local)
                        {
                            il.Emit(OpCodes.Ldloc, local.Parent.Symbols.OfType<VariableSymbol>().ToList().FindIndex(symbol => symbol.Name == local.Name));
                        }
                        else if (foundSymbol is PropertySymbol propertySymbol)
                        {
                            // Load the property
                            il.Emit(OpCodes.Call, property.GetMethod);

                            var index = propertySymbol.Parent.Symbols.OfType<PropertySymbol>().ToList().FindIndex(symbol => symbol.Name == propertySymbol.Name);

                            // Load the value
                            il.Emit(OpCodes.Ldloc, index);

                            // Set the value
                            il.Emit(OpCodes.Callvirt, property.SetMethod);
                        }
                    }
                }
            }
        }

        public void Visit(BinaryExpressionNode node)
        {
            if (assembly == null || currentMethod == null)
            {
                return;
            }

            var il = currentMethod.Body.GetILProcessor();

            if (node.Left is IdentifierNode left)
            {
                EmitIdentifierReference(left);
            }
            else if (node.Left is MemberAccessNode memberAccess)
            {
                EmitMemberAccess(memberAccess);
            }
            else 
            {
                return;
            }

            switch (node.Operation)
            {
                case BinaryOperation.Add:
                    il.Emit(OpCodes.Add);
                    break;
                case BinaryOperation.Subtract:
                    il.Emit(OpCodes.Sub);
                    break;
                case BinaryOperation.Multiply:
                    il.Emit(OpCodes.Mul);
                    break;
                case BinaryOperation.Divide:
                    il.Emit(OpCodes.Div);
                    break;
                case BinaryOperation.Mod:
                    il.Emit(OpCodes.Rem);
                    break;
            }

            if (node.Right is IdentifierNode right)
            {
                EmitIdentifierReference(right);
            }
            else if (node.Right is MemberAccessNode memberAccess)
            {
                EmitMemberAccess(memberAccess);
            }
            else
            {
                return;
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

        public void Visit(InitNode node)
        {
            if (assembly == null || currentType == null)
            {
                return;
            }

            var method = new MethodDefinition(
                ".ctor",
                MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                assembly.MainModule.TypeSystem.Void
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

            var il = method.Body.GetILProcessor();

            // Add the parameters to the method
            foreach (var parameter in node.Parameters)
            {
                var type = (TypeReferenceNode)parameter.Type;
                TypeReference? reference = null;

                if (type == null)
                {
                    return;
                }

                reference = new TypeReference(type.Module, type.Name, assembly.MainModule, assembly.MainModule);

                var def = new ParameterDefinition(parameter.Name, ParameterAttributes.None, reference);
                method.Parameters.Add(def);
            }

            currentMethod = method;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            il.Emit(OpCodes.Ret);

            currentType.Methods.Add(method);
        }

        public void Visit(ModuleNode node)
        {
            currentNamespace = node.Name;
            foreach (var child in node.Children)
            {
                if (child is StructNode str)
                {
                    str.Accept(this);
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

            reference = new TypeReference(returnType.Module, returnType.Name, assembly.MainModule, assembly.MainModule);

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
                method = new MethodDefinition("op_Equality", methodAttributes, assembly.MainModule.TypeSystem.Boolean);
            }
            else if (opType == OperatorType.NotEqual)
            {
                method = new MethodDefinition("op_Inequality", methodAttributes, assembly.MainModule.TypeSystem.Boolean);
            }
            else if (opType == OperatorType.GreaterThan)
            {
                method = new MethodDefinition("op_GreaterThan", methodAttributes, assembly.MainModule.TypeSystem.Boolean);
            }
            else if (opType == OperatorType.GreaterThanOrEqual)
            {
                method = new MethodDefinition("op_GreaterThanOrEqual", methodAttributes, assembly.MainModule.TypeSystem.Boolean);
            }
            else if (opType == OperatorType.LessThan)
            {
                method = new MethodDefinition("op_LessThan", methodAttributes, assembly.MainModule.TypeSystem.Boolean);
            }
            else if (opType == OperatorType.LessThanOrEqual)
            {
                method = new MethodDefinition("op_LessThanOrEqual", methodAttributes, assembly.MainModule.TypeSystem.Boolean);
            }

            if (method == null)
            {
                return;
            }

            // Add the parameters to the method
            foreach (var param in node.Parameters)
            {
                var type = (TypeReferenceNode)param.Type;
                TypeReference? paramReference = null;

                if (type == null)
                {
                    return;
                }

                paramReference = new TypeReference(type.Module, type.Name, assembly.MainModule, assembly.MainModule);

                var def = new ParameterDefinition(param.Name, ParameterAttributes.None, paramReference);
                method.Parameters.Add(def);
            }

            currentType.Methods.Add(method);
            currentMethod = method;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
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

#if IONA_BOOTSTRAP
            switch (type.Name)
            {
                case "bool":
                    reference = assembly.MainModule.TypeSystem.Boolean;
                    break;
                case "byte":
                    reference = assembly.MainModule.TypeSystem.Byte;
                    break;
                case "decimal":
                    reference = assembly.MainModule.TypeSystem.Double;
                    break;
                case "double":
                    reference = assembly.MainModule.TypeSystem.Double;
                    break;
                case "float":
                    reference = assembly.MainModule.TypeSystem.Single;
                    break;
                case "int":
                    reference = assembly.MainModule.TypeSystem.Int32;
                    break;
                case "long":
                    reference = assembly.MainModule.TypeSystem.Int64;
                    break;
                case "nint":
                    reference = assembly.MainModule.TypeSystem.IntPtr;
                    break;
                case "nuint":
                    reference = assembly.MainModule.TypeSystem.UIntPtr;
                    break;
                case "sbyte":
                    reference = assembly.MainModule.TypeSystem.SByte;
                    break;
                case "short":
                    reference = assembly.MainModule.TypeSystem.Int16;
                    break;
                case "string":
                    reference = assembly.MainModule.TypeSystem.String;
                    break;
                case "uint":
                    reference = assembly.MainModule.TypeSystem.UInt32;
                    break;
                case "ulong":
                    reference = assembly.MainModule.TypeSystem.UInt64;
                    break;
                case "ushort":
                    reference = assembly.MainModule.TypeSystem.UInt16;
                    break;
                default:
                    reference = new TypeReference(type.Module, type.Name, assembly.MainModule, assembly.MainModule);
                    break;
            }
            // Check if the name of the type of the node is CIL type
#else
            reference = new TypeReference(type.Module, type.Name, assembly.MainModule, assembly.MainModule);
#endif
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

            var il = getter.Body.GetILProcessor();

            il.Emit(OpCodes.Ldarg_0);

            il.Emit(OpCodes.Ldfld, field);

            // Return the loaded value
            il.Emit(OpCodes.Ret);

            property.GetMethod = getter;

            // Add the setter
            var setter = new MethodDefinition($"set_{node.Name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                assembly.MainModule.TypeSystem.Void);

            setter.HasThis = true;

            setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, reference));

            il = setter.Body.GetILProcessor();

            // Load the instance ('this' or 'self') onto the stack
            il.Emit(OpCodes.Ldarg_0);

            // Load the new value (the first argument to the setter) onto the stack
            il.Emit(OpCodes.Ldarg_1);

            // Store the new value into the backing field
            il.Emit(OpCodes.Stfld, field);

            // Return from the setter
            il.Emit(OpCodes.Ret);

            property.SetMethod = setter;

            currentType.Methods.Add(setter);
        }

        public void Visit(ReturnNode node)
        {
            if (assembly == null || currentMethod == null)
            {
                return;
            }

            var il = currentMethod.Body.GetILProcessor();

            if (node.Value is IdentifierNode identifier)
            {
                EmitIdentifierReference(identifier);
            }
            else if (node.Value is MemberAccessNode memberAccess)
            {
                EmitMemberAccess(memberAccess);
            }
            else if (node.Value is BinaryExpressionNode bin)
            {
                bin.Accept(this);
            }
            else
            {
                return;
            }

            il.Emit(OpCodes.Ret);
        }

        public void Visit(StructNode node)
        {
            TypeAttributes typeAttributes = TypeAttributes.SequentialLayout;

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

            // Load the system namespace
            // Manually construct the TypeReference for System.ValueType
            TypeReference valueTypeReference = new TypeReference(
                "System",
                "ValueType",
                assembly.MainModule,
                assembly.MainModule.TypeSystem.CoreLibrary
            );

            var strct = new TypeDefinition(currentNamespace, node.Name, typeAttributes, valueTypeReference);
            currentType = strct;

            SetTypeLayout(strct, 1);

            // If the struct has a body, visit it
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            assembly?.MainModule.Types.Add(strct);
        }

        public void Visit(TypeReferenceNode node)
        {

        }


        // ---- Helper methods ----
        private void SetTypeLayout(TypeDefinition type, int size)
        {
            var structLayoutTypeReference = new TypeReference(
                "System.Runtime.InteropServices",
                "StructLayoutAttribute",
                assembly.MainModule,
                assembly.MainModule.TypeSystem.CoreLibrary
            );

            var layoutKindTypeReference = new TypeReference(
                "System.Runtime.InteropServices",
                "LayoutKind",
                assembly.MainModule,
                assembly.MainModule.TypeSystem.CoreLibrary
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
        }

        private void AddFieldOffset(FieldDefinition field, int size)
        {
            var fieldOffsetTypeReference = new TypeReference(
                "System.Runtime.InteropServices",
                "FieldOffsetAttribute",
                assembly.MainModule,
                assembly.MainModule.TypeSystem.CoreLibrary
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
        }

        private void EmitIdentifierReference(IdentifierNode node)
        {
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

                il.Emit(OpCodes.Ldloc, index);
            }
            else if (symbol is ParameterSymbol parameter)
            {
                if (parameter.Parent == null)
                {
                    return;
                }

                var index = parameter.Parent.Symbols.OfType<ParameterSymbol>().ToList().FindIndex(symbol => symbol.Name == parameter.Name);

                il.Emit(OpCodes.Ldarg, index);
            }
        }

        private void EmitMemberAccess(MemberAccessNode node)
        {
            if (currentMethod == null)
            {
                return;
            }

            var il = currentMethod.Body.GetILProcessor();

            if (node.Target is IdentifierNode target)
            {
                if (target.Name == "self")
                {
                    var symbol = table.FindBy(target);

                    if (symbol is TypeSymbol structSymbol)
                    {
                        var memberIdentifier = ((IdentifierNode)node.Member).Name;

                        var prop = structSymbol.Symbols.OfType<PropertySymbol>().FirstOrDefault(f => f.Name == memberIdentifier);

                        if (prop != null)
                        {
                            // Get the property from the struct
                        }
                    }
                }
            }
        }
    }
}
