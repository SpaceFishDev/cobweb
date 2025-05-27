namespace Cobweb{
    public class Lexer
    {
        public List<Token> Tokens;
        public int Position;
        public string Source;
        public int Column, Line;
        char Current
        {
            get
            {
                if (Position >= Source.Length)
                {
                    return (char)0;
                }
                return Source[Position]; 
            }
        }
        public void Next()
        {
            ++Position;
            ++Column;
        }

        public void NewLine()
        {
            ++Position;
            ++Line;
            Column = 0;
        }

        public Lexer()
        {
            Tokens = new();
            Position = 0;
            Source = "";
        }
        public Lexer(string src)
        {
            Source = src;
            Tokens = new();
            Position = 0;
        }
        private bool IsAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }
        private bool IsNum(char c)
        {
            return c >= '0' && c <= '9';
        }
        public Token Lex()
        {
            switch (Current)
            {
                case ' ':
                    {
                        Next();
                        return Lex();
                    }
                case '\n':
                    {
                        NewLine();
                        return Lex();
                    }
                case',':
                    {
                        Next();
                        return new(",", TokenType.COMMA, Column, Line);        
                    }
                case '[':
                    {
                        Next();
                        return new("[", TokenType.SQUARE_OPEN, Column, Line);
                    }
                case ']':
                    {
                        Next();
                        return new("]", TokenType.SQUARE_CLOSE, Column, Line);
                    }
                case '"':
                    {
                        string result = "";
                        Next();
                        while (Current != '"')
                        {
                            if (Current == '\n')
                            {
                                NewLine();
                            }
                            result += Current;
                            Next();
                        }
                        Next();
                        return new(result, TokenType.STRING, Column, Line);
                    }
                case '+':
                    {
                        Next();
                        return new("+", TokenType.PLUS, Column, Line);
                    }
                case '-':
                    {
                        Next();
                        if (char.IsNumber(Current))
                        {
                            --Position;
                            --Position;
                            if (char.IsWhiteSpace(Current))
                            {
                                Position += 2;
                                var t = Lex();
                                t.Data = "-" + t.Data;
                                return t;
                            }
                            Position += 2;
                        }
                        return new("-", TokenType.MINUS, Column, Line);
                    }
                case '/':
                    {
                        Next();
                        return new("/", TokenType.DIVIDE, Column, Line);
                    }
                case '*':
                    {
                        Next();
                        return new("*", TokenType.MULTIPLY, Column, Line);
                    }
                case '=':
                    {
                        if ((Position + 1) > Source.Length)
                        {
                            return new();
                        }
                        Next();
                        if (Current == '=')
                        {
                            Next();
                            return new("==", TokenType.BOOLEQ, Column, Line);
                        }
                        if (Current == '>')
                        {
                            Next();
                            return new("=>", TokenType.ARROW, Column, Line);
                        }
                        return new();
                    }
                case '>':
                    {
                        if ((Position + 1) > Source.Length)
                        {
                            return new(">", TokenType.BOOLMORE, Column, Line);
                        }
                        Next();
                        if (Current == '=')
                        {
                            Next();
                            return new("=>", TokenType.BOOLMOREEQ, Column, Line);
                        }
                        return new(">", TokenType.BOOLMORE, Column, Line);
                    }
                case '<':
                    {
                        if ((Position + 1) > Source.Length)
                        {
                            return new("<", TokenType.BOOLLESS, Column, Line);
                        }
                        Next();
                        if (Current == '=')
                        {
                            Next();
                            return new("=<", TokenType.BOOLLESSEQ, Column, Line);
                        }
                        return new("<", TokenType.BOOLLESS, Column, Line);
                    }
                case '(':
                    {
                        Next();
                        return new("(", TokenType.BRACE_OPEN, Column, Line);
                    }
                case ')':
                    {
                        Next();
                        return new(")", TokenType.BRACE_CLOSE, Column, Line);
                    }
                case '#':
                    {
                        while (Current != '\n' && Current != '\0')
                        {
                            Next();
                        }
                        return Lex();    
                    }
            }
            if (IsAlpha(Current))
            {
                string result = "";
                while (IsAlpha(Current) || Current == '_' || IsNum(Current))
                {
                    result += Current;
                    Next();
                }
                return new(result, TokenType.ID, Column, Line);
            }
            if (IsNum(Current))
            {
                string result = "";
                bool foundDecimal = false;
                while (IsNum(Current) ||(Current == '.' && !foundDecimal))
                {
                    if (Current == '.')
                    {
                        foundDecimal = true;    
                    }
                    result += Current;
                    Next();
                }
                return new(result, TokenType.NUMBER, Column, Line);
            }
            return new();
        }
        public void LexAll()
        {
            while (true)
            {
                Token t = Lex();
                Tokens.Add(t);
                if (t.Type == TokenType.END_OF_FILE)
                {
                    return;
                }
            }
        }

    }
}