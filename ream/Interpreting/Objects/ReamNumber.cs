namespace ream.Interpreting.Objects;

public class ReamNumber : ReamObject
{
    private readonly double _value;

    private ReamNumber(double value)
    {
        this._value = value;
    }
    
    // Conversions
    public static ReamNumber From(double value) => new(value);
    public static ReamNumber From(float value) => new(value);
    public static ReamNumber From(decimal value) => new((double)value);
    public static ReamNumber From(int value) => new(value);
    public static ReamNumber From(long value) => new(value);
    public static ReamNumber From(short value) => new(value);
    public static ReamNumber From(byte value) => new(value);
    
    // Core behaviours
    public override object Represent() => this._value;
    public override ReamString Type() => ReamString.From("number");
    public override ReamBoolean Truthy() => ReamBoolean.From(this._value != 0);

    // Behaviours
    public override ReamString String()
    {
        // Tostring it, but if it ends with .0, remove it
        string str = this._value.ToString();
        if (str.EndsWith(".0"))
            str = str[..^2];
        
        return ReamString.From(str);
    }
    
    // Comparison operators
    public override ReamBoolean Less(ReamObject other)
        => ReamBoolean.From(other is ReamNumber number && this._value < number._value);
    
    public override ReamBoolean Greater(ReamObject other)
        => ReamBoolean.From(other is ReamNumber number && this._value > number._value);
    
    // Arithmetic operators
    public override ReamObject Add(ReamObject other)
        => other is ReamNumber number 
            ? ReamNumber.From(this._value + number._value) 
            : ReamNull.Instance;
    
    public override ReamObject Subtract(ReamObject other)
        => other is ReamNumber number 
            ? ReamNumber.From(this._value - number._value) 
            : ReamNull.Instance;
    
    public override ReamObject Multiply(ReamObject other)
        => other is ReamNumber number 
            ? ReamNumber.From(this._value * number._value) 
            : ReamNull.Instance;
    
    public override ReamObject Divide(ReamObject other)
        => other is ReamNumber number && number._value != 0
            ? ReamNumber.From(this._value / number._value) 
            : ReamNull.Instance;
    
    public override ReamObject Modulo(ReamObject other)
        => other is ReamNumber number && number._value != 0
            ? ReamNumber.From(this._value % number._value) 
            : ReamNull.Instance;

    // Unary operators
    public override ReamObject Negate() => ReamNumber.From(-this._value);
}
