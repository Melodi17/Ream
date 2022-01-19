namespace Ream.Interpreting
{
    public class Return : Exception
    {
        public readonly object Value;

        public Return(object value) : base(null, null)
        {
            Value = value;
        }
    }
}
