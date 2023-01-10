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
            this.Type = type;
            this.Raw = raw;
            this.Literal = value;
            this.Line = line;
        }

        public Token(string raw)
        {
            this.Type = TokenType.Identifier;
            this.Raw = raw;
            this.Literal = null;
            this.Line = -1;
        }

        public override string ToString()
        {
            return $"{this.Type} {this.Raw} {this.Literal}";
        }


        public override bool Equals(object obj)
        {
            if (obj is Token tok)
                return this.Type == tok.Type
                    && this.Raw == tok.Raw;
            return base.Equals(obj);
        }
    }
}