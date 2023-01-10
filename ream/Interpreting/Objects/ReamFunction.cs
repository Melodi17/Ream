namespace ream.Interpreting.Objects;

public abstract class ReamFunction : ReamObject
{
    protected ReamFunction() { }

    // Conversions

    // Core behaviours
    public override abstract object Represent();
    public override ReamString Type() => ReamString.From("function");
    public override ReamBoolean Truthy() => ReamBoolean.True;

    // Behaviours
    public override abstract ReamObject Call(ReamSequence args);
    public override ReamString String() => ReamString.From("<function>");

    public abstract ReamFunction Bind(object obj);
}