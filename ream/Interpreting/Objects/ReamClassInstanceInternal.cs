using Ream.Interpreting;

namespace ream.Interpreting.Objects;

public class ReamClassInstanceInternal : ReamClassInstance
{
    private string _typeName;
    private Scope _scope;
    private ReamClassInstanceInternal(string typeName, Scope scope, ReamSequence args)
    {
        this._typeName = typeName;
        Scope instanceScope = new();
        instanceScope.Set("this", this);
        this._scope = instanceScope;
        
        // copy all parent scope members to this scope
        foreach (ReamReference reference in scope.GetMembers())
        // if its a function, bind it to this scope
            if (reference.Value is ReamFunction function)
                this._scope.Set(reference.Name, function.Bind(this), reference.Type);
            else
                this._scope.Set(reference.Name, reference.Value, reference.Type);

        this._scope.Get(MemberType.InstanceInternal)?.Value?.Call(args);
    }

    public static ReamClassInstanceInternal From(string typeName, Scope scope, ReamSequence args) => new(typeName, scope, args);


    protected override void DisposeManaged()
    {
        throw new NotImplementedException();
    }

    protected override void DisposeUnmanaged()
    {
        throw new NotImplementedException();
    }

    public override object Represent() => (ReamObject)this;

    public override ReamString Type() => ReamString.From($"{this._typeName} instance");

    public override ReamSequence Iterate() => this._scope.Get(MemberType.Iterate)?.Value?.Call(ReamSequence.Empty).Iterate()
        ?? ReamSequence.Empty;

    public override ReamObject Index(ReamObject index) => this._scope.Get(MemberType.Index)?.Value?.Call(ReamSequence.FromParams(index)) 
        ?? ReamNull.Instance;

    public override ReamObject SetIndex(ReamObject index, ReamObject newValue) => this._scope.Get(MemberType.Index)?.Value?.Call(ReamSequence.FromParams(index, newValue)) 
        ?? ReamNull.Instance;

    public override ReamObject Call(ReamSequence args) => this._scope.Get(MemberType.Call)?.Value?.Call(args)
        ?? ReamNull.Instance;

    public override ReamString String() => this._scope.Get(MemberType.String)?.Value?.Call(ReamSequence.Empty).ForceString() 
        ?? ReamString.From($"<{this._typeName} instance");

    public override ReamBoolean Equal(ReamObject other) 
        => this._scope.Get(MemberType.Equal)?.Value is not ReamFunction equal ? ReamBoolean.From(this == other) : equal.Call(ReamSequence.FromParams(other)).Truthy();

    public override ReamBoolean NotEqual(ReamObject other) 
        => this._scope.Get(MemberType.Equal)?.Value is not ReamFunction equal ? ReamBoolean.From(this != other) : equal.Call(ReamSequence.FromParams(other)).Truthy();

    public override ReamBoolean Less(ReamObject other) 
        => this._scope.Get(MemberType.Less)?.Value is not ReamFunction less ? ReamBoolean.False : less.Call(ReamSequence.FromParams(other)).Truthy();

    public override ReamBoolean Greater(ReamObject other) 
        => this._scope.Get(MemberType.Greater)?.Value is not ReamFunction greater ? ReamBoolean.False : greater.Call(ReamSequence.FromParams(other)).Truthy();

    public override ReamBoolean LessEqual(ReamObject other) 
        => this._scope.Get(MemberType.LessEqual)?.Value is not ReamFunction lessEqual ? ReamBoolean.False : lessEqual.Call(ReamSequence.FromParams(other)).Truthy();

    public override ReamBoolean GreaterEqual(ReamObject other)
           => this._scope.Get(MemberType.GreaterEqual)?.Value is not ReamFunction greaterEqual ? ReamBoolean.False : greaterEqual.Call(ReamSequence.FromParams(other)).Truthy();

    public override ReamObject Add(ReamObject other)
        => this._scope.Get(MemberType.Add)?.Value?.Call(ReamSequence.FromParams(other)) ?? ReamNull.Instance;

    public override ReamObject Subtract(ReamObject other)
        => this._scope.Get(MemberType.Subtract)?.Value?.Call(ReamSequence.FromParams(other)) ?? ReamNull.Instance;

    public override ReamObject Multiply(ReamObject other)
        => this._scope.Get(MemberType.Multiply)?.Value?.Call(ReamSequence.FromParams(other)) ?? ReamNull.Instance;

    public override ReamObject Divide(ReamObject other)
        => this._scope.Get(MemberType.Divide)?.Value?.Call(ReamSequence.FromParams(other)) ?? ReamNull.Instance;

    public override ReamObject Modulo(ReamObject other)
        => this._scope.Get(MemberType.Modulo)?.Value?.Call(ReamSequence.FromParams(other)) ?? ReamNull.Instance;

    public override ReamObject Negate()
        => this._scope.Get(MemberType.Negate)?.Value?.Call(ReamSequence.Empty) ?? ReamNull.Instance;

    public override ReamBoolean Not()
        => this._scope.Get(MemberType.Not)?.Value?.Call(ReamSequence.Empty).Truthy() ?? ReamBoolean.False;

    protected override ReamObject ClassMember(string name) => this._scope.Get(name).Value;

    protected override ReamObject SetClassMember(string name, ReamObject value)
    {
        this._scope.Set(name, value, VariableType.Local);
        return value;
    }
}
