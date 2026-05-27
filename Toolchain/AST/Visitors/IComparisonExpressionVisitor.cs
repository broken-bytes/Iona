//|--- IComparisonExpressionVisitor.cs -------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;

namespace AST.Visitors
{
    public interface IComparisonExpressionVisitor
    {
        public void Visit(ComparisonExpressionNode node);
    }
}
