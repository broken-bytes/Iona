//|--- CompilerErrorCode.cs ------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Shared
{
    public enum CompilerErrorCode
    {
        SyntaxError,
        UndefinedName,
        TypeMismatch,
        UndefinedTopLevel,
        TypeDoesNotContainProperty,
        TypeDoesNotContainMethod,
        AmbiguousTypes,
        ExpectedMember,
        MissingTypeAnnotation,
        NoBinaryOverload,
        AmbiguousOperatorOverload,
        NoMatchingConstructorForArgs,
        AmbiguousFunctionCall,
        MissingParameterName,
        MissingColonAfterParameterName,
        AmbiguousTypeReference,
        // The type of a prop or variable cannot get inferred
        CannotInferType,
        VariableNotAllowedInTopLevel,
        ImmutableVariableAssignment,
        InaccessibleMember,
        MutatingInNonMutatingFunc,
        MutablePropertyInRecord,
        MutatingFuncInRecord,
        OptionalNotUnwrapped,
        ReturnTypeMismatch,
        ContractConformanceMissingMember,
        ValueTypeCannotInheritClass,
        ForceUnwrapOfNonOptional,
        OptionalArgumentNotUnwrapped,
        InheritingFromClosedClass,
        OverrideOfNonOpenMember,
        OverrideMissingBaseMember,
        MissingOverrideOnShadow,
        GenericConstraintNotSatisfied
    }
}
