//|--- IParamAccessVisitor.cs ----------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using AST.Types;

namespace AST.Visitors;

public interface IParamAccessVisitor
{
    public void Visit(ParamAccessNode node);
}