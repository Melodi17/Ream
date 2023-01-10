namespace ream.Interpreting.Objects;

public class ReamNull : ReamObject
{
    public static readonly ReamNull Instance = new();
    
    private ReamNull () {}


    // Core behaviours
    protected override void DisposeManaged() { /* Do nothing */ }
    protected override void DisposeUnmanaged() { /* Do nothing */ }
    public override ReamString Type() => ReamString.From("null");
    public override ReamBoolean Truthy() => ReamBoolean.False;
    public override object Represent() => null;
    
    // Behaviours
    public override ReamString String() => ReamString.From("null");
}
