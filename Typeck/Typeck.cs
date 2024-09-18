using AST.Nodes;
using AST.Types;
using Typeck.Symbols;

namespace Typeck
{
    internal class Typeck : ITypeck
    {
        private readonly SymbolTableConstructor _tableConstructor;
        private readonly TypeChecker _typeChecker;

        internal Typeck(SymbolTableConstructor tableConstructor, TypeChecker typeChecker)
        {
            _tableConstructor = tableConstructor;
            _typeChecker = typeChecker;
        }

        public SymbolTable BuildSymbolTable(INode node)
        {
            SymbolTable table;

            // The root node shall be a file node, but we strip it and only add the module
            if (node is not FileNode fileNode)
            {
                // Panic
                return new SymbolTable();
            }

            _tableConstructor.ConstructSymbolTable(fileNode, out table);

            return table;
        }

        public void TypeCheck(INode node, SymbolTable table)
        {
            if (node is FileNode fileNode)
            {
                _typeChecker.TypeCheckAST(ref fileNode, table);
            }
            return;
            // We need to check types for the following nodes:
            // - ClassNode (Generic args, Inheritance)
            // - ContractNode (Generic args, Inheritance)
            // - EnumNode (Enum Type)
            // - FuncNode (Return type, Parameters)
            // - PropertyNode (Type)
            // - StructNode (Generic args)
            // - VariableNode (Type)
            // - ExpressionNode (Type)

            if (node is FileNode file)
            {
                foreach (var child in file.Children)
                {
                    TypeCheck(child, table);
                }
            }
            else if (node is ModuleNode module)
            {
                foreach (var child in module.Children)
                {
                    TypeCheck(child, table);
                }
            }
            else if (node is ClassNode clazz)
            {
                TypeCheckClass(clazz, table);
            }
            else if (node is ContractNode contract)
            {
                TypeCheckContract(contract, table);
            }
            else if (node is EnumNode enm)
            {
                TypeCheckEnum(enm, table);
            }
            else if (node is FuncNode func)
            {
                TypeCheckFunc(func, table);
            }
            else if (node is PropertyNode prop)
            {
                TypeCheckProperty(prop, table);
            }
            else if (node is StructNode strct)
            {
                TypeCheckStruct(strct, table);
            }
        }

        public SymbolTable MergeTables(List<SymbolTable> tables)
        {
            var mergedTable = new SymbolTable();

            RegisterBuiltins(mergedTable);

            foreach (var table in tables)
            {
                foreach (var module in table.Modules)
                {
                    mergedTable.Modules.Add(module);
                }
            }

            return mergedTable;
        }

        // ---- Helper methods ----
        // ---- Type checking ----
        private void RegisterBuiltins(SymbolTable table)
        {
            // Add the builtins to the global table (Int, Float, String, Bool)
            var intType = new TypeSymbol("Int", TypeKind.Struct);
            var floatType = new TypeSymbol("Float", TypeKind.Struct);
            var stringType = new TypeSymbol("String", TypeKind.Class);
            var boolType = new TypeSymbol("Bool", TypeKind.Struct);

            var builtins = new ModuleSymbol("Builtins");
            ((ISymbol)builtins).AddSymbol(intType);
            ((ISymbol)builtins).AddSymbol(floatType);
            ((ISymbol)builtins).AddSymbol(stringType);
            ((ISymbol)builtins).AddSymbol(boolType);

            table.Modules.Add(builtins);
        }

        // ---- Type checking ----
        private void InferExpressionType(INode node, SymbolTable table)
        {

        }

        private void TypeCheckClass(ClassNode node, SymbolTable table)
        {
            // TODO: Complete checks
            if (node.Body != null)
            {
                foreach (var child in node.Body.Children)
                {
                    TypeCheck(child, table);
                }
            }
        }

        private void TypeCheckContract(ContractNode node, SymbolTable table)
        {

        }

        private void TypeCheckEnum(EnumNode node, SymbolTable table)
        {
            // Check the enum type
            // TODO: Enums don't support types yet
        }

        private void TypeCheckFunc(FuncNode node, SymbolTable table)
        {
            // Check the return type
            if (node.ReturnType != null)
            {
                var returnType = TypeCheckTypeReference(node.ReturnType, table);
                if (returnType != null)
                {
                    node.ReturnType = returnType;
                }
            }
            else
            {
                // Find the builtins module
                var builtins = new IdentifierNode("Builtins", node);
                node.ReturnType = new MemberAccessNode(builtins, new TypeReferenceNode("None"), node);
            }

            // Check the parameters
            foreach (var param in node.Parameters)
            {
                TypeCheck(param.Type, table);
            }

            if (node.Body != null)
            {
                foreach (var child in node.Body.Children)
                {
                    TypeCheck(child, table);
                }
            }
        }

        private void TypeCheckProperty(PropertyNode node, SymbolTable table)
        {
            if (node.TypeNode == null)
            {
                // Panic
                return;
            }

            var type = TypeCheckTypeReference(node.TypeNode, table);

            node.TypeNode = type;
        }

        private void TypeCheckStruct(StructNode node, SymbolTable table)
        {
            // TODO: Complete checks
            if (node.Body != null)
            {
                foreach (var child in node.Body.Children)
                {
                    TypeCheck(child, table);
                }
            }
        }

        private INode? TypeCheckTypeReference(INode node, SymbolTable table)
        {
            if (node is not TypeReferenceNode type)
            {
                return null;
            }

            // First we need to find the module this type usage is in
            var module = GetModuleForNode(node);

            if (module == null)
            {
                // Panic
                return null;
            }

            // Find the module in the symbol table
            var moduleSymbol = table.Modules.Find(m => m.Name == module.Name);

            if (moduleSymbol == null)
            {
                // Panic
                return null;
            }

            // Now we know the reference to the type and what module we are in
            // - Check if the type is in the current module
            if (moduleSymbol.Symbols.Find(s => s.Name == type.Name) != null)
            {
                var moduleName = new IdentifierNode(moduleSymbol.Name, node);
                var access = new MemberAccessNode(moduleName, type, node);

                return access;
            }
            else
            {
                var typeFoundInModules = new List<ModuleSymbol>();
                // The type is not in the current module, check the imported modules
                foreach (var import in ((FileNode)module.Root).Children.Where(child => child is ImportNode))
                {
                    var importNode = (ImportNode)import;

                    var importModule = table.Modules.Find(m => m.Name == importNode.Name);

                    if (importModule == null)
                    {
                        // Panic
                        return null;
                    }

                    if (importModule.Symbols.Find(s => s.Name == type.Name) != null)
                    {
                        typeFoundInModules.Add(importModule);
                    }
                }

                if(typeFoundInModules.Count == 1)
                {
                    var moduleName = new IdentifierNode(typeFoundInModules[0].Name, node);
                    var access = new MemberAccessNode(moduleName, type, node);

                    return access;
                }
                else if(typeFoundInModules.Count > 1)
                {
                    var moduleString = string.Join(", ", typeFoundInModules.Select(m => m.Name));
                    return new ErrorNode(0, 0, 0, "", $"Ambiguous type reference, {type.Name} found in multiple modules [{moduleString}]");
                }
            }

            // Return an error node
            return new TypeReferenceNode("Error", node);
        }

        private ModuleNode? GetModuleForNode(INode node)
        {
            if (node is ModuleNode or FileNode)
            {
                return null;
            }


            INode current = node;

            while (current.Parent != null)
            {
                if (current is ModuleNode module)
                {
                    return module;
                }

                current = current.Parent;
            }

            return null;
        }
    }
}
