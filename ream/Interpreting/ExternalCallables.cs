namespace Ream.Interpreting
{
    public class ExternalCustomCallable : ICallable
    {
        public Func<Interpreter, List<object>, object> _func;
        private int _argumentCount;
        public ExternalCustomCallable(Func<Interpreter, List<object>, object> func, int argumentCount)
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
}
