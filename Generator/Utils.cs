using AST.Types;

namespace Generator;

public static class Utils
{
    public static string AssignmentString(this AssignmentType assignmentType)
    {
        switch (assignmentType)
        {
            case AssignmentType.Assign:
                return " = ";
            case AssignmentType.AddAssign:
                return " += ";
            case AssignmentType.SubAssign:
                return " -= ";
        }

        return "";
    }
}