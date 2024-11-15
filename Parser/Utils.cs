using AST.Nodes;
using Lexer.Tokens;
using System.Text;
using Shared;

namespace Parser
{
    internal static class Utils
    {
        internal static void SetMeta(INode node, Token token)
        {
            var meta = new Metadata
            {
                File = token.File,
                LineStart = token.Line,
                LineEnd = token.Line,
                ColumnStart = token.ColumnStart,
                ColumnEnd = token.ColumnEnd
            };
            node.Meta = meta;
        }

        internal static void SetMeta(INode node, List<Token> tokens)
        {
            var token = tokens[0];
            var start = token.ColumnStart;
            var end = tokens[tokens.Count - 1].ColumnEnd;

            var meta = new Metadata
            {
                File = token.File,
                LineStart = token.Line,
                LineEnd = tokens[tokens.Count - 1].Line,
                ColumnStart = start,
                ColumnEnd = end
            };
            node.Meta = meta;
        }

        internal static void SetMeta(INode node, INode startNode, INode endNode)
        {
            var meta = new Metadata
            {
                File = startNode.Meta.File,
                LineStart = startNode.Meta.LineStart,
                LineEnd = endNode.Meta.LineEnd,
                ColumnStart = startNode.Meta.ColumnStart,
                ColumnEnd = endNode.Meta.ColumnEnd
            };
            node.Meta = meta;
        }

        internal static void IncreaseColumn(INode node, int amount)
        {
            var meta = new Metadata
            {
                File = node.Meta.File,
                LineStart = node.Meta.LineStart,
                LineEnd = node.Meta.LineEnd,
                ColumnStart = node.Meta.ColumnStart,
                ColumnEnd = node.Meta.ColumnEnd + amount
            };

            node.Meta = meta;
        }

        internal static void SetColumnEnd(INode node, int columnEnd)
        {
            var meta = new Metadata
            {
                File = node.Meta.File,
                LineStart = node.Meta.LineStart,
                LineEnd = node.Meta.LineEnd,
                ColumnStart = node.Meta.ColumnStart,
                ColumnEnd = columnEnd
            };

            node.Meta = meta;
        }

        internal static void SetLineEnd(INode node, int lineEnd)
        {
            var meta = new Metadata
            {
                File = node.Meta.File,
                LineStart = node.Meta.LineStart,
                LineEnd = lineEnd,
                ColumnStart = node.Meta.ColumnStart,
                ColumnEnd = node.Meta.ColumnEnd,
            };

            node.Meta = meta;
        }

        internal static void SetLineEnd(INode node, Token token)
        {
            var meta = new Metadata
            {
                File = node.Meta.File,
                LineStart = node.Meta.LineStart,
                LineEnd = token.Line,
                ColumnStart = node.Meta.ColumnStart,
                ColumnEnd = node.Meta.ColumnEnd,
            };

            node.Meta = meta;
        }

        internal static void SetStart(INode node, Token token)
        {
            var meta = new Metadata
            {
                File = node.Meta.File ?? token.File,
                LineStart = token.Line,
                LineEnd = node.Meta.LineEnd,
                ColumnStart = token.ColumnStart,
                ColumnEnd = node.Meta.ColumnEnd,
            };

            node.Meta = meta;
        }

        internal static void SetEnd(INode node, Token token)
        {
            var meta = new Metadata
            {
                File = node.Meta.File,
                LineStart = node.Meta.LineStart,
                LineEnd = token.Line,
                ColumnStart = node.Meta.ColumnStart,
                ColumnEnd = token.ColumnEnd,
            };

            node.Meta = meta;
        }

        internal static string ResolveFullyQualifiedName(ITypeNode node)
        {
            var current = node.Parent;

            var nodes = new List<ITypeNode>();
            ModuleNode? module = null;

            while (current != null)
            {
                if (current is ITypeNode typeNode)
                {
                    nodes.Add(typeNode);
                }

                if (current is ModuleNode modNode)
                {
                    module = modNode;
                    break;
                }

                current = current.Parent;
            }

            // This should never happen
            if (module == null)
            {
                return node.Name;
            }

            nodes.Reverse();

            var strBuilder = new StringBuilder();

            strBuilder.Append(module?.Name);

            foreach (var n in nodes)
            {
                strBuilder.Append(n.Name);
                strBuilder.Append(".");
            }

            strBuilder.Append($".{node.Name}");

            var name = strBuilder.ToString();

            return name;
        }
    }
}
