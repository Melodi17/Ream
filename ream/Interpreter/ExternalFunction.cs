using System;
using Ream.Lexer;

namespace Ream.Interpreter
{
    public class ExternalFunction : IFunction
    {
        public string Name => _name;
        private string _name;
        public Func<Token[], Token> Action => _action;
        private Func<Token[], Token> _action;

        public ExternalFunction(string name, Func<Token[], Token> action)
        {
            _name = name;
            _action = action;
        }

        public Token Invoke(Interpreter interpreter, Scope scope, Token[] parameters)
        {
            Token res = _action.Invoke(parameters);
            return res;
        }
    }
}
