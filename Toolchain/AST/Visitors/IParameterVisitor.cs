//|--- IParameterVisitor.cs ------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Types;

namespace AST.Visitors;

public interface IParameterVisitor
{
    public void Visit(ParameterNode parameter)
    {
        parameter.Accept(this);
    }
}