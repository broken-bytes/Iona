using Lexer.Tokens;

namespace Lexer
{
    public static class Utils
    {
        public static TokenFamily GetTokenFamily(TokenType type)
        {
            switch (type)
            {
                // Identifiers
                case TokenType.Identifier:
                    return TokenFamily.Identifier;

                // Literals
                case TokenType.Boolean:
                case TokenType.Float:
                case TokenType.Integer:
                case TokenType.NullLiteral:
                case TokenType.String:
                    return TokenFamily.Literal;

                // Operators
                case TokenType.BitAnd:
                case TokenType.BitInverse:
                case TokenType.BitLShift:
                case TokenType.BitNegate:
                case TokenType.BitOr:
                case TokenType.BitRShift:
                case TokenType.And:
                case TokenType.Arrow:
                case TokenType.Assign:
                case TokenType.Divide:
                case TokenType.DivideAssign:
                case TokenType.Dot:
                case TokenType.Equal:
                case TokenType.Greater:
                case TokenType.GreaterEqual:
                case TokenType.Less:
                case TokenType.LessEqual:
                case TokenType.Minus:
                case TokenType.MinusAssign:

                case TokenType.Modulo:
                case TokenType.ModAssign:
                case TokenType.Multiply:
                case TokenType.MultiplyAssign:
                case TokenType.NotEqual:
                case TokenType.Plus:
                case TokenType.PlusAssign:
                case TokenType.Pipe:
                case TokenType.Or:
                case TokenType.Xor:
                    return TokenFamily.Operator;

                // Grouping
                case TokenType.BracketLeft:
                case TokenType.BracketRight:
                case TokenType.CurlyLeft:
                case TokenType.CurlyRight:
                case TokenType.ParenLeft:
                case TokenType.ParenRight:
                    return TokenFamily.Grouping;

                // Keywords
                case TokenType.Async:
                case TokenType.Await:
                case TokenType.Break:
                case TokenType.Catch:
                case TokenType.Continue:
                case TokenType.Contract:
                case TokenType.Do:
                case TokenType.Else:
                case TokenType.Enum:
                case TokenType.For:
                case TokenType.Fileprivate:
                case TokenType.Finally:
                case TokenType.Fn:
                case TokenType.If:
                case TokenType.Use:
                case TokenType.In:
                case TokenType.Init:
                case TokenType.Let:
                case TokenType.Module:
                case TokenType.Mutating:
                case TokenType.Of:
                case TokenType.Open:
                case TokenType.Private:
                case TokenType.Public:
                case TokenType.Return:
                case TokenType.Self:
                case TokenType.Struct:
                case TokenType.Throw:
                case TokenType.Throws:
                case TokenType.Try:
                case TokenType.Until:
                case TokenType.Var:
                case TokenType.When:
                case TokenType.While:
                case TokenType.Yield:
                    return TokenFamily.Keyword;

                // Special
                case TokenType.Comma:
                case TokenType.Colon:
                case TokenType.Linebreak:
                    return TokenFamily.Special;

                // Unknown
                default:
                    return TokenFamily.Unknown;
            }
        }

        internal static bool CheckMatchingSequence(string source, string target)
        {
            // Check if strings are equal
            if (source.StartsWith(target) && source.Length == target.Length)
            {
                return true;
            }

            // Check if source is longer and the next character is an operator or whitespace
            if (source.StartsWith(target) &&
                source.Length > target.Length &&
                (IsOperator(source[target.Length]) || 
                char.IsWhiteSpace(source[target.Length]) ||
                (IsGrouping(source[target.Length])))
            )
            {
                return true;
            }

            return false;
        }

        internal static bool IsOperator(char c)
        {
            char[] operators = { '+', '-', '*', '/', '%', '=', '<', '>', '!', '&', '|', '^', '~', '?', ':', '.', };
            return operators.Contains(c);
        }

        internal static bool IsGrouping(char c)
        {
            char[] groupings = { '(', ')', '[', ']', '{', '}', };
            return groupings.Contains(c);
        }

        internal static Token MakeToken(TokenType tokenType, string value)
        {
            return new Token
            {
                Family = GetTokenFamily(tokenType),
                Type = tokenType,
                Value = value,
                File = string.Empty,  // Set later with UPDATE_TOKEN
                Line = 0,                  // Set later with UPDATE_TOKEN
                ColumnStart = 0,           // Set later with UPDATE_TOKEN
                ColumnEnd = value.Length,
                Error = string.Empty
            };
        }

        internal static void UpdateToken(ref Token token, string file, int line, int start)
        {
            token.File = file;
            token.Line = line;
            token.ColumnStart = start;
            token.ColumnEnd = start + token.ColumnEnd;
        }

