using Lexer.Tokens;

namespace Lexer.Processors
{
    public class OperatorProcessor : IProcessor
    {
        public Token? Process(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            // An operator cannot start with a letter or digit
            if (char.IsLetterOrDigit(source[0]))
            {
                return null;
            }

            // Find the length of the potential operator (using LINQ)
            int count = source.TakeWhile(c => Utils.IsOperator(c)).Count();

            string operatorStr = source.Substring(0, count);

            var token = ProcessCompoundOperator(operatorStr);
            if (token != null)
            {
                return token;
            }

            token = ProcessMathOperator(operatorStr);
            if (token != null)
            {
                return token;
            }

            token = ProcessComparisonOperator(operatorStr);
            if (token != null)
            {
                return token;
            }

            token = ProcessLogicalOperator(operatorStr);
            if (token != null)
            {
                return token;
            }

            token = ProcessSpecialOperator(operatorStr);
            if (token != null)
            {
                return token;
            }

            token = ProcessBitwiseOperator(operatorStr);
            if (token != null)
            {
                return token;
            }

            return null; // No matching operator found
        }

        private Token? ProcessCompoundOperator(string source)
        {
            switch (source)
            {
                case "+=": return Utils.MakeToken(TokenType.PlusAssign, Operator.AddAssign.AsString());
                case "-=": return Utils.MakeToken(TokenType.MinusAssign, Operator.SubAssign.AsString());
                case "*=": return Utils.MakeToken(TokenType.MultiplyAssign, Operator.MulAssign.AsString());
                case "/=": return Utils.MakeToken(TokenType.DivideAssign, Operator.DivAssign.AsString());
                case "%=": return Utils.MakeToken(TokenType.ModAssign, Operator.ModAssign.AsString());
                case "&=": return Utils.MakeToken(TokenType.BitAndAssign, Operator.AndAssign.AsString());
                case "|=": return Utils.MakeToken(TokenType.BitOrAssign, Operator.OrAssign.AsString());
                case "^=": return Utils.MakeToken(TokenType.BitXorAssign, Operator.XorAssign.AsString());
                case "!=": return Utils.MakeToken(TokenType.NotEqual, Operator.NotEqual.AsString());
                case "<<=": return Utils.MakeToken(TokenType.BitLShiftAssign, Operator.ShiftLeftAssign.AsString());
                case ">>=": return Utils.MakeToken(TokenType.BitRShiftAssign, Operator.ShiftRightAssign.AsString());
                case "=": return Utils.MakeToken(TokenType.Assign, Operator.Assign.AsString());
                default: return null;
            }
        }

        private Token? ProcessMathOperator(string source)
        {
            switch (source)
            {
                case "+": return Utils.MakeToken(TokenType.Plus, Operator.Add.AsString());
                case "-": return Utils.MakeToken(TokenType.Minus, Operator.Sub.AsString());
                case "*": return Utils.MakeToken(TokenType.Multiply, Operator.Mul.AsString());
                case "/": return Utils.MakeToken(TokenType.Divide, Operator.Div.AsString());
                case "%": return Utils.MakeToken(TokenType.Modulo, Operator.Mod.AsString());
                default: return null;
            }
        }

        private Token? ProcessComparisonOperator(string source)
        {
            switch (source)
            {
                case "==": return Utils.MakeToken(TokenType.Equal, Operator.Equal.AsString());
                // The '!=' case is already handled in ProcessCompoundOperator
                case "<=": return Utils.MakeToken(TokenType.LessEqual, Operator.LessEqual.AsString());
                case ">=": return Utils.MakeToken(TokenType.GreaterEqual, Operator.GreaterEqual.AsString());
                case "<": return Utils.MakeToken(TokenType.ArrowLeft, Operator.Less.AsString());
                case ">": return Utils.MakeToken(TokenType.ArrowRight, Operator.Greater.AsString());
                default: return null;
            }
        }

        private Token? ProcessLogicalOperator(string source)
        {
            switch (source)
            {
                case "&&": return Utils.MakeToken(TokenType.And, Operator.AndAnd.AsString());
                case "||": return Utils.MakeToken(TokenType.Or, Operator.OrOr.AsString());
                default: return null;
            }
        }

        private Token? ProcessSpecialOperator(string source)
        {
            switch (source)
            {
                case "::": return Utils.MakeToken(TokenType.Scope, Operator.Scope.AsString());
                case "++": return Utils.MakeToken(TokenType.Increment, Operator.Inc.AsString());
                case "--": return Utils.MakeToken(TokenType.Decrement, Operator.Dec.AsString());
                case "->": return Utils.MakeToken(TokenType.Arrow, Operator.Arrow.AsString());
                case "!": return Utils.MakeToken(TokenType.Not, Operator.Not.AsString());
                case ".": return Utils.MakeToken(TokenType.Dot, Operator.Dot.AsString());
                default: return null;
            }
        }

        private Token? ProcessBitwiseOperator(string source)
        {
            switch (source)
            {
                case "<<": return Utils.MakeToken(TokenType.BitLShift, Operator.ShiftLeft.AsString());
                case ">>": return Utils.MakeToken(TokenType.BitRShift, Operator.ShiftRight.AsString());
                case "&": return Utils.MakeToken(TokenType.BitAnd, Operator.And.AsString());
                case "|": return Utils.MakeToken(TokenType.BitOr, Operator.Or.AsString());
                case "^": return Utils.MakeToken(TokenType.Xor, Operator.Xor.AsString());
                default: return null;
            }
        }
    }
}
