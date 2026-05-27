//|--- DeclPassFactory.cs --------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Shared;

namespace Typeck.Passes.Decl;

internal static class DeclPassFactory
{
    internal static DeclPass Create(ExpressionResolver expressionResolver, IErrorCollector errorCollector)
    {
        var memberSubPass = new DeclPassMemberRegisterSubPass();
        var referenceSubPass = new DeclPassMemberReferenceResolveSubPass(errorCollector, expressionResolver);
        
        return new DeclPass([memberSubPass, referenceSubPass]);
    }
}