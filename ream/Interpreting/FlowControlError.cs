using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ream.Lexing;

namespace Ream.Interpreting
{
    public abstract class FlowControlError : Exception
    {
        public Token SourceToken { get; private set; }
        public FlowControlError(Token source) : base(null, null)
        {
            SourceToken = source;
        }
    }
    public class Return : FlowControlError
    {
        public readonly object Value;

        public Return(Token source, object value) : base(source)
        {
            Value = value;
        }
    }
    public class Break : FlowControlError
    {
        public Break(Token source) : base(source) { }
    }
    public class Continue : FlowControlError
    {
        public Continue(Token source) : base(source) { }
    }
}
