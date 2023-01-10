namespace ream.Interpreting.Objects;

public class ReamNumber : ReamObject
{
    private static readonly Dictionary<double, ReamNumber> cache = new();
    private readonly double _value;

    private ReamNumber(double value)
    {
        this._value = value;
    }
    
    // Conversions
    public static ReamNumber From(double value)
    {
        if (!cache.ContainsKey(value))
            cache.Add(value, new(value));
        
        return cache[value];
    }
    public static ReamNumber From(float value) => From((double)value);
    public static ReamNumber From(decimal value) => From((double)value);
    public static ReamNumber From(int value) => From((double)value);
    public static ReamNumber From(long value) => From((double)value);
    public static ReamNumber From(short value) => From((double)value);
    public static ReamNumber From(byte value) => From((double)value);
    
    // Core behaviours
    protected override void DisposeManaged() { /* Do nothing */ }

    protected override void DisposeUnmanaged()
    {
        if (cache.ContainsKey(this._value))
            cache.Remove(this._value);
    }
    public override object Represent() => this._value;
    public override T RepresentAs<T>()
    {
        return (T)Convert.ChangeType(this._value, typeof(T));
    }

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
