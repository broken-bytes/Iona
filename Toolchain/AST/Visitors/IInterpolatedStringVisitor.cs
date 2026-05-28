//|--- IInterpolatedStringVisitor.cs ---------------------------|
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
    public interface IInterpolatedStringVisitor
    {
        public void Visit(InterpolatedStringNode node);
    }
}
