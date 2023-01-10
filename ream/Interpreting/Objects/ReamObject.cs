using System.Reflection;
using Ream.Interpreting;

namespace ream.Interpreting.Objects;

public abstract class ReamObject : IDisposable
{
    public static int ObjectCount { get; private set; }
    private bool _disposed = false;

    protected ReamObject()
    {
        ObjectCount++;
    }

    // Core behaviours
    ~ReamObject() { this.Dispose(false); }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!this._disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                this.DisposeManaged();
            }
            
            // Dispose unmanaged resources
            this.DisposeUnmanaged();
            
            this._disposed = true;
            ObjectCount--;
        }
    }

    protected abstract void DisposeManaged();
    protected abstract void DisposeUnmanaged();
    public abstract object Represent();

    public virtual T RepresentAs<T>() => (T)this.Represent();

    public object RepresentAs(Type t)
    {
        object repr = this.Represent();

        return Convert.ChangeType(repr, t);
    }

    public abstract ReamString Type();
    public abstract ReamBoolean Truthy();

    // Behaviors
    public virtual ReamSequence Iterate() => ReamSequence.Empty;
    public virtual ReamObject Index(ReamObject index) => ReamNull.Instance;
    public virtual ReamObject SetIndex(ReamObject index, ReamObject newValue) => ReamNull.Instance;
    public virtual ReamObject Call(ReamSequence args) => ReamNull.Instance;

    public virtual ReamObject Member(string name) => name switch
    {
        MemberType.Type => ReamFunctionExternal.From(this.Type),
        MemberType.Truthy => ReamFunctionExternal.From(this.Truthy),

        MemberType.Iterate => ReamFunctionExternal.From(this.Iterate),
        MemberType.Index => ReamFunctionExternal.From(this.Index),
        MemberType.SetIndex => ReamFunctionExternal.From(this.SetIndex),
        MemberType.Call => ReamFunctionExternal.From(this.Call),

        MemberType.Member => ReamFunctionExternal.From(this.Member),
        MemberType.SetMember => ReamFunctionExternal.From(this.SetMember),
        MemberType.String => ReamFunctionExternal.From(this.String),
        MemberType.Equal => ReamFunctionExternal.From(this.Equal),
        MemberType.NotEqual => ReamFunctionExternal.From(this.NotEqual),
        MemberType.Less => ReamFunctionExternal.From(this.Less),
        MemberType.LessEqual => ReamFunctionExternal.From(this.LessEqual),
        MemberType.Greater => ReamFunctionExternal.From(this.Greater),
        MemberType.GreaterEqual => ReamFunctionExternal.From(this.GreaterEqual),

        MemberType.Add => ReamFunctionExternal.From(this.Add),
        MemberType.Subtract => ReamFunctionExternal.From(this.Subtract),
        MemberType.Multiply => ReamFunctionExternal.From(this.Multiply),
        MemberType.Divide => ReamFunctionExternal.From(this.Divide),
        MemberType.Modulo => ReamFunctionExternal.From(this.Modulo),

        MemberType.Negate => ReamFunctionExternal.From(this.Negate),
        MemberType.Not => ReamFunctionExternal.From(this.Not),
        _ => ReamNull.Instance,
    };
    public virtual ReamObject SetMember(string name, ReamObject value) => ReamNull.Instance;

    public virtual ReamString String() => ReamString.From("<object>");

    // Comparison operators
    public virtual ReamBoolean Equal(ReamObject other) => ReamBoolean.From(this.Represent().Equals(other.Represent()));
    public virtual ReamBoolean NotEqual(ReamObject other) => this.Equal(other).Not();
    public virtual ReamBoolean Less(ReamObject other) => ReamBoolean.False;
    public virtual ReamBoolean Greater(ReamObject other) => ReamBoolean.False;
    public virtual ReamBoolean LessEqual(ReamObject other) => ReamBoolean.From(this.Less(other).RepresentAs<bool>() || this.Equal(other).RepresentAs<bool>());
    public virtual ReamBoolean GreaterEqual(ReamObject other) => ReamBoolean.From(this.Greater(other).RepresentAs<bool>() || this.Equal(other).RepresentAs<bool>());


    // Arithmetic operators
    public virtual ReamObject Add(ReamObject other) => ReamNull.Instance;
    public virtual ReamObject Subtract(ReamObject other) => ReamNull.Instance;
    public virtual ReamObject Multiply(ReamObject other) => ReamNull.Instance;
    public virtual ReamObject Divide(ReamObject other) => ReamNull.Instance;
    public virtual ReamObject Modulo(ReamObject other) => ReamNull.Instance;

    // Unary operators
    public virtual ReamObject Negate() => ReamNull.Instance;
    public virtual ReamBoolean Not() => ReamBoolean.From(!this.Truthy().RepresentAs<bool>());
}
public static class ReamObjectFactory
{
    /// <summary>
    /// Converts a C# object to a Ream object.
    /// </summary>
    /// <param name="csObject">C# object</param>
    /// <returns>A Ream object</returns>
    public static ReamObject Create(object csObject)
    {
        if (csObject is ReamObject reamObject) return reamObject;

        if (csObject is true) return ReamBoolean.True;
        if (csObject is false) return ReamBoolean.False;
        if (csObject is null) return ReamNull.Instance;

        if (csObject is double doubleV) return ReamNumber.From(doubleV);
        if (csObject is float floatV) return ReamNumber.From(floatV);
        if (csObject is decimal decimalV) return ReamNumber.From(decimalV);
        if (csObject is int intV) return ReamNumber.From(intV);
        if (csObject is long longV) return ReamNumber.From(longV);
        if (csObject is short shortV) return ReamNumber.From(shortV);
        if (csObject is byte byteV) return ReamNumber.From(byteV);

        if (csObject is string stringV) return ReamString.From(stringV);
        if (csObject is char charV) return ReamString.From(charV);
        
        if (csObject is MethodInfo methodInfo)
            return ReamFunctionExternal.From(methodInfo);

        if (csObject.GetType().IsGenericType && csObject.GetType().GetInterfaces().Any(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            return (ReamObject)typeof(ReamSequence).GetMethod(nameof(ReamSequence.From))?.MakeGenericMethod(csObject.GetType().GetGenericArguments()[0]).Invoke(null, new object[] { csObject });
        
        if (csObject.GetType().IsGenericType && csObject.GetType().GetInterfaces().Any(x => x.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            return (ReamObject)typeof(ReamDictionary).GetMethod(nameof(ReamDictionary.From))?.MakeGenericMethod(csObject.GetType().GetGenericArguments()[0], csObject.GetType().GetGenericArguments()[1]).Invoke(null, new object[] { csObject });
        
        throw new("Unknown object type '" + csObject.GetType().Name + "'");
    }
}
public static class GarbageCollector
{
    public static void Collect()
    {
        GC.Collect();
    }
}
