namespace ream.Interpreting.Objects;

public class ReamString : ReamObject
{
    private static readonly Dictionary<string, ReamString> cache = new();
    public static ReamString Empty = new(string.Empty);
    
    private readonly string _value;
    public string Value => this._value;
    private ReamString(string value)
    {
        this._value = value;
    }

    // Conversions
    public static ReamString From(string value)
    {
        if (!cache.ContainsKey(value))
            cache.Add(value, new(value));
        
        return cache[value];
    }
    public static ReamString From(char value) => From(value.ToString());


    // Core behaviours
    protected override void DisposeManaged() { /* Do nothing */ }
    protected override void DisposeUnmanaged()
    {
        if (cache.ContainsKey(this._value))
            cache.Remove(this._value);
    }
    public override object Represent() => this._value;
    public override ReamString Type() => ReamString.From("string");
    public override ReamBoolean Truthy() => ReamBoolean.From(this._value.Length > 0);

    // Behaviours
    public override ReamSequence Iterate() => ReamSequence.From(this._value.Select(ReamString.From));
    public override ReamObject Index(ReamObject index) => ReamString.From(this._value[index.RepresentAs<int>()]);
    public override ReamString String() => ReamString.From($"'{this._value}'");
    public override ReamObject Member(string name) => name switch
    {
        // Properties
        "length" => this.Length(),
            
        // Methods
        "contains" => ReamFunctionExternal.From(this.Contains),
        "starts" => ReamFunctionExternal.From(this.Starts),
        "ends" => ReamFunctionExternal.From(this.Ends),
        "replace" => ReamFunctionExternal.From(this.Replace),
        "substring" => ReamFunctionExternal.From(this.Substring),
        "upper" => ReamFunctionExternal.From(this.Upper),
        "lower" => ReamFunctionExternal.From(this.Lower),
        "trim" => ReamFunctionExternal.From(this.Trim),
        "split" => ReamFunctionExternal.From(this.Split),
        "find" => ReamFunctionExternal.From(this.Find),

        _ => base.Member(name),
    };

    // Arithmetic operators
    public override ReamObject Add(ReamObject other)
        => ReamString.From(this._value
            + (other is ReamString s
                ? s._value
                : other.String().Value));

    public override ReamObject Multiply(ReamObject other)
        => ReamString.From(
            string.Concat(
                Enumerable.Repeat(this._value, other.RepresentAs<int>())));
    
    
    public ReamNumber Length() => ReamNumber.From(this._value.Length);
    public ReamBoolean Contains(ReamString other) => ReamBoolean.From(this._value.Contains(other._value));
    public ReamBoolean Starts(ReamString other) => ReamBoolean.From(this._value.StartsWith(other._value));
    public ReamBoolean Ends(ReamString other) => ReamBoolean.From(this._value.EndsWith(other._value));
    public ReamString Replace(ReamString old, ReamString @new) => ReamString.From(this._value.Replace(old._value, @new._value));
    public ReamString Substring(ReamNumber start, ReamNumber end) => ReamString.From(this._value.Substring(start.IntValue, end.IntValue));
    public ReamString Upper() => ReamString.From(this._value.ToUpper());
    public ReamString Lower() => ReamString.From(this._value.ToLower());
    public ReamString Trim() => ReamString.From(this._value.Trim());
    public ReamSequence Split(ReamString separator) => ReamSequence.From(this._value.Split(separator._value));
    public ReamNumber Find(ReamString other) => ReamNumber.From(this._value.IndexOf(other._value, StringComparison.Ordinal));
}
