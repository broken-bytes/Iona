//|--- ASTLoggerFactory.cs -------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace ASTLogger
{
    public static class ASTLoggerFactory
    {
        public static IASTLogger Create()
        {
            return new ASTLogger();
        }
    }
}
