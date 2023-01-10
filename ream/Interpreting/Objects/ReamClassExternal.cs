using System.Reflection;

namespace ream.Interpreting.Objects;

public class ReamClassExternal : ReamClass
{
    private Type _type;

    private ReamClassExternal(Type type)
    {
        this._type = type;
    }

    public static ReamClassExternal From(Type type) => new(type);
    public static ReamClassExternal From<T>() => new(typeof(T));
    protected override void DisposeManaged() { /* Do nothing */ }
    protected override void DisposeUnmanaged()
    {
        this._type = null;
    }
    public override object Represent() => this._type;
    public override ReamString Type()
    {
        return ReamString.From(this._type.Name);
    }
    public override ReamObject Member(string name)
    {
        // Use reflection to get the member
        MemberInfo[] member = this._type.GetMember(name, BindingFlags.Public | BindingFlags.Static);
        if (member.Length == 0)
            return ReamNull.Instance;

        return member[0] switch
        {
            FieldInfo field => ReamObjectFactory.Create(field.GetValue(null)),
            PropertyInfo property => ReamObjectFactory.Create(property.GetValue(null)),
            MethodInfo method => ReamFunctionExternal.From(method),
            _ => ReamNull.Instance,
        };
    }
    public override ReamObject SetMember(string name, ReamObject value)
    {
        // Use reflection to set the member
        MemberInfo[] member = this._type.GetMember(name, BindingFlags.Public | BindingFlags.Static);
        if (member.Length == 0)
            return ReamNull.Instance;

        bool found = true;
        switch (member[0])
        {
            case FieldInfo field:
                field.SetValue(null, value.RepresentAs(field.FieldType));
                break;
            case PropertyInfo property:
                property.SetValue(null, value.RepresentAs(property.PropertyType));
                break;
            default:
                found = false;
                break;
        }
        
        return found ? value : ReamNull.Instance;
    }

    public override ReamClassInstance New(ReamSequence args)
    {
        List<object> objectArgs = args.RepresentAs<List<object>>();
        
        // Create instance of the object itself
        object instance = this._type.GetConstructor(BindingFlags.Public,
                null,
                objectArgs.Select(arg => arg.GetType())
                    .ToArray(),
                null)?
            
            .Invoke(objectArgs.ToArray());
        
        // Create instance of the wrapper
        return ReamClassInstanceExternal.From(instance);
    }
}
