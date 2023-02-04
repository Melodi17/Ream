using System.Linq.Expressions;
using System.Reflection;

namespace ream.Interpreting.Objects;

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

    public override ReamObject Call(ReamSequence args)
    {
        int argCount = args.Length.IntValue;
        object[] methodArgs = new object[this._methodInfo.GetParameters().Length];
        ParameterInfo[] parameters = this._methodInfo.GetParameters();
        for (int i = 0; i < methodArgs.Length; i++)
        {
            methodArgs[i] = parameters[i].ParameterType == typeof(ReamObject) 
                || parameters[i].ParameterType.BaseType == typeof(ReamObject)
                    ? i < argCount ? args[ReamNumber.From(i)] : ReamNull.Instance
                    : i < argCount ? args[ReamNumber.From(i)].RepresentAs(parameters[i].ParameterType) : parameters[i].DefaultValue;
        }

        // Call method
        object result = this._methodInfo.Invoke(this._boundInstance, methodArgs);
        return ReamObjectFactory.Create(result);
    }

    public override ReamFunction Bind(object instance)
        => new ReamFunctionExternal(this._methodInfo, instance);
    
    protected override void DisposeManaged() { /* Do nothing */ }
    protected override void DisposeUnmanaged()
    {
        this._methodInfo = null;
        this._boundInstance = null;   
    }
    public override object Represent()
    {
        // Generate a delegate from the method info
        return Delegate.CreateDelegate(
            Expression.GetDelegateType(
                this._methodInfo.GetParameters()
                    .Select(p => p.ParameterType)
                    .Append(this._methodInfo.ReturnType)
                    .ToArray()
            ), this._boundInstance, this._methodInfo);
    }
}
