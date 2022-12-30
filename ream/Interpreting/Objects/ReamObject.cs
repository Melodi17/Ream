using System.Reflection;

namespace ream.Interpreting.Objects;

public abstract class ReamObject
{
    // Core behaviours
    public abstract object Represent();
    public T RepresentAs<T>()
    {
        // Convert object to its actual type, then to the desired type
        return (T)Convert.ChangeType(this.Represent(), typeof(T));
    }

    public abstract ReamString Type();
    public abstract ReamBoolean Truthy();

    // Behaviors
    public virtual ReamSequence Iterate() => ReamSequence.Empty;
    public virtual ReamObject Index(ReamObject index) => ReamNull.Instance;
    public virtual ReamObject SetIndex(ReamObject index, ReamObject newValue) => ReamNull.Instance;
    public virtual ReamObject Call(ReamObject[] args) => ReamNull.Instance;
    public virtual ReamObject Member(string name) => ReamNull.Instance;
    public virtual ReamString String() => ReamString.From("object");

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
    public static ReamObject Create<T>(T csObject)
    {

        // Convert switch to if-else
        
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
        
        if (csObject.GetType().IsGenericType && csObject.GetType().GetInterfaces().Any(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            return (ReamObject)typeof(ReamSequence).GetMethod(nameof(ReamSequence.From))?.MakeGenericMethod(csObject.GetType().GetGenericArguments()[0]).Invoke(null, new object[] { csObject });
        
        if (csObject is MethodInfo methodInfo)
            return ReamFunctionExternal.From(methodInfo);
        
        if (csObject is ReamObject reamObject) return reamObject;
        
        throw new Exception("Unknown object type '" + csObject.GetType().Name + "'");
    }
}