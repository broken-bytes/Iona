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

        internal void ConstructSymbolTable(FileNode file, SymbolTable table, string assembly)
        {
            _currentSymbol = null;
            _assembly = assembly;
            _symbolTable = table;

            // Add the builtins module to the imports of the file node
            file.Children.Insert(0, new ImportNode("Iona.Builtins", file));

            file.Accept(this);
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
            
            // We do not want to have the blocks in our symbol table structure as they are only needed in the AST so we do not add them at all
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
            
            _currentSymbol = null;
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

            var lastSymbol = _currentSymbol;

            var csharpName = Shared.Utils.IonaToCSharpName(node.Name);
            var symbol = new FuncSymbol(node.Name, csharpName);
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

            _currentSymbol = lastSymbol;
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

            if (_currentSymbol is not TypeSymbol typeSymbol)
            {
                return;
            }

            var symbol = new InitSymbol();
            symbol.ReturnType = typeSymbol;
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

            symbol = _symbolTable.Modules.Find(module => module.Name == node.Name);

            if (symbol == null)
            {
                symbol = new ModuleSymbol(node.Name, _assembly);
                _symbolTable.Modules.Add(symbol);
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

            _currentSymbol = null;
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
            string cSharpName = Shared.Utils.IonaToCSharpName(node.Name);
            if (node.AccessLevel == AccessLevel.Private)
            {
                cSharpName = $"_{node.Name}";
            }
            // TODO: Add actual get set access levels instead of public per default
            var symbol = new PropertySymbol(
                node.Name,
                cSharpName, 
                new TypeSymbol("Unknown", TypeKind.Unknown),
                false,
                true,
                AccessLevel.Public,
                AccessLevel.Public
                );

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
