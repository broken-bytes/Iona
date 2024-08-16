using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer.Tokens
{
    public class TokenStreamException: Exception
    {
        public TokenStreamException(string message) : base(message) { }
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
                throw new TokenStreamException("End of file");
            }

            Token token = tokens.Dequeue();
            currentPosition++;
            return token;
        }

        public Token ConsumeExpectingType(TokenType expectedType)
        {
            Token token = Consume();

            if (token.Type != expectedType)
            {
                throw new TokenStreamException("Unexpected token");
            }

            return token;
        }

        public Token? Peek()
        {
            return tokens.FirstOrDefault(); // Returns null if empty
        }

        public List<Token> PeekN(int count)
        {
            if (currentPosition + count >= tokens.Count)
            {
                throw new TokenStreamException("Peeked past end of token stream");
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

            throw new TokenStreamException("Peeked past end of token stream");
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
