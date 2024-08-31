namespace Lexer.Tokens
{
    public class TokenStreamWrongTypeException: Exception
    {
        public TokenStreamWrongTypeException(string message) : base(message) { }
    }

    public class TokenStreamEmptyException : Exception
    {
        public TokenStreamEmptyException() : base("End of file") { }
    }

    public class TokenStream : IEnumerable<Token>
    {
        private readonly Queue<Token> tokens;
        private int currentPosition;

        public TokenStream(List<Token> tokens)
        {
            this.tokens = new Queue<Token>(tokens);
            this.currentPosition = 0;
        }

        public void PushBack(Token token)
        {
            tokens.Enqueue(token);
        }

        public Token Consume()
        {
            if (IsEmpty())
            {
                throw new TokenStreamEmptyException();
            }

            Token token = tokens.Dequeue();
            currentPosition++;
            return token;
        }

        public Token Consume(TokenType expectedType)
        {
            Token token = Consume();

            if (token.Type != expectedType)
            {
                throw new TokenStreamWrongTypeException("Unexpected token");
            }

            return token;
        }

        public Token Peek()
        {
            return tokens.FirstOrDefault(); // Returns null if empty
        }

        public List<Token> PeekN(int count)
        {
            if (currentPosition + count >= tokens.Count)
            {
                throw new TokenStreamEmptyException();
            }

            return tokens.Skip(currentPosition).Take(count).ToList();
        }

        public Token PeekUntilNonLinebreak()
        {
            for (int i = currentPosition; i < tokens.Count; i++)
            {
                if (tokens.ElementAt(i).Type != TokenType.Linebreak)
                {
                    currentPosition = i;
                    return tokens.ElementAt(i);
                }
            }

            throw new TokenStreamEmptyException();
        }

        public bool IsEmpty()
        {
            return currentPosition >= tokens.Count;
        }

        // Iterator interface implementation
        public IEnumerator<Token> GetEnumerator()
        {
            return tokens.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();

        }
    }
}
