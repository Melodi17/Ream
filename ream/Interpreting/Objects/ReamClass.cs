using Ream.Interpreting;

namespace ream.Interpreting.Objects;

public abstract class ReamClass : ReamObject
{
    // Core behaviours
    public override abstract object Represent();
    public override abstract ReamString Type();
    public override ReamBoolean Truthy() => ReamBoolean.True;

    // Behaviours
    public override ReamObject Member(string name)
    {
        ReamObject member = base.Member(name);
        if (member == ReamNull.Instance)
            member = this.ClassMember(name);
        
        return member;
    }
    protected abstract ReamObject ClassMember(string name);

    public override ReamObject SetMember(string name, ReamObject value)
    {
        ReamObject member = base.SetMember(name, value);
        if (member == ReamNull.Instance)
            member = this.SetClassMember(name, value);
        
        return member;
    }
    protected abstract ReamObject SetClassMember(string name, ReamObject value);

    public override ReamString String() => ReamString.From("<class>");
    public override abstract ReamObject New(ReamSequence args);
}