namespace Lexer.Tokens
{
    public class TokenStreamException : Exception
    {
        public Token ErrorToken { get; }

        public TokenStreamException(Token errorToken, string message) : base(message)
        {
            this.ErrorToken = errorToken;
        }
    }

    public class TokenStreamWrongTypeException : TokenStreamException
    {
        public TokenStreamWrongTypeException(Token errorToken, string message) : base(errorToken, message)
        {

        }
    }

    public class TokenStreamEmptyException : TokenStreamException
    {
        public TokenStreamEmptyException(Token errorToken, string message) : base(errorToken, message) { }
    }

    public class TokenStream : IEnumerable<Token>
    {
        private readonly Queue<Token> _tokens;

        public TokenStream(List<Token> tokens)
        {
            _tokens = new Queue<Token>(tokens);
        }

        public Token Consume()
        {
            if (IsEmpty())
            {
                throw new TokenStreamEmptyException(
                     new Token
                     {
                         Family = TokenFamily.Error,
                         Type = TokenType.Error,
                         Value = "",
                         File = this.First().File,
                         Line = 0,
                         ColumnStart = 0,
                         ColumnEnd = 0,
                         Error = $"Expected token, got EOF"
                     },
                    $"Expected token, got EOF"
                );
            }

            Token token = _tokens.Dequeue();
            return token;
        }

        public Token Consume(TokenType expectedType, TokenType panicUntil)
        {
            if (IsEmpty())
            {
                throw new TokenStreamEmptyException(
                    new Token
                    {
                        Family = TokenFamily.Error,
                        Type = TokenType.Error,
                        Value = "",
                        File = this.First().File,
                        Line = 0,
                        ColumnStart = 0,
                        ColumnEnd = 0,
                        Error = $"Expected {expectedType}, got EOF"
                    },
                    $"Expected {expectedType}, got EOF"
                );
            }

            Token token = Consume();

            if (token.Type != expectedType)
            {
                var errorToken = new Token
                {
                    Family = TokenFamily.Error,
                    Type = TokenType.Error,
                    Value = token.Value,
                    File = token.File,
                    Line = token.Line,
                    ColumnStart = token.ColumnStart,
                    ColumnEnd = token.ColumnEnd,
                    Error = $"Expected {expectedType}, got {token.Type}"
                };

                Panic(panicUntil);

                throw new TokenStreamWrongTypeException(errorToken, $"Expected {expectedType}, got {token.Type}");
            }

            return token;
        }

        public Token Consume(TokenType expectedType, TokenFamily panicUntil)
        {
            if (IsEmpty())
            {
                throw new TokenStreamEmptyException(
                    new Token
                    {
                        Family = TokenFamily.Error,
                        Type = TokenType.Error,
                        Value = "",
                        File = this.First().File,
                        Line = 0,
                        ColumnStart = 0,
                        ColumnEnd = 0,
                        Error = $"Expected {expectedType}, got EOF"
                    },
                    $"Expected {expectedType}, got EOF"
                );
            }

            Token token = Consume();

            if (token.Type != expectedType)
            {
                var errorToken = new Token
                {
                    Family = TokenFamily.Error,
                    Type = TokenType.Error,
                    Value = token.Value,
                    File = token.File,
                    Line = token.Line,
                    ColumnStart = token.ColumnStart,
                    ColumnEnd = token.ColumnEnd,
                    Error = $"Expected {expectedType}, got {token.Type}"
                };

                Panic(panicUntil);

                throw new TokenStreamWrongTypeException(errorToken, $"Expected {expectedType}, got {token.Type}");
            }

            return token;
        }

        public Token Consume(TokenFamily family, TokenType panicUntil)
        {
            if (IsEmpty())
            {
                throw new TokenStreamEmptyException(
                    new Token
                    {
                        Family = TokenFamily.Error,
                        Type = TokenType.Error,
                        Value = "",
                        File = this.First().File,
                        Line = 0,
                        ColumnStart = 0,
                        ColumnEnd = 0,
                        Error = $"Expected {family}, got EOF"
                    },
                    $"Expected {family}, got EOF"
                );
            }

            Token token = Consume();

            if (token.Family != family)
            {
                var errorToken = new Token
                {
                    Family = TokenFamily.Error,
                    Type = TokenType.Error,
                    Value = token.Value,
                    File = token.File,
                    Line = token.Line,
                    ColumnStart = token.ColumnStart,
                    ColumnEnd = token.ColumnEnd,
                    Error = $"Expected {family}, got {token.Family}"
                };

                Panic(panicUntil);

                throw new TokenStreamWrongTypeException(errorToken, $"Expected {family}, got {token.Family}");
            }

            return token;
        }

        public Token Consume(TokenFamily family, TokenFamily panicUntil)
        {
            if (IsEmpty())
            {
                throw new TokenStreamEmptyException(
                    new Token
                    {
                        Family = TokenFamily.Error,
                        Type = TokenType.Error,
                        Value = "",
                        File = this.First().File,
                        Line = 0,
                        ColumnStart = 0,
                        ColumnEnd = 0,
                        Error = $"Expected {family}, got EOF"
                    },
                    $"Expected {family}, got EOF"
                );
            }

            Token token = Consume();

            if (token.Family != family)
            {
                var errorToken = new Token
                {
                    Family = TokenFamily.Error,
                    Type = TokenType.Error,
                    Value = token.Value,
                    File = token.File,
                    Line = token.Line,
                    ColumnStart = token.ColumnStart,
                    ColumnEnd = token.ColumnEnd,
                    Error = $"Expected {family}, got {token.Family}"
                };

                Panic(panicUntil);

                throw new TokenStreamWrongTypeException(errorToken, $"Expected {family}, got {token.Family}");
            }

            return token;
        }

        public Token Peek()
        {
            return _tokens.FirstOrDefault(); // Returns null if empty
        }

        public List<Token> Peek(int count)
        {
            if (count > _tokens.Count)
            {
                var errorToken = new Token
                {
                    Family = TokenFamily.Error,
                    Type = TokenType.Error,
                    Value = "",
                    File = this.First().File,
                    Line = 0,
                    ColumnStart = 0,
                    ColumnEnd = 0,
                    Error = $"Got EOF"
                };

                throw new TokenStreamEmptyException(errorToken, $"Expected {count} tokens, got EOF");
            }

            return _tokens.Skip(0).Take(count).ToList();
        }

        public bool IsEmpty()
        {
            return _tokens.Count <= 0;
        }

        // Iterator interface implementation
        public IEnumerator<Token> GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Panics until a token of the given type is found
        /// </summary>
        /// <param name="type"></param>
        public void Panic(TokenType type)
        {
            while (!IsEmpty())
            {
                if (Peek().Type == type)
                {
                    return;
                }

                Consume();
            }
        }

        /// <summary>
        /// Panics until a token of the given family is found
        /// </summary>
        /// <param name="family"></param>
        /// <exception cref="TokenStreamEmptyException"></exception>
        public void Panic(TokenFamily family)
        {
            while (!IsEmpty())
            {
                if (Peek().Family == family)
                {
                    return;
                }

                Consume();
            }
        }

        public void Append(Token token)
        {
            _tokens.Enqueue(token);
        }

        public TokenStream Copy()
        {
            return new TokenStream(_tokens.ToList());
        }
    }
}
