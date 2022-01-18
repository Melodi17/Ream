namespace Ream.Lexing
{
    public class Token
    {
        public readonly TokenType Type;
        public readonly string Raw;
        public readonly object Literal;
        public readonly int Line;

        public Token(TokenType type, string raw, object value, int line)
        {
            Type = type;
            Raw = raw;
            Literal = value;
            Line = line;
        }

        public override string ToString()
        {
            return $"{Type} {Raw} {Literal}";
        }
    }
}