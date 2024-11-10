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
    }
}
