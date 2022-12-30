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
            sb.Append(this._value[i].String().RepresentAs<string>());
        }
        sb.Append(']');
        return ReamString.From(sb.ToString());
    }

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
    
    public ReamNumber Length() => ReamNumber.From(this._value.Count);
}
