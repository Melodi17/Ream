using Ream.Lexing;

namespace Ream.Interpreting
{
    public class RuntimeError : Exception
    {
        public readonly Token Token;
        public RuntimeError(Token token, string message) : base(message)
        {
            this.Token = token;
        }
        public RuntimeError(string message) : base(message)
        {
            this.Token = null;
        }
    }
}
