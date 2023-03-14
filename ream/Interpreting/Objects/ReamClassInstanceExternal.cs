using System.Collections;
using System.Reflection;

namespace ream.Interpreting.Objects;

public class ReamClassInstanceExternal : ReamClassInstance
{
    private object _instance;
    
    private ReamClassInstanceExternal(object instance)
    {
        this._instance = instance;
    }

    public static ReamClassInstanceExternal From(object instance) => new(instance);
    
    protected override void DisposeManaged() { /* Do nothing */ }

    protected override void DisposeUnmanaged()
    {
        this._instance = null;
    }
    public override object Represent() => this._instance;
    public override ReamString Type() => ReamString.From(this._instance.GetType().Name);
    public override ReamSequence Iterate()
    {
        if (this._instance is IEnumerable enumerable)
            return ReamSequence.From(enumerable.Cast<object>().Select(ReamObjectFactory.Create));

        return ReamSequence.Empty;
    }

    public override ReamObject Index(ReamObject index)
    {
        if (this._instance is IList list)
        {
            int i = index.RepresentAs<int>();
            if (i >= 0 && i < list.Count)
                return ReamObjectFactory.Create(list[i]);
        }

        return ReamNull.Instance;
    }

    public override ReamObject SetIndex(ReamObject index, ReamObject newValue)
    {
        if (this._instance is IList list)
        {
            int i = index.RepresentAs<int>();
            if (i >= 0 && i < list.Count)
            {
                list[i] = newValue.RepresentAs(list[i]?.GetType());
                return newValue;
            }
        }

        return ReamNull.Instance;
    }

    public override ReamObject Call(ReamSequence args) => ReamNull.Instance;

    protected override ReamObject ClassMember(string name)
    {
        // Use reflection to get the member
        MemberInfo[] member = this._instance.GetType().GetMember(name, BindingFlags.Public | BindingFlags.Instance);
        if (member.Length == 0)
            return ReamNull.Instance;

        return member[0] switch
        {
            FieldInfo field => ReamObjectFactory.Create(field.GetValue(this._instance)),
            PropertyInfo property => ReamObjectFactory.Create(property.GetValue(this._instance)),
            MethodInfo method => ReamFunctionExternal.From(method),
            _ => ReamNull.Instance,
        };
    }
    
    protected override ReamObject SetClassMember(string name, ReamObject value)
    {
        // Use reflection to set the member
        MemberInfo[] member = this._instance.GetType().GetMember(name, BindingFlags.Public | BindingFlags.Instance);
        if (member.Length == 0)
            return ReamNull.Instance;

        bool found = true;
        switch (member[0])
        {
            case FieldInfo field:
                field.SetValue(this._instance, value.RepresentAs(field.FieldType));
                break;
            case PropertyInfo property:
                property.SetValue(this._instance, value.RepresentAs(property.PropertyType));
                break;
            default:
                found = false;
                break;
        }
        
        return found ? value : ReamNull.Instance;
    }

    public override ReamBoolean Equal(ReamObject other) => ReamBoolean.From(this._instance.Equals(other.Represent()));

    public override ReamBoolean NotEqual(ReamObject other) => ReamBoolean.From(!this._instance.Equals(other.Represent()));

    public override ReamBoolean Less(ReamObject other)
    {
        if (this._instance is IComparable comparable)
            return ReamBoolean.From(comparable.CompareTo(other.Represent()) < 0);
        
        return ReamBoolean.False;
    }

    public override ReamBoolean Greater(ReamObject other)
    {
        if (this._instance is IComparable comparable)
            return ReamBoolean.From(comparable.CompareTo(other.Represent()) > 0);
        
        return ReamBoolean.False;
    }

    public override ReamBoolean LessEqual(ReamObject other)
    {
        if (this._instance is IComparable comparable)
            return ReamBoolean.From(comparable.CompareTo(other.Represent()) <= 0);
        
        return ReamBoolean.False;
    }

    public override ReamBoolean GreaterEqual(ReamObject other)
    {
        if (this._instance is IComparable comparable)
            return ReamBoolean.From(comparable.CompareTo(other.Represent()) >= 0);
        
        return ReamBoolean.False;
    }

    public override ReamObject Add(ReamObject other)
    {
        throw new NotImplementedException();
    }

    public override ReamObject Subtract(ReamObject other)
    {
        throw new NotImplementedException();
    }

    public override ReamObject Multiply(ReamObject other)
    {
        throw new NotImplementedException();
    }

    public override ReamObject Divide(ReamObject other)
    {
        throw new NotImplementedException();
    }

    public override ReamObject Modulo(ReamObject other)
    {
        throw new NotImplementedException();
    }

    public override ReamObject Negate() => this._instance switch
    {
        int i => ReamNumber.From(-i),
        long l => ReamNumber.From(-l),
        float f => ReamNumber.From(-f),
        double d => ReamNumber.From(-d),
        short s => ReamNumber.From(-s),
        byte b => ReamNumber.From(-b),
        decimal d => ReamNumber.From(-d),
        _ => ReamNull.Instance,
    };

    public override ReamBoolean Not()
    {
        if (this._instance is bool b)
            return ReamBoolean.From(!b);
        
        return ReamBoolean.False; 
    }

    public override ReamString String() => ReamString.From(this._instance.ToString());
}
