namespace ream.Interpreting.Objects;

public class ReamString : ReamObject
{
    public static ReamString Empty = new(string.Empty);
    
    private readonly string _value;

    private ReamString(string value)
    {
        this._value = value;
    }

    // Conversions
    public static ReamString From(string value) => new(value);
    public static ReamString From(char value) => new(value.ToString());


    // Core behaviours
    public override object Represent() => this._value;
    public override ReamString Type() => ReamString.From("string");
    public override ReamBoolean Truthy() => ReamBoolean.From(this._value.Length > 0);

    // Behaviours
    public override ReamSequence Iterate() => ReamSequence.From(this._value.Select(ReamString.From));
    public override ReamObject Index(ReamObject index) => ReamString.From(this._value[index.RepresentAs<int>()]);
    public override ReamString String() => ReamString.From($"'{this._value}'");
    public override ReamObject Member(string name)
    {
        return name switch
        {
            // Properties
            nameof(this.Length) => this.Length(),
            
            // Methods
            nameof(this.Contains) => ReamFunctionExternal.From(this.Contains),
            nameof(this.Starts) => ReamFunctionExternal.From(this.Starts),
            nameof(this.Ends) => ReamFunctionExternal.From(this.Ends),
            nameof(this.Replace) => ReamFunctionExternal.From(this.Replace),
            nameof(this.Substring) => ReamFunctionExternal.From(this.Substring),
            nameof(this.Upper) => ReamFunctionExternal.From(this.Upper),
            nameof(this.Lower) => ReamFunctionExternal.From(this.Lower),
            nameof(this.Trim) => ReamFunctionExternal.From(this.Trim),
            nameof(this.Split) => ReamFunctionExternal.From(this.Split),

            _ => base.Member(name)
        };
    }

    // Arithmetic operators
    public override ReamObject Add(ReamObject other)
        => ReamString.From(this._value
            + (other is ReamString s
                ? s._value
                : other.String().RepresentAs<string>()));

    public override ReamObject Multiply(ReamObject other)
        => ReamString.From(
            string.Concat(
                Enumerable.Repeat(this._value, other.RepresentAs<int>())));
    
    
    public ReamNumber Length() => ReamNumber.From(this._value.Length);
    public ReamBoolean Contains(ReamString other) => ReamBoolean.From(this._value.Contains(other._value));
    public ReamBoolean Starts(ReamString other) => ReamBoolean.From(this._value.StartsWith(other._value));
    public ReamBoolean Ends(ReamString other) => ReamBoolean.From(this._value.EndsWith(other._value));
    public ReamString Replace(ReamString old, ReamString @new) => ReamString.From(this._value.Replace(old._value, @new._value));
    public ReamString Substring(ReamNumber start, ReamNumber end) => ReamString.From(this._value.Substring(start.RepresentAs<int>(), end.RepresentAs<int>()));
    public ReamString Upper() => ReamString.From(this._value.ToUpper());
    public ReamString Lower() => ReamString.From(this._value.ToLower());
    public ReamString Trim() => ReamString.From(this._value.Trim());
    public ReamSequence Split(ReamString separator) => ReamSequence.From(this._value.Split(separator._value));
}
