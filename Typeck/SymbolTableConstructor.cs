using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Symbols;
using Symbols.Symbols;
using System.Reflection;
using System.Runtime.Loader;

namespace Typeck
{
    internal class SymbolTableConstructor :
        IArrayVisitor,
        IAssignmentVisitor,
        IBlockVisitor,
        IBinaryExpressionVisitor,
        IClassVisitor,
        IContractVisitor,
        IErrorVisitor,
        IFileVisitor,
        IFuncCallVisitor,
        IFuncVisitor,
        IIdentifierVisitor,
        IImportVisitor,
        IInitVisitor,
        ILiteralVisitor,
        IMemberAccessVisitor,
        IModuleVisitor,
        IObjectLiteralVisitor,
        IOperatorVisitor,
        IPropertyVisitor,
        IStructVisitor,
        ITypeReferenceVisitor,
        IUnaryExpressionVisitor,
        IVariableVisitor
    {
        private SymbolTable _symbolTable = new SymbolTable();
        private ISymbol? _currentSymbol;
        private string _assembly;

        internal SymbolTableConstructor()
        {
        }

        internal void ConstructSymbolTable(FileNode file, out SymbolTable table, string assembly)
        {
            _symbolTable = new SymbolTable();
            _symbolTable.Assemblies.Add(new AssemblySymbol(assembly));
            _assembly = assembly;
            table = _symbolTable;

            // Add the builtins module to the imports of the file node
            file.Children.Insert(0, new ImportNode("Iona.Builtins", file));

            file.Accept(this);
        }

        internal void ConstructSymbolsForAssembly(string assemblyName)
        {

            Assembly assembly;
            // First try loading it from the current working directory
            try
            {
                var path = $"{Environment.GetEnvironmentVariable("IONA_SDK_DIR")}\\{assemblyName}.dll";
                assembly = Assembly.LoadFrom(path);
            }
            catch
            {
                // If that fails, try loading it from the GAC
                assembly = Assembly.Load(assemblyName);
            }

            var assemblySymbol = new AssemblySymbol(assemblyName);
            _symbolTable.Assemblies.Add(assemblySymbol);

            var types = assembly.GetTypes();

            foreach (var type in assembly.GetTypes())
            {
                var nspace = type.Namespace;

                if (nspace == null)
                {
                    continue;
                }

                var module = assemblySymbol.Symbols.OfType<ModuleSymbol>().ToList().Find(m => m.Name == nspace);
                if (module == null)
                {
                    module = new ModuleSymbol(nspace, assemblyName);
                    module.Parent = assemblySymbol;
                    assemblySymbol.Symbols.Add(module);
                }
                TypeKind kind = TypeKind.Unknown;
                if (type.IsClass)
                {
                    kind = TypeKind.Class;
                }
                else if (type.IsInterface)
                {
                    kind = TypeKind.Contract;
                }
                else if (type.IsEnum)
                {
                    kind = TypeKind.Enum;
                }
                else if (type.IsValueType)
                {
                    kind = TypeKind.Struct;
                }

                var symbol = new TypeSymbol(type.Name, kind);
                symbol.Parent = module;
                module.Symbols.Add(symbol);

                foreach (var member in type.GetMembers())
                {
                    if (member.MemberType == MemberTypes.Method)
                    {
                        var method = member as MethodInfo;
                        var funcSymbol = new FuncSymbol(method.Name);
                        funcSymbol.Parent = symbol;
                        symbol.Symbols.Add(funcSymbol);

                        foreach (var param in method.GetParameters())
                        {
                            var paramSymbol = new ParameterSymbol(param.Name, new TypeSymbol(param.ParameterType.Name, TypeKind.Unknown), funcSymbol);
                            funcSymbol.Symbols.Add(paramSymbol);
                        }

                        var returnType = new TypeSymbol(method.ReturnType.Name, TypeKind.Unknown);
                        funcSymbol.ReturnType = returnType;
                    }
                    else if (member.MemberType == MemberTypes.Field)
                    {
                        var field = member as FieldInfo;
                        var fieldSymbol = new VariableSymbol(field.Name, new TypeSymbol(field.FieldType.Name, TypeKind.Unknown));
                        fieldSymbol.Parent = symbol;
                        symbol.Symbols.Add(fieldSymbol);
                    }
                    else if (member.MemberType == MemberTypes.Property)
                    {
                        var property = member as PropertyInfo;
                        var propertySymbol = new PropertySymbol(property.Name, new TypeSymbol(property.PropertyType.Name, TypeKind.Unknown));
                        propertySymbol.Parent = symbol;
                        symbol.Symbols.Add(propertySymbol);
                    }
                }
            }
        }

        public void Visit(ArrayNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(AssignmentNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(BlockNode node)
        {
            if (_currentSymbol == null)
            {
                return;
            }

            var symbol = new BlockSymbol();
            AddSymbol(symbol);

            _currentSymbol = symbol;

            foreach (var child in node.Children)
            {
                switch (child)
                {
                    case ArrayNode array:
                        array.Accept(this);
                        break;
                    case BlockNode block:
                        block.Accept(this);
                        break;
                    case ClassNode classNode:
                        classNode.Accept(this);
                        break;
                    case ContractNode contract:
                        contract.Accept(this);
                        break;
                    case FuncCallNode funcCall:
                        funcCall.Accept(this);
                        break;
                    case FuncNode func:
                        func.Accept(this);
                        break;
                    case InitNode init:
                        init.Accept(this);
                        break;
                    case OperatorNode op:
                        op.Accept(this);
                        break;
                    case PropertyNode property:
                        property.Accept(this);
                        break;
                    case StructNode structNode:
                        structNode.Accept(this);
                        break;
                    case VariableNode variable:
                        variable.Accept(this);
                        break;
                    default:
                        break;
                }

                _currentSymbol = symbol;
            }
        }

        public void Visit(BinaryExpressionNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(ClassNode node)
        {
            if (_currentSymbol == null)
            {
                return;
            }

            var symbol = new TypeSymbol(node.Name, TypeKind.Class);
            AddSymbol(symbol);

            _currentSymbol = symbol;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(ContractNode node)
        {
            if (_currentSymbol == null)
            {
                return;
            }

            var symbol = new TypeSymbol(node.Name, TypeKind.Contract);
            AddSymbol(symbol);

            _currentSymbol = symbol;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(ErrorNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(FileNode node)
        {
            // We ignore the file itself, but we visit its children and add the module to the symbol table
            // (a file can only have one module)
            foreach (var child in node.Children)
            {
                if (child is ModuleNode module)
                {
                    module.Accept(this);
                }
            }
        }

        public void Visit(FuncCallNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(FuncNode node)
        {
            if (_currentSymbol == null)
            {
                return;
            }

            var symbol = new FuncSymbol(node.Name);
            foreach (var param in node.Parameters)
            {
                var typeRef = param.TypeNode as TypeReferenceNode;

                if (typeRef == null)
                {
                    continue;
                }

                var paramType = new TypeSymbol(typeRef.Name, TypeKind.Unknown);
                var parameter = new ParameterSymbol(param.Name, paramType, symbol);
                symbol.Symbols.Add(parameter);
            }

            if (node.ReturnType is TypeReferenceNode returnType)
            {
                symbol.ReturnType = new TypeSymbol(returnType.Name, TypeKind.Unknown);
            }

            AddSymbol(symbol);

            _currentSymbol = symbol;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(IdentifierNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(ImportNode import)
        {
            throw new NotImplementedException();
        }

        public void Visit(InitNode node)
        {
            if (_currentSymbol == null)
            {
                return;
            }

            var symbol = new InitSymbol(node.Name);
            foreach (var param in node.Parameters)
            {
                var typeRef = param.TypeNode as TypeReferenceNode;

                if (typeRef == null)
                {
                    continue;
                }

                var paramType = new TypeSymbol(typeRef.Name, TypeKind.Unknown);
                var parameter = new ParameterSymbol(param.Name, paramType, symbol);
                symbol.Symbols.Add(parameter);
            }

            AddSymbol(symbol);
            _currentSymbol = symbol;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(LiteralNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(MemberAccessNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(ModuleNode node)
        {
            ModuleSymbol? symbol = null;

            var assembly = _symbolTable.Assemblies.FirstOrDefault(assembly => assembly.Name == _assembly);
            var modules = assembly.Symbols.OfType<ModuleSymbol>().ToList();
            symbol = modules.Find(module => module.Name == node.Name);

            if (symbol == null)
            {
                symbol = new ModuleSymbol(node.Name, _assembly);
                assembly.Symbols.Add(symbol);
            }

            _currentSymbol = symbol;

            foreach (var child in node.Children)
            {
                switch (child)
                {
                    case ClassNode classNode:
                        classNode.Accept(this);
                        break;
                    case ContractNode contractNode:
                        contractNode.Accept(this);
                        break;
                    case FuncNode funcNode:
                        funcNode.Accept(this);
                        break;
                    case StructNode structNode:
                        structNode.Accept(this);
                        break;
                    case VariableNode variableNode:
                        variableNode.Accept(this);
                        break;
                    default:
                        break;
                }

                _currentSymbol = symbol;
            }
        }

        public void Visit(ObjectLiteralNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(OperatorNode node)
        {
            if (_currentSymbol == null)
            {
                return;
            }

            var symbol = new OperatorSymbol(node.Op);
            foreach (var param in node.Parameters)
            {
                var typeRef = param.TypeNode as TypeReferenceNode;

                if (typeRef == null)
                {
                    continue;
                }

                var paramType = new TypeSymbol(typeRef.Name, TypeKind.Unknown);
                var parameter = new ParameterSymbol(param.Name, paramType, symbol);
                symbol.Symbols.Add(parameter);
            }

            if (node.ReturnType is TypeReferenceNode returnType)
            {
                symbol.ReturnType = new TypeSymbol(returnType.Name, TypeKind.Unknown);
            }

            AddSymbol(symbol);

            _currentSymbol = symbol;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(PropertyNode node)
        {
            var symbol = new PropertySymbol(node.Name, new TypeSymbol("Unknown", TypeKind.Unknown));

            if (_currentSymbol == null)
            {
                return;
            }

            if (node.TypeNode is TypeReferenceNode type)
            {
                symbol.Type = new TypeSymbol(type.Name, TypeKind.Unknown);
            }

            AddSymbol(symbol);
        }

        public void Visit(StructNode node)
        {
            if (_currentSymbol == null)
            {
                return;
            }

            var symbol = new TypeSymbol(node.Name, TypeKind.Struct);
            AddSymbol(symbol);
            _currentSymbol = symbol;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(TypeReferenceNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(UnaryExpressionNode node)
        {
            throw new NotImplementedException();
        }

        public void Visit(VariableNode node)
        {
            var symbol = new VariableSymbol(node.Name, new TypeSymbol("Unknown", TypeKind.Unknown));

            if (_currentSymbol == null)
            {
                return;
            }

            if (node.TypeNode is TypeReferenceNode type)
            {
                symbol.Type = new TypeSymbol(type.Name, TypeKind.Unknown);
            }

            AddSymbol(symbol);
        }

        private void AddSymbol(ISymbol symbol)
        {
            if (_currentSymbol == null)
            {
                return;
            }

            _currentSymbol.Symbols.Add(symbol);
            symbol.Parent = _currentSymbol;
        }
    }
}
