using AST.Nodes;
using Lexer.Tokens;

namespace Parser
{
    internal static class Utils
    {
        internal static void SetMeta(INode node, Token token)
        {
            var meta = new INode.Metadata
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

            var meta = new INode.Metadata
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
            var meta = new INode.Metadata
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
            var meta = new INode.Metadata
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
            var meta = new INode.Metadata
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
            var meta = new INode.Metadata
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
            var meta = new INode.Metadata
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
            var meta = new INode.Metadata
            {
                File = node.Meta.File,
                LineStart = token.Line,
                LineEnd = node.Meta.LineEnd,
                ColumnStart = token.ColumnStart,
                ColumnEnd = node.Meta.ColumnEnd,
            };

            node.Meta = meta;
        }

        internal static void SetEnd(INode node, Token token)
        {
            var meta = new INode.Metadata
            {
                File = node.Meta.File,
                LineStart = node.Meta.LineStart,
                LineEnd = token.Line,
                ColumnStart = node.Meta.ColumnStart,
                ColumnEnd = token.ColumnEnd,
            };

            node.Meta = meta;
        }
    }
}
