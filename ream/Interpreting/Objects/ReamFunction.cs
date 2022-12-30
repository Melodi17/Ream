using System.Reflection;

namespace ream.Interpreting.Objects;

public abstract class ReamFunction : ReamObject
{

    protected ReamFunction() { }

    // Conversions

    // Core behaviours
    public override abstract object Represent();
    public override ReamString Type() => ReamString.From("function");
    public override ReamBoolean Truthy() => ReamBoolean.True;

    // Behaviours
    public override abstract ReamObject Call(ReamObject[] args);
    
    public abstract ReamFunction Bind(object obj);
}

public class ReamFunctionExternal : ReamFunction
{
    private object _boundInstance;
    private MethodInfo _methodInfo;

    private ReamFunctionExternal(MethodInfo methodInfo, object boundInstance)
    {
        this._methodInfo = methodInfo;
        this._boundInstance = boundInstance;
    }

    public static ReamFunctionExternal From(MethodInfo method)
        => new(method, null);
        
    public static ReamFunctionExternal From(Delegate d)
        => new(d.Method, d.Target);

    public static ReamFunctionExternal From(MethodInfo method, object boundInstance)
        => new(method, boundInstance);

    public override ReamObject Call(ReamObject[] args)
    {
        // Make amount of arguments match, args might have less or more than the method requires
        object[] methodArgs = new object[this._methodInfo.GetParameters().Length];
        for (int i = 0; i < methodArgs.Length; i++)
        {
            methodArgs[i] = i < args.Length
                ? args[i].Represent()
                : this._methodInfo.GetParameters()[i].DefaultValue;
        }
            
        // Call method
        object result = this._methodInfo.Invoke(this._boundInstance, methodArgs);
        return ReamObjectFactory.Create(result);
    }
        
    public override ReamFunction Bind(object instance)
        => new ReamFunctionExternal(this._methodInfo, instance);
        
    public override object Represent() => this._methodInfo;
}

public class ReamFunctionInternal : ReamFunction
{
    
}