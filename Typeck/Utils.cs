using AST.Types;
using Symbols.Symbols;

namespace Typeck
{
    internal class Utils
    {
        internal static Kind SymbolKindToASTKind(TypeKind kind)
        {
            switch (kind)
            {
                case TypeKind.Class:
                    return AST.Types.Kind.Class;
                case TypeKind.Struct:
                    return AST.Types.Kind.Struct;
                case TypeKind.Contract:
                    return AST.Types.Kind.Contract;
                case TypeKind.Enum:
                    return AST.Types.Kind.Enum;
            }

            return Kind.Unknown;
        }

        internal static TypeKind ASTKindToSymbolKind(Kind kind)
        {
            switch (kind)
            {
                case AST.Types.Kind.Class:
                    return TypeKind.Class;
                case AST.Types.Kind.Struct:
                    return TypeKind.Struct;
                case AST.Types.Kind.Contract:
                    return TypeKind.Contract;
                case AST.Types.Kind.Enum:
                    return TypeKind.Enum;
            }

            return TypeKind.Unknown;
        }
        
        internal static string GetFullyQualifiedName(ISymbol symbol)
        {
            var name = symbol.Name;
            var parent = symbol.Parent;

            while (parent != null)
            {
                name = $"{parent.Name}.{name}";
                parent = parent.Parent;
            }

            return name;
        }
    }
}
