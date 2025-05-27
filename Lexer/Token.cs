namespace Cobweb{
    public struct Token
    {
        public TokenType Type;
        public string Data;
        public int Column, Row;
        public Token()
        {
            Data = "";
            Type = TokenType.END_OF_FILE;
            Column = -1;
            Row = -1;
        }
        public Token(string data, TokenType type, int column, int row)
        {
            Type = type;
            Data = data;
            Column = column;
            Row = row;
        }
        public override string ToString()
        {
            return $"TOKEN:({Type}, {Data})";
        }
    }
}