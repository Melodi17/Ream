using System.Reflection;
using Ream.Interpreting;
using Ream.Lexing;
using Ream.Parsing;

namespace Ream.Interpreting
{
    public interface IClass : ICallable { }
    public class Class : IClass, IPropable, ICallable
    {
        public readonly string Name;
        public readonly Scope Scope;
        public readonly Scope StaticScope;
        private readonly Func<KeyValuePair<string, object>, bool> GetNonStaticInitializer;
        private readonly Func<KeyValuePair<string, object>, bool> GetStaticInitializer;
        public readonly Interpreter interpreter;
        public Class(string name, Interpreter interpreter, Scope scope, Scope staticScope)
        {
            this.Name = name;
            this.Scope = scope;
            this.StaticScope = staticScope;
            this.interpreter = interpreter;

            this.StaticScope.Define("this", this, VariableType.Normal);

            GetNonStaticInitializer = x =>
                x.Key == Resolver.OVERRIDE_INSTANCE;

            GetStaticInitializer = x =>
            {
                VariableType type = staticScope.GetData(x.Key).Type;

                return x.Key == Resolver.OVERRIDE_INSTANCE
                    && type.HasFlag(VariableType.Static);
            };

            var all = staticScope.All();
            if (all.Any(GetStaticInitializer))
            {
                Function initializer = all.First(GetStaticInitializer).Value as Function;
                initializer.Bind(this).Call(this.interpreter, new());
            }
        }

