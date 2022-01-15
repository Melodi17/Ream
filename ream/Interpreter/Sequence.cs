using Ream.Lexer;

namespace Ream.Interpreter
{
    public class Sequence
    {
        public Token[] Value;
        public Sequence(Token[] value)
        {
            Value = value;
        }
        public override string ToString()
        {
            return string.Join<Token>(", ", Value);
        }
    }
}
