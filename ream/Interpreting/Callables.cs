using Ream.Parsing;

namespace Ream.Interpreting
{
    public class ExternalFunction : ICallable
    {
        public Func<Interpreter, List<object>, object> _func;
        private int _argumentCount;
        public ExternalFunction(Func<Interpreter, List<object>, object> func, int argumentCount)
        {
            _func = func;
            _argumentCount = argumentCount;
        }
        public int ArgumentCount()
            => _argumentCount;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            return _func.Invoke(interpreter, arguments);
        }
    }

    public class Function : ICallable
    {
        private readonly Stmt.Function Declaration;

        public Function(Stmt.Function declaration)
        {
            Declaration = declaration;
        }

        public int ArgumentCount()
        {
            return Declaration.parameters.Count;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            Scope scope = new(interpreter.Globals);
            for (int i = 0; i < Declaration.parameters.Count; i++)
            {
                scope.SetLocal(Declaration.parameters[i], arguments[i]);
            }

            try
            {
                interpreter.ExecuteBlock(Declaration.body, scope);
            }
            catch (Return returnVal)
            {
                return returnVal.Value;
            }
            return null;
        }
    }
}
