namespace Typeck.Passes.Decl;

internal static class DeclPassFactory
{
    internal static DeclPass Create(ExpressionResolver expressionResolver)
    {
        var memberSubPass = new DeclPassMemberRegisterSubPass();
        var registerSubPass = new DeclPassMemberRegisterSubPass();
        
        return new DeclPass([memberSubPass, registerSubPass]);
    }
}