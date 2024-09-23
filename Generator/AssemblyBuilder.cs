using AST.Nodes;
using AST.Visitors;
using Mono.Cecil.Cil;
using Mono.Cecil;
using Symbols;
using Symbols.Symbols;

namespace Generator
{
    public class AssemblyBuilder :
        IAssignmentVisitor,
        IBlockVisitor,
        IIdentifierVisitor,
        IInitVisitor,
        IFileVisitor,
        IModuleVisitor,
        IPropertyVisitor,
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

        public void Visit(BlockNode node)
        {
            foreach (var child in node.Children)
            {
                if (child is PropertyNode property)
                {
                    property.Accept(this);
                }

                if (child is InitNode init)
                {
                    init.Accept(this);
                }

                if (child is BlockNode block)
                {
                    block.Accept(this);
                }

                if (child is AssignmentNode assignment)
                {
                    assignment.Accept(this);
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

                reference = new TypeReference(type.Module, type.Name, assembly.MainModule, assembly.MainModule.TypeSystem.CoreLibrary);

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
                    reference = new TypeReference(type.Module, type.Name, assembly.MainModule, assembly.MainModule.TypeSystem.CoreLibrary);
                    break;
            }
            // Check if the name of the type of the node is CIL type
#else
            reference = new TypeReference(type.Module, type.Name, assembly.MainModule, assembly.MainModule.TypeSystem.CoreLibrary);
#endif
            // Add the property to the current type
            var property = new PropertyDefinition(node.Name, PropertyAttributes.None, reference);
            currentType?.Properties.Add(property);

            // Add a backing field for the property
            var field = new FieldDefinition($"__{node.Name}__", FieldAttributes.Private, reference);
            currentType.Fields.Add(field);

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

        public void Visit(StructNode node)
        {
            TypeAttributes typeAttributes = TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;

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

            var strct = new TypeDefinition(currentNamespace, node.Name, typeAttributes, assembly.MainModule.ImportReference(typeof(System.ValueType)));
            currentType = strct;

            if (node.AccessLevel == AST.Types.AccessLevel.Public)
            {
                var exported = new CustomAttribute(assembly?.MainModule.ImportReference(typeof(System.Runtime.InteropServices.ComVisibleAttribute).GetConstructor(new[] { typeof(bool) })));
                exported.ConstructorArguments.Add(new CustomAttributeArgument(assembly?.MainModule.TypeSystem.Boolean, true));
                strct.CustomAttributes.Add(exported);
            }

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
    }
}
