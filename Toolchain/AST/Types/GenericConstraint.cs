//|--- GenericConstraint.cs ------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace AST.Types
{
    public struct GenericConstraint
    {
        public GenericCondition Condition { get; set; }
        public IType? Type { get; set; }
    }
}
