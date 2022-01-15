using System;
using Ream.Lexer;
using Ream.Parser;

namespace Ream.Interpreter
{
    public class Function : IFunction
    {
        public string Name => _name;
        private string _name;
        public string[] ParameterNames => _parameterNames;
        private string[] _parameterNames;
        public Node Node => _node;
        private Node _node;

        public Function(string name, string[] parameterNames, Node node)
        {
            _name = name;
            _parameterNames = parameterNames;
            _node = node;
        }

        public Token Invoke(Interpreter interpreter, Scope scope, Token[] parameters)
        {
            Scope localScope = scope.CreateChild();
            for (int i = 0; i < Math.Min(parameters.Length, _parameterNames.Length); i++)
            {
                localScope.Set(_parameterNames[i], parameters[i]);
            }
            return interpreter.Dive(_node, localScope, true);
        }
    }
}
