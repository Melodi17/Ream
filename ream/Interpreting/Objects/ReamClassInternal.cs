using System.Reflection;
using Ream.Interpreting;

namespace ream.Interpreting.Objects;

public class ReamClassInternal : ReamClass
{
    private Scope _staticScope;
    private Scope _instanceScope;
    private string _typeName;

    private ReamClassInternal(string typeName, Scope staticScope, Scope instanceScope, ReamSequence args)
    {
        this._staticScope = staticScope;
        this._instanceScope = instanceScope;
        this._typeName = typeName;
        
        this._staticScope.Set("that", this, VariableType.Local);
        this._instanceScope.Set("that", this, VariableType.Local);

        ReamReference reference = this._staticScope.Get(MemberType.Instance);
        reference?.Value?.Call(args);
    }
    
    public static ReamClassInternal From(string typeName, Scope staticScope, Scope instanceScope, ReamSequence args) => new(typeName, staticScope, instanceScope, args);
    

    protected override void DisposeManaged()
    {
        this._instanceScope.Dispose();
        this._staticScope.Dispose();
    }
    protected override void DisposeUnmanaged() { /* Do nothing */ }
    public override object Represent() => (ReamObject)this;
    public override ReamString Type()
    {
        return ReamString.From(this._typeName);
    }
    protected override ReamObject ClassMember(string name)
    {
        ReamReference reference = this._staticScope.Get(name);
        return reference?.Value ?? ReamNull.Instance;
    }
    protected override ReamObject SetClassMember(string name, ReamObject value)
    {
        ReamReference reference = this._staticScope.Get(name);
        if (reference == null)
            return ReamNull.Instance;
        
        reference.Value = value;
        return value;
    }

    public override ReamObject New(ReamSequence args) => ReamClassInstanceInternal.From(this._typeName, this._instanceScope, args);
}
