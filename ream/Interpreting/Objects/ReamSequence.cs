using System.Collections;
using System.Text;

namespace ream.Interpreting.Objects;

public class ReamSequence : ReamObject
{
    public static ReamSequence Empty => new(Array.Empty<ReamObject>());
    private readonly List<ReamObject> _value;

    private ReamSequence(IEnumerable<ReamObject> value)
    {
        this._value = value.ToList();
    }

    // Conversions
    public static ReamSequence From<T>(IEnumerable<T> list) => new(list.Select(x => ReamObjectFactory.Create(x)));

    // Core behaviours
    protected override void DisposeManaged() { /* Do nothing */ }
    protected override void DisposeUnmanaged()
    {
        this._value.Clear();
    }

    public override object Represent() => this._value.Select(x => x.Represent()).ToList();
    public override ReamString Type() => ReamString.From("sequence");
    public override ReamBoolean Truthy() => ReamBoolean.From(this._value.Count > 0);

    // Behaviours
    public override ReamSequence Iterate() => this;
    public override ReamObject Index(ReamObject index) => this._value[index.RepresentAs<int>()];

    public override ReamObject SetIndex(ReamObject index, ReamObject newValue)
    {
        this._value[index.RepresentAs<int>()] = newValue;
        return newValue;
    }

    public override ReamString String()
    {
        StringBuilder sb = new();
        sb.Append('[');
        for (int i = 0; i < this._value.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(this._value[i].String().Value);
        }
        sb.Append(']');
        return ReamString.From(sb.ToString());
    }

    public override ReamObject Member(string name) => name switch
    {
        "length" => this.Length,
        "append" => ReamFunctionExternal.From(this.Append),
        "remove" => ReamFunctionExternal.From(this.Remove),
        "insert" => ReamFunctionExternal.From(this.Insert),
        "desert" => ReamFunctionExternal.From(this.Desert),
        "contains" => ReamFunctionExternal.From(this.Contains),
        "find" => ReamFunctionExternal.From(this.Find),
        "join" => ReamFunctionExternal.From(this.Join),
        _ => base.Member(name),
    };

    // Arithmetic operators
    public override ReamObject Add(ReamObject other)
    {
        List<ReamObject> newList = new(this._value);
        newList.Add(other);
        return new ReamSequence(newList);
    }

    public override ReamObject Subtract(ReamObject other)
    {
        List<ReamObject> newList = new(this._value);
        newList.Remove(other);
        return new ReamSequence(newList);
    }

    public ReamObject Append(ReamObject item)
    {
        this._value.Add(item);
        return item;
    }

    public ReamObject Remove(ReamObject item)
    {
        this._value.Remove(item);
        return item;
    }

    public ReamObject Insert(ReamObject index, ReamObject item)
    {
        this._value.Insert(index.RepresentAs<int>(), item);
        return item;
    }

    public ReamObject Desert(ReamObject index)
    {
        ReamObject item = this._value[index.RepresentAs<int>()];
        this._value.RemoveAt(index.RepresentAs<int>());
        return item;
    }

    public ReamBoolean Contains(ReamObject item) => ReamBoolean.From(this._value.Contains(item));
    public ReamNumber Find(ReamObject item) => ReamNumber.From(this._value.IndexOf(item));
    public ReamString Join(ReamString separator) => ReamString.From(string.Join(separator.Value, this._value.Select(x => x.String().Value)));

    public ReamNumber Length => ReamNumber.From(this._value.Count);

    public ReamObject this[ReamNumber index] { get => this.Index(index); set => this.SetIndex(index, value); }
}
