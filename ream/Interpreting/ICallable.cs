namespace Ream.Interpreting
{
    public interface ICallable
    {
        public int ArgumentCount();
        public object Call(Interpreter interpreter, List<object> arguments);
    }
}
