using System.Text;
using AST.Nodes;
using AST.Types;

namespace AST
{
    public static class Utils
    {
        public static FileNode GetRoot(INode node)
        {
            while (node.Parent != null)
            {
                node = node.Parent;
            }

            if (node is not FileNode fileNode)
            {
                throw new NullReferenceException();
            }
            
            return fileNode;
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

            if (node is FileNode fileNode)
            {
                var module = fileNode.Children.OfType<ModuleNode>().FirstOrDefault();

                if (module is not null)
                {
                    return module;
                }
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

        public static string SourceValue(this INode node)
        {
            var builder = new StringBuilder();
            
            switch (node)
            {
                case IdentifierNode identifier:
                {
                    builder.Append(identifier.ILValue);
                    break;
                }
                case PropAccessNode propAccess:
                {
                    builder.Append(propAccess.Object.SourceValue());
                    builder.Append(propAccess.Parent.SourceValue());
                    break;
                }
                case VariableNode variable:
                {
                    builder.Append(variable.Name);
                    break;
                }
            }
            
            return builder.ToString();
        }
    }
}
