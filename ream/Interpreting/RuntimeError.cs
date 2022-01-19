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
    }
    public class Return : Exception
    {
        public readonly object Value;

        public Return(object value) : base(null, null)
        {
            Value = value;
        }
    }
}
