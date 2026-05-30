//|--- CecilInspector.cs ---------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Mono.Cecil;

namespace Tests.Helpers;

// Direct Cecil reads of a produced assembly so codegen tests can assert metadata shape —
// generic param constraints, method signatures, etc. — without launching the runtime.
public static class CecilInspector
{
    // "App.Box`1.T = App.Numeric,App.Comparable" entries.
    public static List<string> ListGenericParamConstraints(string assemblyPath)
    {
        var asm = AssemblyDefinition.ReadAssembly(assemblyPath);
        var result = new List<string>();
        foreach (var t in asm.MainModule.Types)
        {
            foreach (var gp in t.GenericParameters)
            {
                var constraints = string.Join(",", gp.Constraints
                    .Select(c => c.ConstraintType.FullName));
                result.Add($"{t.FullName}.{gp.Name}={constraints}");
            }
            foreach (var m in t.Methods)
            {
                foreach (var gp in m.GenericParameters)
                {
                    var constraints = string.Join(",", gp.Constraints
                        .Select(c => c.ConstraintType.FullName));
                    result.Add($"{t.FullName}.{m.Name}.{gp.Name}={constraints}");
                }
            }
        }
        return result;
    }
}
