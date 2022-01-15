using Ream.Lexer;

namespace Ream.Interpreter
{
    public interface IFunction
    {
        public Token Invoke(Interpreter interpreter, Scope scope, Token[] parameters);
    }
}
