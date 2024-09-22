using AST.Nodes;
using AST.Visitors;
using Mono.Cecil.Cil;
using Mono.Cecil;
using Symbols;

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

            il.Append(il.Create(OpCodes.Ret));

            currentMethod = method;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

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
