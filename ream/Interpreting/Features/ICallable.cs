using System.Reflection;
using Ream.Lexing;
using Ream.Parsing;

namespace Ream.Interpreting
{
    public interface ICallable
    {
        public int ArgumentCount();
        public object Call(Interpreter interpreter, List<object> arguments);
    }

    public class ExternalFunction : ICallable
    {
        public Func<object, List<object>, object> _func;
        public string Name;
        public VariableType Type;
        private int _argumentCount;
        public object ClassRef;
        private MethodInfo mi;
        private ExternalFunction() { }
        public ExternalFunction(Func<object, List<object>, object> func, int argumentCount)
        {
            this._func = func;
            this._argumentCount = argumentCount;
        }

        public ExternalFunction(MethodInfo info)
        {
            this.mi = info;
            this._argumentCount = info.GetParameters().Length;
            this._func = new((ctx, args) =>
                 info.Invoke(ctx, args.ToArray()));
            this.Type = VariableType.Normal;
            if (info.IsStatic && !this.Type.HasFlag(VariableType.Static))
                this.Type |= VariableType.Static;

            this.Name = info.Name;
        }
        public int ArgumentCount()
            => this._argumentCount;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            // Attempt to cast all the types for it
            if (this.mi != null)
            {
                ParameterInfo[] parameters = this.mi.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo item = parameters[i];
                    arguments[i] = interpreter.resolver.ToNative(item.ParameterType, arguments[i]);
                }
            }

            try
            {
                return this._func.Invoke(this.ClassRef, arguments);
            }
            catch (Exception)
            {
                if (interpreter.raiseErrors)
                    throw new RuntimeError("Can't call external function");

                return null;
            }
        }

        public ExternalFunction Bind(object clss)
        {
            return new()
            {
                ClassRef = clss,
                Name = this.Name,
                Type = this.Type,
                _argumentCount = this._argumentCount,
                _func = this._func
            };
        }
    }
    public class Function : ICallable
    {
        List<Token> parameters;
        List<Stmt> body;
        //private readonly Stmt.Function Declaration;
        private readonly Scope ParentScope;

        public Function(Stmt.Function declaration, Scope scope)
        {
            //Declaration = declaration;
            this.parameters = declaration.parameters;
            this.body = declaration.body;
            this.ParentScope = scope;
        }

        public Function(Stmt.Method declaration, Scope scope)
        {
            this.parameters = declaration.parameters;
            this.body = declaration.body;
            this.ParentScope = scope;
        }

        public Function(Expr.Lambda declaration, Scope scope)
        {
            this.parameters = declaration.parameters;
            this.body = declaration.body;
            this.ParentScope = scope;
        }

        public Function(List<Token> parameters, List<Stmt> body, Scope scope)
        {
            this.parameters = parameters;
            this.body = body;
            this.ParentScope = scope;
        }

        public Function Bind(IPropable instance)
        {
            // Clone it
            Scope scope = new(this.ParentScope);
            scope.Set("this", instance, VariableType.Local);
            return new(this.parameters, this.body, scope);
        }

        public int ArgumentCount()
        {
            return this.parameters.Count;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            Scope scope = new(this.ParentScope);
            for (int i = 0; i < this.parameters.Count; i++)
            {
                scope.Set(this.parameters[i], arguments.ElementAtOrDefault(i) ?? null, VariableType.Local);
            }

            try
            {
                interpreter.ExecuteBlock(this.body, scope);
            }
            catch (Return returnVal)
            {
                return returnVal.Value;
            }
            return null;
        }
    }
}
