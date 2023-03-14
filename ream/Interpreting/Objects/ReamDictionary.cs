using System.Collections;
using System.Net.Security;
using System.Text;

namespace ream.Interpreting.Objects;

public class ReamDictionary : ReamObject
{
    public static ReamDictionary Empty => new(new Dictionary<ReamObject, ReamObject>());
    private readonly Dictionary<ReamObject, ReamObject> _value;

    private ReamDictionary(IDictionary<ReamObject, ReamObject> value)
    {
        this._value = new(value);
    }

    // Conversions
    public static ReamDictionary From<TK, TV>(IDictionary<TK, TV> map) => new(map.ToDictionary(
        item => ReamObjectFactory.Create(item.Key),
        item => ReamObjectFactory.Create(item.Value))
    );

    // Core behaviours
    protected override void DisposeManaged()
    {
        /* Do nothing */
    }

    protected override void DisposeUnmanaged()
    {
        this._value.Clear();
    }

    public override object Represent() => this._value.Select(
        item => new KeyValuePair<object, object>(item.Key.Represent(), item.Value.Represent())
    ).ToDictionary(item => item.Key, item => item.Value);

    public override ReamString Type() => ReamString.From("dictionary");
    public override ReamBoolean Truthy() => ReamBoolean.From(this._value.Count > 0);

    // Behaviours
    public override ReamSequence Iterate() => ReamSequence.From(this._value.Keys);
    public override ReamObject Index(ReamObject index) => this._value[index];

    public override ReamObject SetIndex(ReamObject index, ReamObject newValue)
    {
        this._value[index] = newValue;
        return newValue;
    }

    public override ReamString String()
    {
        string text = "{" + string.Join(", ", this._value.Select(
            item => $"{item.Key.String().Represent()}: {item.Value.String().Represent()}"
        )) + "}";
        return ReamString.From(text);
    }

    public override ReamObject Member(string name) => name switch
    {
        "length" => this.Length,
        "remove" => ReamFunctionExternal.From(this.Remove),
        "contains" => ReamFunctionExternal.From(this.Contains),
        _ => base.Member(name),
    };

    // Arithmetic operators

    public ReamObject Remove(ReamObject item)
    {
        ReamObject result = this._value[item];
        this._value.Remove(item);
        return result;
    }

    public ReamBoolean Contains(ReamObject item) => ReamBoolean.From(this._value.ContainsKey(item));

    public ReamNumber Length => ReamNumber.From(this._value.Count);

    public ReamObject this[ReamObject index] { get => this.Index(index); set => this.SetIndex(index, value); }
}