        public static int DropWhitespace(string source)
        {
            int index = 0;
            while (index < source.Length && char.IsWhiteSpace(source[index]) && source[index] != '\n')
            {
                index++;
            }

            return index;
        }

        public static string AsString(this Keyword keyword)
        {
            switch (keyword)
            {
                case Keyword.Actor: return "actor";
                case Keyword.As: return "as";
                case Keyword.Async: return "async";
                case Keyword.Await: return "await";
                case Keyword.Break: return "break";
                case Keyword.Captured: return "captured";
                case Keyword.Catch: return "catch";
                case Keyword.Class: return "class";
                case Keyword.Continue: return "continue";
                case Keyword.Contract: return "contract";
                case Keyword.Defer: return "defer";
                case Keyword.Do: return "do";
                case Keyword.Else: return "else";
                case Keyword.Enum: return "enum";
                case Keyword.False: return "false";
                case Keyword.Fileprivate: return "fileprivate";
                case Keyword.For: return "for";
                case Keyword.From: return "from";
                case Keyword.Fn: return "fn";
                case Keyword.If: return "if";
                case Keyword.Use: return "use";
                case Keyword.Init: return "init";
                case Keyword.Internal: return "internal";
                case Keyword.Let: return "let";
                case Keyword.Module: return "module";
                case Keyword.Mut: return "mut";
                case Keyword.Null: return "null";
                case Keyword.Op: return "op";
                case Keyword.Open: return "open";
                case Keyword.Private: return "private";
                case Keyword.Public: return "public";
                case Keyword.Return: return "return";
                case Keyword.This: return "self";
                case Keyword.Static: return "static";
                case Keyword.Struct: return "struct";
                case Keyword.Task: return "task";
                case Keyword.Throw: return "throw";
                case Keyword.Throws: return "throws";
                case Keyword.True: return "true";
                case Keyword.Try: return "try";
                case Keyword.Until: return "until";
                case Keyword.Var: return "var";
                case Keyword.When: return "when";
                case Keyword.While: return "while";
                case Keyword.With: return "with";
                case Keyword.Yield: return "yield";
                default: return "";
            }
        }

        public static string AsString(this Grouping grouping)
        {
            switch (grouping)
            {
                case Grouping.ParenLeft: return "(";
                case Grouping.ParenRight: return ")";
                case Grouping.BracketLeft: return "[";
                case Grouping.BracketRight: return "]";
                case Grouping.CurlyLeft: return "{";
                case Grouping.CurlyRight: return "}";
                default: return "";
            }
        }

        public static string AsString(this Operator op)
        {
            switch (op)
            {
                case Operator.Add: return "+";
                case Operator.Sub: return "-";
                case Operator.Mul: return "*";
                case Operator.Div: return "/";
                case Operator.Mod: return "%";
                case Operator.Pow: return "^";
                case Operator.Inc: return "++";
                case Operator.Dec: return "--";
                case Operator.Assign: return "=";
                case Operator.AddAssign: return "+=";
                case Operator.SubAssign: return "-=";
                case Operator.MulAssign: return "*=";
                case Operator.DivAssign: return "/=";
                case Operator.ModAssign: return "%=";
                case Operator.PowAssign: return "^=";
                case Operator.And: return "&";
                case Operator.Or: return "|";
                case Operator.Xor: return "^";
                case Operator.Not: return "!";
                case Operator.AndAssign: return "&=";
                case Operator.OrAssign: return "|=";
                case Operator.XorAssign: return "^=";
                case Operator.NotAssign: return "!=";
                case Operator.AndAnd: return "&&";
                case Operator.OrOr: return "||";
                case Operator.Equal: return "==";
                case Operator.NotEqual: return "!=";
                case Operator.Less: return "<";
                case Operator.Greater: return ">";
                case Operator.LessEqual: return "<=";
                case Operator.GreaterEqual: return ">=";
                case Operator.ShiftLeft: return "<<";
                case Operator.ShiftRight: return ">>";
                case Operator.ShiftLeftAssign: return "<<=";
                case Operator.ShiftRightAssign: return ">>=";
                case Operator.Ternary: return "?";
                case Operator.Arrow: return "->";
                case Operator.Dot: return ".";
                default:
                    // Handle invalid/unsupported operators appropriately 
                    // (e.g., throw an exception, return an error string, etc.)
                    throw new ArgumentException("Invalid Operator");
            }
        }

        public static string AsString(this Special special)
        {
            switch (special)
            {
                case Special.Comma: return ",";
                case Special.Colon: return ":";
                case Special.HardUnwrap: return "!";
                case Special.SoftUnwrap: return "?";
                default:
                    throw new ArgumentException("Invalid Operator");
            }
        }
    }
}
