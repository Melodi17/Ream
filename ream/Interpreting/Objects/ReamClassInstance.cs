﻿namespace ream.Interpreting.Objects;

public abstract class ReamClassInstance : ReamObject
{
    // Core behaviours
    public override abstract object Represent();
    public override abstract ReamString Type();
    public override ReamBoolean Truthy() => ReamBoolean.True;

    // Behaviours
    public override abstract ReamSequence Iterate();
    public override abstract ReamObject Index(ReamObject index);
    public override abstract ReamObject SetIndex(ReamObject index, ReamObject newValue);
    public override abstract ReamObject Call(ReamSequence args);
    public override abstract ReamString String();

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

    // Comparison operators
    public override abstract ReamBoolean Equal(ReamObject other);
    public override abstract ReamBoolean NotEqual(ReamObject other);
    public override abstract ReamBoolean Less(ReamObject other);
    public override abstract ReamBoolean Greater(ReamObject other);
    public override abstract ReamBoolean LessEqual(ReamObject other);
    public override abstract ReamBoolean GreaterEqual(ReamObject other);

    // Arithmetic operators
    public override abstract ReamObject Add(ReamObject other);
    public override abstract ReamObject Subtract(ReamObject other);
    public override abstract ReamObject Multiply(ReamObject other);
    public override abstract ReamObject Divide(ReamObject other);
    public override abstract ReamObject Modulo(ReamObject other);

    // Unary operators
    public override abstract ReamObject Negate();
    public override abstract ReamBoolean Not();
}