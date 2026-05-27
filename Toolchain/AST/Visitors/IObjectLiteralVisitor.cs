//|--- IObjectLiteralVisitor.cs --------------------------------|
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
    public interface IObjectLiteralVisitor
    {
        public void Visit(ObjectLiteralNode node);
    }
}
