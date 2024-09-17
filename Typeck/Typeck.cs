using AST.Nodes;
using AST.Types;
using Typeck.Symbols;

namespace Typeck
{
    internal class Typeck : ITypeck
    {
        public SymbolTable BuildSymbolTable(INode node)
        {
            var table = new SymbolTable();

            // The root node shall be a file node, but we strip it and only add the module
            if (node is not FileNode fileNode)
            {
                // Panic
                return table;
            }

            // Ensure that the file node has a module
            if (fileNode.Children.Count == 0)
            {
                // Panic
                return table;
            }

            fileNode.Children.Insert(0, new ImportNode("Builtins", fileNode));

            var moduleNode = fileNode.Children.Find(c => c is ModuleNode);

            if (moduleNode == null)
            {
                // Panic
                return table;
            }

            var module = CreateSymbol(moduleNode);

            if (module is ModuleSymbol symbol)
            {
                table.Modules.Add(symbol);
            }

            return table;
        }

        public void TypeCheck(INode node, SymbolTable table)
        {
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

        // ---- Symbol table ----
        private ISymbol? CreateSymbol(INode node)
        {
            ISymbol? symbol = null;

            if (node is ModuleNode)
            {
                symbol = CreateModuleSymbol(node);
            }
            else if (node.Type is NodeType.Declaration)
            {
                var statement = ((IStatementNode)node).StatementType;
                switch (statement)
                {
                    case StatementType.FunctionDeclaration:
                        symbol = CreateFunctionSymbol(node);
                        break;
                    case StatementType.ClassDeclaration:
                    case StatementType.ContractDeclaration:
                    case StatementType.EnumDeclaration:
                    case StatementType.StructDeclaration:
                        symbol = CreateTypeSymbol(node);
                        break;
                    case StatementType.PropertyDeclaration:
                        symbol = CreatePropertySymbol(node);
                        break;
                    case StatementType.VariableDeclaration:
                        symbol = CreateVariableSymbol(node);
                        break;
                }
            }
            else if (node is BlockNode)
            {
                symbol = CreateBlockSymbol(node);
            }

            return symbol;
        }

        private ISymbol CreateBlockSymbol(INode node)
        {
            var blockSymbol = new BlockSymbol();

            var blockNode = (BlockNode)node;

            foreach (var child in blockNode.Children)
            {
                var symbol = CreateSymbol(child);

                if (symbol != null)
                {
                    ((ISymbol)blockSymbol).AddSymbol(symbol);
                }
            }

            return blockSymbol;
        }

        private ModuleSymbol? CreateModuleSymbol(INode node)
        {
            if (node is ModuleNode moduleNode)
            {
                var moduleSymbol = new ModuleSymbol(moduleNode.Name);

                foreach (var child in moduleNode.Children)
                {
                    var symbol = CreateSymbol(child);

                    if (symbol != null)
                    {
                        ((ISymbol)moduleSymbol).AddSymbol(symbol);
                    }
                }

                return moduleSymbol;
            }

            return null;
        }

        private FuncSymbol? CreateFunctionSymbol(INode node)
        {
            if (node.Type is NodeType.Declaration)
            {
                var statement = (IStatementNode)node;

                if (statement.StatementType == StatementType.FunctionDeclaration)
                {
                    var functionNode = (FuncNode)node;

                    var functionSymbol = new FuncSymbol(functionNode.Name);

                    // Add the parameters
                    foreach (var param in functionNode.Parameters)
                    {
                        functionSymbol.Parameters.Add(new TypeSymbol(param.Name, TypeKind.Unknown));
                    }

                    // Add the return type
                    functionSymbol.ReturnType = new TypeSymbol("", TypeKind.Unknown);

                    // Parse the body
                    if (functionNode.Body != null)
                    {
                        foreach (var child in functionNode.Body.Children)
                        {
                            var symbol = CreateSymbol(child);

                            if (symbol != null)
                            {
                                ((ISymbol)functionSymbol).AddSymbol(symbol);
                            }
                        }
                    }

                    return functionSymbol;
                }
            }

            return null;
        }

        private TypeSymbol? CreateTypeSymbol(INode node)
        {
            TypeSymbol? type = null;

            if (node.Type is NodeType.Declaration)
            {
                var statement = (IStatementNode)node;

                if (statement.StatementType == StatementType.ClassDeclaration)
                {
                    var classNode = (ClassNode)node;
                    type = new TypeSymbol(classNode.Name, TypeKind.Class);

                    if (classNode.Body != null)
                    {
                        ((ISymbol)type).AddSymbol(CreateBlockSymbol(classNode.Body));
                    }
                }
                else if (statement.StatementType == StatementType.ContractDeclaration)
                {
                    var contractNode = (ContractNode)node;
                    type = new TypeSymbol(contractNode.Name, TypeKind.Contract);

                    if (contractNode.Body != null)
                    {
                        ((ISymbol)type).AddSymbol(CreateBlockSymbol(contractNode.Body));
                    }
                }
                else if (statement.StatementType == StatementType.EnumDeclaration)
                {
                    var enumNode = (EnumNode)node;
                    type = new TypeSymbol(enumNode.Name, TypeKind.Enum);
                }
                else if (statement.StatementType == StatementType.StructDeclaration)
                {
                    var structNode = (StructNode)node;
                    type = new TypeSymbol(structNode.Name, TypeKind.Struct);

                    if (structNode.Body != null)
                    {
                        ((ISymbol)type).AddSymbol(CreateBlockSymbol(structNode.Body));
                    }
                }

                // Sanity check, don't parse children if the type is null
                if (type == null)
                {
                    return null;
                }
            }

            return type;
        }

        private PropertySymbol? CreatePropertySymbol(INode node)
        {
            if (node.Type is NodeType.Declaration)
            {
                var statement = (IStatementNode)node;

                if (statement.StatementType == StatementType.PropertyDeclaration)
                {
                    var propNode = (PropertyNode)node;

                    var type = new TypeSymbol("", TypeKind.Unknown);

                    var propSymbol = new PropertySymbol(propNode.Name, type);

                    return propSymbol;
                }
            }

            return null;
        }

        private VariableSymbol? CreateVariableSymbol(INode node)
        {
            if (node.Type is NodeType.Declaration)
            {
                var statement = (IStatementNode)node;

                if (statement.StatementType == StatementType.VariableDeclaration)
                {
                    var variable = (VariableNode)node;

                    var type = new TypeSymbol("", TypeKind.Unknown);

                    var variableSymbol = new VariableSymbol(variable.Name, type);

                    return variableSymbol;
                }
            }

            return null;
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
            if(node.TypeNode == null)
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
            if(moduleSymbol.Symbols.Find(s => s.Name == type.Name) != null)
            {
                var moduleName = new IdentifierNode(moduleSymbol.Name, node);
                var access = new MemberAccessNode(moduleName, type, node);

                return access;
            }
            else
            {
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

                    if(importModule.Symbols.Find(s => s.Name == type.Name) != null)
                    {
                        var moduleName = new IdentifierNode(importModule.Name, node);
                        var access = new MemberAccessNode(moduleName, type, node);

                        return access;
                    }
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
