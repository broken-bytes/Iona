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
                CompilerErrorCode.AmbigiousOperatorOverload,
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
                CompilerErrorCode.AmbigiousFunctionCall,
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
    }
}
