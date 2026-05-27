//|--- Container.cs --------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Iona.Builtins
{
    public class Container
    {
        public bool IsEmpty => _isEmpty;
        private bool _isEmpty;

        public NInt Count => new NInt(0);
    }
}
