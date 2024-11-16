using AST.Nodes;
using AST.Types;

namespace AST
{
    public static class Utils
    {
        public static INode GetRoot(INode node)
        {
            while (node.Parent != null)
            {
                node = node.Parent;
            }

            return node;
        }

        public static ModuleNode GetModule(INode node)
        {
            while (node.Parent != null)
            {
                if (node is ModuleNode module)
                {
                    return module;
                }

                node = node.Parent;
            }

            return new ModuleNode("Global", "Global", null);
        }

        public static string Name(this LiteralType literalType)
        {
            switch (literalType)
            {
                case LiteralType.Boolean: return "Bool";
                case LiteralType.Char: return "Char";
                case LiteralType.Double: return "Double";
                case LiteralType.Float: return "Float";
                case LiteralType.Integer: return "Int32";
                case LiteralType.Null: return "None";
                case LiteralType.String: return "String";
            }

            return "";
        }
    }
}
