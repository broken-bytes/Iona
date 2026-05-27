//|--- ASTVisualizerFactory.cs ---------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace ASTVisualizer
{
    public static class ASTVisualizerFactory
    {
        public static IASTVisualizer Create()
        {
            return new ASTVisualizer();
        }
    }
}