        public int ArgumentCount()
        {
            var all = Scope.All();

            if (all.Any(GetNonStaticInitializer))
            {
                Function initializer = all.First(GetNonStaticInitializer).Value as Function;
                return initializer.ArgumentCount();
            }
            return 0;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            ClassInstance inst = new(this, Scope);
            var all = Scope.All();

            if (all.Any(GetNonStaticInitializer))
            {
                Function initializer = all.First(GetNonStaticInitializer).Value as Function;
                initializer.Bind(inst).Call(interpreter, arguments);
            }

            return inst;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
        {
            VariableType type = StaticScope.AutoDetectType(key, manualType);
            if (type.HasFlag(VariableType.Dynamic))
                return type & ~VariableType.Dynamic;
            
            return type;
        }

        public object Get(Token key)
        {
            if (StaticScope.AutoDetectType(key, VariableType.Normal).HasFlag(VariableType.Dynamic))
                return interpreter.Evaluate(StaticScope.Get(key) as Expr, this.StaticScope);
            
            return StaticScope.Get(key);
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
            => StaticScope.Set(key, value, type);

        public override string ToString()
            => Name;
    }
    public class ClassInstance : IPropable
    {
        public Class Class;
        private Scope scope;

        public ClassInstance(Class clss, Scope scope)
        {
            Class = clss;
            this.scope = new(scope);

            this.scope.Define("this", this);
        }

        public object Get(Token key)
        {
            object resp = scope.Get(key);
            if (scope.AutoDetectType(key, VariableType.Normal).HasFlag(VariableType.Dynamic))
                resp = Class.interpreter.Evaluate(resp as Expr, this.scope);
            
            if (resp is Function c)
                return c.Bind(this);

            return resp;
        }

        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
        {
            VariableType type = scope.AutoDetectType(key, manualType);
            if (type.HasFlag(VariableType.Dynamic))
                return type & ~VariableType.Dynamic;

            return scope.AutoDetectType(key, manualType);
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        {
            scope.Set(key, value, type);
        }

        public override string ToString()
        {
            return $"{Class.Name} instance";
        }
    }
    public class ExternalClass : IClass, IPropable, ICallable
    {
        public Type Type;
        private Scope scope;
        private Scope staticScope;
        private readonly Func<KeyValuePair<string, object>, bool> GetNonStaticInitializer;
        private readonly Func<KeyValuePair<string, object>, bool> GetStaticInitializer;
        public ExternalClass(Type type, Interpreter interpreter)
        {
            scope = new();
            staticScope = new();

            GetNonStaticInitializer = x =>
                x.Key == Resolver.OVERRIDE_INSTANCE;

            GetStaticInitializer = x =>
            {
                VariableType type = staticScope.GetData(x.Key).Type;

                return x.Key == Resolver.OVERRIDE_INSTANCE
                    && type.HasFlag(VariableType.Static);
            };

            Type = type;
            ExternalFunction[] functions = Type.GetMethods()
                .Select(x => new ExternalFunction(x)).ToArray();

            foreach (ExternalFunction function in functions)
            {
                (function.Type.HasFlag(VariableType.Static) ? staticScope : scope).Set(function.Name, function, function.Type);
            }

            var all = staticScope.All();

            if (all.Any(GetStaticInitializer))
            {
                ExternalFunction initializer = all.First(GetStaticInitializer).Value as ExternalFunction;
                initializer.Call(interpreter, new());
            }
        }
        public int ArgumentCount()
        {
            var all = scope.All();

            if (all.Any(GetNonStaticInitializer))
            {
                ExternalFunction initializer = all.First(GetNonStaticInitializer).Value as ExternalFunction;
                return initializer.ArgumentCount();
            }
            return -1;
        }

        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => staticScope.AutoDetectType(key, manualType);

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            ExternalClassInstance inst = new(this, scope, arguments);
            var all = scope.All();

            if (all.Any(GetNonStaticInitializer))
            {
                ExternalFunction initializer = all.First(GetNonStaticInitializer).Value as ExternalFunction;
                initializer.Bind(inst.Instance).Call(interpreter, arguments);
            }

            return inst;
        }

        public object Get(Token key)
        {
            if (staticScope.Has(key.Raw))
            {
                return staticScope.Get(key);
            }
            else
            {
                PropertyInfo prop = Type.GetProperties().FirstOrDefault(x =>
                {
                    //var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                    //if (attribute == null)
                    //    return x.Name == key.Raw && x.IsStatic();

                    //return attribute.Name == key.Raw && attribute.Type.HasFlag(VariableType.Static);
                    return x.Name == key.Raw && x.IsStatic();
                });
                if (prop != null)
                    if (prop.CanRead)
                        return prop.GetValue(null);

                FieldInfo field = Type.GetFields().FirstOrDefault(x =>
                {
                    //var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                    //if (attribute == null)
                    //    return x.Name == key.Raw && x.IsStatic;

                    //return attribute.Name == key.Raw && attribute.Type.HasFlag(VariableType.Static);
                    return x.Name == key.Raw && x.IsStatic;
                });
                if (field != null)
                    return field.GetValue(null);

                

                return null;
            }
        }

        public void Set(Token key, object value, VariableType vt = VariableType.Normal)
        {
            PropertyInfo prop = Type.GetProperties().FirstOrDefault(x =>
            {
                //var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                //if (attribute == null)
                //    return x.Name == key.Raw && x.IsStatic();
                //return attribute.Name == key.Raw && attribute.Type.HasFlag(VariableType.Static);
                return x.Name == key.Raw && x.IsStatic();
            });
            if (prop != null)
            {
                // If it wants a string, stringify it first
                value = Program.Interpreter.resolver.ToNative(prop.PropertyType, value);

                if (prop.CanWrite)
                    prop.SetValue(null, value);
                return;
            }

            FieldInfo field = Type.GetFields().FirstOrDefault(x =>
            {
                //var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                //if (attribute == null)
                //    return x.Name == key.Raw && x.IsStatic;
                //return attribute.Name == key.Raw && attribute.Type.HasFlag(VariableType.Static);
                return x.Name == key.Raw && x.IsStatic;
            });
            if (field != null)
            {
                // If it wants a string, stringify it first
                value = Program.Interpreter.resolver.ToNative(field.FieldType, value);

                if (!field.Attributes.HasFlag(FieldAttributes.InitOnly))
                    field.SetValue(null, value);
                return;
            }

            staticScope.Set(key, value, vt);
        }
    }
    public class ExternalClassInstance : IPropable
    {
        public ExternalClass Class;
        public object Instance;
        private Scope Scope;
        public ExternalClassInstance(ExternalClass clss, Scope scope, List<object> aruments)
        {
            Class = clss;
            Scope = scope;
            Instance = Activator.CreateInstance(Class.Type, aruments.ToArray());

            // Bind all the functions to the instance and re-assign
            foreach (KeyValuePair<string, object> pair in Scope.All())
            {
                if (pair.Value is ExternalFunction func)
                {
                    func.Bind(Instance);
                    Scope.Set(pair.Key, func);
                }
            }
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => Scope.AutoDetectType(key, manualType);

        public object Get(Token key)
        {
            if (Scope.Has(key.Raw))
            {
                object resp = Scope.Get(key);

                return resp;
            }
            else
            {
                PropertyInfo prop = Class.Type.GetProperties().FirstOrDefault(x =>
                {
                    //var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                    //if (attribute == null)
                    //    return x.Name == key.Raw && !x.IsStatic();
                    //return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
                    return x.Name == key.Raw && !x.IsStatic();
                });
                if (prop != null)
                    if (prop.CanRead)
                        return prop.GetValue(Instance);

                FieldInfo field = Class.Type.GetFields().FirstOrDefault(x =>
                {
                    //var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                    //if (attribute == null)
                    //    return x.Name == key.Raw && !x.IsStatic;
                    //return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
                    return x.Name == key.Raw && !x.IsStatic;
                });
                if (field != null)
                    return field.GetValue(Instance);

                return null;
            }
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        {
            PropertyInfo prop = Class.Type.GetProperties().FirstOrDefault(x =>
            {
                //var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                //if (attribute == null)
                //    return x.Name == key.Raw && !x.IsStatic();
                //return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
                return x.Name == key.Raw && !x.IsStatic();
            });
            if (prop != null)
            {
                // If it wants a string, stringify it first
                value = Program.Interpreter.resolver.ToNative(prop.PropertyType, value);

                if (prop.CanWrite)
                    prop.SetValue(Instance, value);
                return;
            }

            FieldInfo field = Class.Type.GetFields().FirstOrDefault(x =>
            {
                //var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                //if (attribute == null)
                //    return x.Name == key.Raw && !x.IsStatic;
                //return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
                return x.Name == key.Raw && !x.IsStatic;
            });
            if (field != null)
            {
                // If it wants a string, stringify it first
                value = Program.Interpreter.resolver.ToNative(field.FieldType, value);

                if (!field.Attributes.HasFlag(FieldAttributes.InitOnly))
                    field.SetValue(Instance, value);
                return;
            }

            Scope.Set(key, value, type);
        }
    }
    public static class Extensions
    {
        public static bool IsStatic(this PropertyInfo source, bool nonPublic = false)
            => source.GetAccessors(nonPublic).Any(x => x.IsStatic);
    }
}
