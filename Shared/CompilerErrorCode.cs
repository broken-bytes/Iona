namespace Shared
{
    public enum CompilerErrorCode
    {
        SyntaxError,
        /// Used when a symbol is not defined in the current scope
        UndefinedName,
        /// Used when two types are not compatible (e.g. trying to assign a string to an int)
        TypeMismatch,
        /// Used when a class, contract, enum, struct, or module is not defined
        UndefinedTopLevel,
        /// Used when a top level type has no definition for the name (foo.bar) where bar is not defined in foo
        TypeDoesNotContainProperty,
        /// Used when a type does not contain a method (e.g. trying to call a method that doesn't exist)
        TypeDoesNotContainMethod,
        /// Used when a symbol is ambiguous (e.g. two functions with the same name, or two types from different modules)
        AmbiguousTypes,
        /// Used when a token was found inside a top level type that does not start any member (func, op, prop)
        ExpectedMember,
        /// Used when a prop or variable does not have a type annotation nor is assigned a value during declaration
        MissingTypeAnnotation,
        /// Used when neither type overrides the binary operation used
        NoBinaryOverload,
        /// Used when two types implement the same operator overload
        AmbiguousOperatorOverload,
        /// Used when a type has no constructor that matches the provided arguments
        NoMatchingConstructorForArgs,
        /// Used when a function call uses a function that is defined in multiple modules
        AmbiguousFunctionCall,
        /// The name of a parameter was not provided
        MissingParameterName,
        /// The colon between parameter name and argument was not provided
        MissingColonAfterParameterName,
        /// The referenced type is found in multiple import modules
        AmbiguousTypeReference,
        // The type of a prop or variable cannot get inferred
        CannotInferType
    }
}
