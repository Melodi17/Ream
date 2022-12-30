namespace ream.Interpreting.Objects;

public class ReamNull : ReamObject
{
    public static readonly ReamNull Instance = new();
    
    private ReamNull () {}


    // Core behaviours
    public override ReamString Type() => ReamString.From("null");
    public override ReamBoolean Truthy() => ReamBoolean.False;
    public override object Represent() => null;
    
    // Behaviours
    public override ReamString String() => ReamString.From("null");
}
