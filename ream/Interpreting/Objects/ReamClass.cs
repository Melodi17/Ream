using Ream.Interpreting;

namespace ream.Interpreting.Objects;

public abstract class ReamClass : ReamObject
{
    // Core behaviours
    public override abstract object Represent();
    public override abstract ReamString Type();
    public override ReamBoolean Truthy() => ReamBoolean.True;

    // Behaviours
    public override abstract ReamObject Member(string name);
    public override abstract ReamObject SetMember(string name, ReamObject value);

    public override ReamString String() => ReamString.From("<class>");
    public override abstract ReamObject New(ReamSequence args);
}