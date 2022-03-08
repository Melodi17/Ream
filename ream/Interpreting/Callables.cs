using System.Reflection;
using Ream.Parsing;

namespace Ream.Interpreting
{
    public class ExternalFunctionAttribute : Attribute
    {
        public int ArgumentCount;
        public string Name;
        public VariableType Type;
        public ExternalFunctionAttribute(string name = "", int argumentCount = -1, VariableType type = VariableType.Normal)
        {
            ArgumentCount = argumentCount;
            Type = type;
            Name = name;
        }
    }
    public class ExternalFunction : ICallable
    {
        public Func<object, List<object>, object> _func;
        public string Name;
        public VariableType Type;
        private int _argumentCount;
        public object ClassRef;
        private ExternalFunction() { }
        public ExternalFunction(Func<object, List<object>, object> func, int argumentCount)
        {
            _func = func;
            _argumentCount = argumentCount;
        }

        public ExternalFunction(MethodInfo info)
        {
            var attribute = info.GetCustomAttribute<ExternalFunctionAttribute>();
            if (attribute == null)
                throw new Exception("Unable to create external method without an ExternalFunctionAttribute");

            _argumentCount = attribute.ArgumentCount == -1 ? info.GetParameters().Length : attribute.ArgumentCount;
            _func = new Func<object, List<object>, object>((ctx, args) =>
                 info.Invoke(ctx, args.ToArray()));
            Type = attribute.Type;
            if (info.IsStatic && !Type.HasFlag(VariableType.Static))
                Type |= VariableType.Static;

            Name = attribute.Name == "" ? info.Name : attribute.Name;
        }
        public int ArgumentCount()
            => _argumentCount;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            return _func.Invoke(ClassRef, arguments);
        }

        public ExternalFunction Bind(object clss)
        {
            return new()
            {
                ClassRef = clss,
                Name = this.Name,
                Type = this.Type,
                _argumentCount = _argumentCount,
                _func = this._func
            };
        }
    }
    public class Function : ICallable
    {
        private readonly Stmt.Function Declaration;
        private readonly Scope ParentScope;

        public Function(Stmt.Function declaration, Scope scope)
        {
            Declaration = declaration;
            ParentScope = scope;
        }

        public Function Bind(IClassInstance instance)
        {
            // Clone it
            Scope scope = new(ParentScope);
            scope.Set("this", instance, VariableType.Local);
            return new Function(Declaration, scope);
        }

        public int ArgumentCount()
        {
            return Declaration.parameters.Count;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            Scope scope = new(ParentScope);
            for (int i = 0; i < Declaration.parameters.Count; i++)
            {
                scope.Set(Declaration.parameters[i], arguments[i], VariableType.Local);
            }

            try
            {
                interpreter.ExecuteBlock(Declaration.body, scope);
            }
            catch (Return returnVal)
            {
                return returnVal.Value;
            }
            return null;
        }
    }
    public class Lambda : ICallable
    {
        private readonly Expr.Lambda Declaration;
        private readonly Scope ParentScope;

        public Lambda(Expr.Lambda declaration, Scope scope)
        {
            Declaration = declaration;
            ParentScope = scope;
        }

        public int ArgumentCount()
        {
            return Declaration.parameters.Count;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            Scope scope = new(ParentScope);
            for (int i = 0; i < Declaration.parameters.Count; i++)
            {
                scope.Set(Declaration.parameters[i], arguments[i], VariableType.Local);
            }

            try
            {
                interpreter.ExecuteBlock(Declaration.body, scope);
            }
            catch (Return returnVal)
            {
                return returnVal.Value;
            }
            return null;
        }
    }
}
