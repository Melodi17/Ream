namespace ream.Interpreting.Objects;

public class ReamBoolean : ReamObject
{
    private readonly bool _value;
    public bool Value => this._value;

    public static readonly ReamBoolean True = new(true);
    public static readonly ReamBoolean False = new(false);
    private ReamBoolean(bool value)
    {
        this._value = value;
    }
    
    // Conversions
    public static ReamBoolean From(bool value) => value ? True : False;

    // Core behaviours
    protected override void DisposeManaged() { /* Do nothing */ }
    protected override void DisposeUnmanaged() { /* Do nothing */ }
    public override object Represent() => this._value;
    public override ReamString Type() => ReamString.From("boolean");
    public override ReamBoolean Truthy() => this;
    
    // Behaviours
    public override ReamString String() => ReamString.From(this._value ? "true" : "false");
    
    // Unary operators
    public override ReamBoolean Not() => this._value ? False : True;
}
