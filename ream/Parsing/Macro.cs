using Ream.Lexing;

namespace Ream.Parsing
{
    public class Macro
    {
        public List<Token> Body { get; set; }
        public Macro(List<Token> body)
        {
            this.Body = body;
        }

        public List<Token> Call()
        {
            return Body;
        }
    }
}
