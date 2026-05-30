//|--- CompilerErrorFactory.cs ---------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public static class CompilerErrorFactory
    {
        public static CompilerError TopLevelDefinitionError(string name, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.UndefinedTopLevel,
                $"Top level definition `{name}` is not defined",
                meta
            );
        }

        public static CompilerError TypeMismatchError(string expected, string actual, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.TypeMismatch,
                $"Expected `{expected}`, but got `{actual}`",
                meta
            );
        }

        public static CompilerError UndefinedNameError(string name, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.UndefinedName,
                $"`{name}` is not defined",
                meta
            );
        }

        public static CompilerError SyntaxError(string message, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.SyntaxError,
                message,
                meta
            );
        }

        public static CompilerError TypeDoesNotContainProperty(string type, string prop, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.TypeDoesNotContainProperty,
                $"`{type}` does not contain property `{prop}`",
                meta
            );
        }

        public static CompilerError TypeDoesNotContainMethod(string type, string method, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.TypeDoesNotContainMethod,
                $"`{type}` does not contain method `{method}`",
                meta
            );
        }

        public static CompilerError ExpectedMember(string token, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.ExpectedMember,
                $"Expected function or property declaration, but got `{token}`",
                meta
            );
        }

        public static CompilerError MissingTypeAnnotation(string name, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.MissingTypeAnnotation,
                $"Missing type annotation.", 
                meta
                );
        }

        public static CompilerError NoBinaryOverload(
            string method, 
            string left, 
            string right,
            string? returnType,
            Metadata meta
            )
        {
            var msg =
                $"Neither `{left}` nor `{right}` implements binary operation `{method}`";

            if (returnType != null)
            {
                msg += $" returning `{returnType}`";
            }

            msg += "\n\n";
            msg += $"Left operand: `{left}`\n";
            msg += $"Right operand: `{right}`\n";
            if (returnType != null)
            {
                msg += $"Expected return type: `{returnType}`\n";
            }

            return new CompilerError(
                CompilerErrorCode.MissingTypeAnnotation,
                msg, 
                meta
            );
        }
        
        public static CompilerError AmbigiousOperatorOverload(string thisType, string otherType, string method, string left, string right, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.AmbiguousOperatorOverload,
                $"Both `{thisType}` and `{otherType}` implement binary operation `{method}` taking [lhs: {left}, rhs: {right}]", 
                meta
            );
        }

        public static CompilerError NoMatchingConstructorForArgs(string typeName, Dictionary<string, string> args, Metadata meta)
        {
            var argString = args.Aggregate("", (lhs, rhs) => lhs.ToString() + ", " + rhs.ToString());
            
            return new CompilerError(
                CompilerErrorCode.NoMatchingConstructorForArgs,
                $"`{typeName}` does not implement any constructor matching {argString}", 
                meta
            );
        }

        public static CompilerError AmbigiousFunctionCall(string functionName, List<string> modules, Metadata meta)
        {
            var modulesString = modules.Aggregate("", (lhs, rhs) => lhs.ToString() + " and" + rhs.ToString());
            
            return new CompilerError(
                CompilerErrorCode.AmbiguousFunctionCall,
                $"`Ambiguous call of {functionName}`. It is defined in `{modulesString}`", 
                meta
            );
        }

        public static CompilerError MissingParameterName(Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.MissingParameterName,
                $"Missing parameter name before argument. Parameter names must be specified in Iona.", 
                meta
            );
        }
        
        public static CompilerError MissingColonAfterParameterName(Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.MissingColonAfterParameterName,
                $"Missing colon between parameter name and argument.", 
                meta
            );
        }

        public static CompilerError AmbiguousTypeReference(string name, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.AmbiguousTypeReference,
                $"Ambiguous type reference. Type `{name}` is defined in multiple modules", 
                meta
            );
        }

        public static CompilerError CannotInferType(string target, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.CannotInferType,
                $"Type inference for `{target}` failed. Check previous errors for more information.", 
                meta
            );
        }

        public static CompilerError VariableNotAllowedInTopLevel(Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.VariableNotAllowedInTopLevel,
                $"Variables are not allowed as top-level statements.",
                meta
            );
        }

        public static CompilerError InaccessibleMember(string member, string typeKind, string typeName, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.InaccessibleMember,
                $"Cannot access private member `{member}` of {typeKind} `{typeName}`",
                meta
            );
        }

        public static CompilerError ImmutableVariableAssignment(string name, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.ImmutableVariableAssignment,
                $"Cannot change value of immutable variable `{name}`",
                meta
            );
        }

        public static CompilerError MutatingInNonMutatingFunc(string property, string func, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.MutatingInNonMutatingFunc,
                $"Cannot assign to property `{property}` in non-mutating function `{func}`. Use `mut fn` to allow mutation",
                meta
            );
        }

        public static CompilerError MutablePropertyInRecord(string name, string record, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.MutablePropertyInRecord,
                $"Record `{record}` cannot have mutable property `{name}`. Use `let` instead of `var`",
                meta
            );
        }

        public static CompilerError MutatingFuncInRecord(string name, string record, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.MutatingFuncInRecord,
                $"Record `{record}` cannot have mutating function `{name}`. Records are immutable",
                meta
            );
        }

        public static CompilerError OptionalNotUnwrapped(string name, string type, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.OptionalNotUnwrapped,
                $"Value of optional type `{type}?` must be unwrapped before accessing members of `{name}`. Use `?.` for optional chaining, `!` for force unwrap, or `guard` to safely unwrap",
                meta
            );
        }

        public static CompilerError OptionalArgumentNotUnwrapped(string paramName, string type, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.OptionalArgumentNotUnwrapped,
                $"Argument for `{paramName}` is an optional `{type}?` but the parameter is not optional. Unwrap it with `!`, `?.`, or `guard` before passing",
                meta
            );
        }

        public static CompilerError ForceUnwrapOfNonOptional(string type, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.ForceUnwrapOfNonOptional,
                $"Cannot force-unwrap `{type}` because it is not optional. Remove the `!`",
                meta
            );
        }

        public static CompilerError ReturnTypeMismatch(string expected, string actual, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.ReturnTypeMismatch,
                $"Cannot return value of type `{actual}` in a function expected to return `{expected}`",
                meta
            );
        }

        public static CompilerError ContractConformanceMissingFunc(
            string typeName, string contractName, string funcName, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.ContractConformanceMissingMember,
                $"Type `{typeName}` does not implement function `{funcName}` required by contract `{contractName}`",
                meta
            );
        }

        public static CompilerError ContractConformanceMissingProperty(
            string typeName, string contractName, string propertyName, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.ContractConformanceMissingMember,
                $"Type `{typeName}` does not implement property `{propertyName}` required by contract `{contractName}`",
                meta
            );
        }

        public static CompilerError ContractConformanceMissingOperator(
            string typeName, string contractName, string operatorName, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.ContractConformanceMissingMember,
                $"Type `{typeName}` does not implement operator `{operatorName}` required by contract `{contractName}`",
                meta
            );
        }

        public static CompilerError ContractConformanceMissingInit(
            string typeName, string contractName, string initSignature, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.ContractConformanceMissingMember,
                $"Type `{typeName}` does not implement initializer `{initSignature}` required by contract `{contractName}`",
                meta
            );
        }

        public static CompilerError ValueTypeCannotInheritClass(
            string typeName, string className, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.ValueTypeCannotInheritClass,
                $"`{typeName}` cannot inherit from class `{className}`. Only classes can inherit from other classes",
                meta
            );
        }

        public static CompilerError InheritingFromClosedClass(string subclass, string baseClass, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.InheritingFromClosedClass,
                $"`{subclass}` cannot inherit from `{baseClass}` because it is not `open`",
                meta
            );
        }

        public static CompilerError OverrideOfNonOpenMember(string memberName, string baseClass, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.OverrideOfNonOpenMember,
                $"`{memberName}` cannot override `{baseClass}.{memberName}` because it is not `open`",
                meta
            );
        }

        public static CompilerError OverrideMissingBaseMember(string memberName, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.OverrideMissingBaseMember,
                $"`override` `{memberName}` does not match any inherited member",
                meta
            );
        }

        public static CompilerError MissingOverrideOnShadow(string memberName, string baseClass, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.MissingOverrideOnShadow,
                $"`{memberName}` hides inherited `{baseClass}.{memberName}`; add `override` to replace it",
                meta
            );
        }

        public static CompilerError GenericConstraintNotSatisfied(string suppliedType, string paramName, string constraintName, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.GenericConstraintNotSatisfied,
                $"Type `{suppliedType}` does not satisfy constraint `{paramName}: {constraintName}`",
                meta
            );
        }
    }
}
