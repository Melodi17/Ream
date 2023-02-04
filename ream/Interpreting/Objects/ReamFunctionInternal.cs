using System.Linq.Expressions;
using Ream.Interpreting;
using Ream.Parsing;

namespace ream.Interpreting.Objects;

public class ReamFunctionInternal : ReamFunction
{
    private List<string> Parameters { get; }
    private List<Stmt> Body { get; }
    private Scope Scope { get; }
    private ReamObject This { get; }

    public static ReamFunctionInternal From(List<string> parameters, List<Stmt> body, Scope scope, ReamObject @this)
        => new(parameters, body, scope, @this);

    public static ReamFunctionInternal From(List<string> parameters, List<Stmt> body, Scope scope)
        => new(parameters, body, scope, null);

    private ReamFunctionInternal(List<string> parameters, List<Stmt> body, Scope scope, ReamObject @this)
    {
        this.Parameters = parameters;
        this.Body = body;
        this.Scope = scope;
        this.This = @this;
    }

    public override ReamObject Call(ReamSequence args)
    {
        // Make amount of arguments match, args might have less or more than the method requires
        object[] methodArgs = new object[this.Parameters.Count];
        for (int i = 0; i < methodArgs.Length; i++)
        {
            methodArgs[i] = i < args.Length.IntValue
                ? args[ReamNumber.From(i)].Represent()
                : null;
        }

        // Call method
        Scope scope = new(this.Scope);
        scope.Set("this", this.This, VariableType.Local);
        for (int i = 0; i < this.Parameters.Count; i++)
        {
            scope.Set(this.Parameters[i], ReamObjectFactory.Create(methodArgs[i]), VariableType.Local);
        }
        try
        {
            Interpreter.Instance.ExecuteBlock(this.Body, scope);
        }
        catch (Return e)
        {
            return ReamObjectFactory.Create(e.Value);
        }
        finally
        {
            Interpreter.Instance.Dispose();
        }

        return ReamNull.Instance;
    }

    public override ReamFunction Bind(object instance)
    {
        // Object MUST be ReamObject
        if (instance is not ReamObject reamObject)
            throw new ArgumentException("Object must be of type ReamObject");

        return new ReamFunctionInternal(this.Parameters, this.Body, this.Scope, reamObject);
    }

    protected override void DisposeManaged()
    {
        this.Scope.Dispose();
    }

    protected override void DisposeUnmanaged()
    {
        this.Parameters.Clear();
        this.Body.Clear();
    }

    public override object Represent()
    {
        // Use reflection to generate a lambda expression, that takes specified amount of arguments
        // and returns a ReamObject
        ParameterExpression[] parameters = new ParameterExpression[this.Parameters.Count];
        for (int i = 0; i < parameters.Length; i++)
            parameters[i] = Expression.Parameter(typeof(ReamObject));



        // Create a lambda expression, when it is called, it simply runs the Call method
        LambdaExpression lambda = Expression.Lambda(
            Expression.Call(
                Expression.Constant(this),
                typeof(ReamFunctionInternal).GetMethod("Call")!,
                parameters
            ),
            parameters
        );

        // Compile lambda expression to a delegate
        return lambda.Compile();
    }
}
