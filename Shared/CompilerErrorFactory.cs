﻿using System;
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
            msg += $"Expected return type: `{returnType}`\n";
            
            return new CompilerError(
                CompilerErrorCode.MissingTypeAnnotation,
                msg, 
                meta
            );
        }
        
        public static CompilerError AmbigiousOperatorOverload(string thisType, string otherType, string method, string left, string right, Metadata meta)
        {
            return new CompilerError(
                CompilerErrorCode.AmbigiousOperatorOverload,
                $"Both `{thisType}` and `{otherType}` implement binary operation `{method}` taking [lhs: {left}, rhs: {right}]", 
                meta
            );
        }
    }
}
