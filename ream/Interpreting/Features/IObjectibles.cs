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

            this.GetNonStaticInitializer = x =>
                x.Key == Resolver.OVERRIDE_INSTANCE;

            this.GetStaticInitializer = x =>
            {
                VariableType type = staticScope.GetData(x.Key).Type;

                return x.Key == Resolver.OVERRIDE_INSTANCE
                    && type.HasFlag(VariableType.Static);
            };

            Dictionary<string, object> all = staticScope.All();
            if (all.Any(this.GetStaticInitializer))
            {
                Function initializer = all.First(this.GetStaticInitializer).Value as Function;
                initializer.Bind(this).Call(this.interpreter, new());
            }
        }

        public int ArgumentCount()
        {
            Dictionary<string, object> all = this.Scope.All();

            if (all.Any(this.GetNonStaticInitializer))
            {
                Function initializer = all.First(this.GetNonStaticInitializer).Value as Function;
                return initializer.ArgumentCount();
            }
            return 0;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            ClassInstance inst = new(this, this.Scope);
            Dictionary<string, object> all = this.Scope.All();

            if (all.Any(this.GetNonStaticInitializer))
            {
                Function initializer = all.First(this.GetNonStaticInitializer).Value as Function;
                initializer.Bind(inst).Call(interpreter, arguments);
            }

            return inst;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
        {
            VariableType type = this.StaticScope.AutoDetectType(key, manualType);
            if (type.HasFlag(VariableType.Dynamic))
                return type & ~VariableType.Dynamic;
            
            return type;
        }

        public object Get(Token key)
        {
            if (this.StaticScope.AutoDetectType(key, VariableType.Normal).HasFlag(VariableType.Dynamic))
                return this.interpreter.Evaluate(this.StaticScope.Get(key) as Expr, this.StaticScope);
            
            return this.StaticScope.Get(key);
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
            => this.StaticScope.Set(key, value, type);

        public override string ToString()
            => this.Name;
    }
    public class ClassInstance : IPropable
    {
        public Class Class;
        private Scope scope;

        public ClassInstance(Class clss, Scope scope)
        {
            this.Class = clss;
            this.scope = new(scope);

            this.scope.Define("this", this);
        }

        public object Get(Token key)
        {
            object resp = this.scope.Get(key);
            if (this.scope.AutoDetectType(key, VariableType.Normal).HasFlag(VariableType.Dynamic))
                resp = this.Class.interpreter.Evaluate(resp as Expr, this.scope);
            
            if (resp is Function c)
                return c.Bind(this);

            return resp;
        }

        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
        {
            VariableType type = this.scope.AutoDetectType(key, manualType);
            if (type.HasFlag(VariableType.Dynamic))
                return type & ~VariableType.Dynamic;

            return this.scope.AutoDetectType(key, manualType);
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        {
            this.scope.Set(key, value, type);
        }

        public override string ToString()
        {
            return $"{this.Class.Name} instance";
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
            this.scope = new();
            this.staticScope = new();

            this.GetNonStaticInitializer = x =>
                x.Key == Resolver.OVERRIDE_INSTANCE;

            this.GetStaticInitializer = x =>
            {
                VariableType type = this.staticScope.GetData(x.Key).Type;

                return x.Key == Resolver.OVERRIDE_INSTANCE
                    && type.HasFlag(VariableType.Static);
            };

            this.Type = type;
            ExternalFunction[] functions = this.Type.GetMethods()
                .Select(x => new ExternalFunction(x)).ToArray();

            foreach (ExternalFunction function in functions)
            {
                (function.Type.HasFlag(VariableType.Static) ? this.staticScope : this.scope).Set(function.Name, function, function.Type);
            }

            Dictionary<string, object> all = this.staticScope.All();

            if (all.Any(this.GetStaticInitializer))
            {
                ExternalFunction initializer = all.First(this.GetStaticInitializer).Value as ExternalFunction;
                initializer.Call(interpreter, new());
            }
        }
        public int ArgumentCount()
        {
            Dictionary<string, object> all = this.scope.All();

            if (all.Any(this.GetNonStaticInitializer))
            {
                ExternalFunction initializer = all.First(this.GetNonStaticInitializer).Value as ExternalFunction;
                return initializer.ArgumentCount();
            }
            return -1;
        }

        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => this.staticScope.AutoDetectType(key, manualType);

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            ExternalClassInstance inst = new(this, this.scope, arguments);
            Dictionary<string, object> all = this.scope.All();

            if (all.Any(this.GetNonStaticInitializer))
            {
                ExternalFunction initializer = all.First(this.GetNonStaticInitializer).Value as ExternalFunction;
                initializer.Bind(inst.Instance).Call(interpreter, arguments);
            }

            return inst;
        }

        public object Get(Token key)
        {
            if (this.staticScope.Has(key.Raw))
            {
                return this.staticScope.Get(key);
            }
            else
            {
                PropertyInfo prop = this.Type.GetProperties().FirstOrDefault(x =>
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

                FieldInfo field = this.Type.GetFields().FirstOrDefault(x =>
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
            PropertyInfo prop = this.Type.GetProperties().FirstOrDefault(x =>
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

            FieldInfo field = this.Type.GetFields().FirstOrDefault(x =>
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

            this.staticScope.Set(key, value, vt);
        }
    }
    public class ExternalClassInstance : IPropable
    {
        public ExternalClass Class;
        public object Instance;
        private Scope Scope;
        public ExternalClassInstance(ExternalClass clss, Scope scope, List<object> aruments)
        {
            this.Class = clss;
            this.Scope = scope;
            this.Instance = Activator.CreateInstance(this.Class.Type, aruments.ToArray());

            // Bind all the functions to the instance and re-assign
            foreach (KeyValuePair<string, object> pair in this.Scope.All())
            {
                if (pair.Value is ExternalFunction func)
                {
                    func.Bind(this.Instance);
                    this.Scope.Set(pair.Key, func);
                }
            }
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => this.Scope.AutoDetectType(key, manualType);

        public object Get(Token key)
        {
            if (this.Scope.Has(key.Raw))
            {
                object resp = this.Scope.Get(key);

                return resp;
            }
            else
            {
                PropertyInfo prop = this.Class.Type.GetProperties().FirstOrDefault(x =>
                {
                    //var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                    //if (attribute == null)
                    //    return x.Name == key.Raw && !x.IsStatic();
                    //return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
                    return x.Name == key.Raw && !x.IsStatic();
                });
                if (prop != null)
                    if (prop.CanRead)
                        return prop.GetValue(this.Instance);

                FieldInfo field = this.Class.Type.GetFields().FirstOrDefault(x =>
                {
                    //var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                    //if (attribute == null)
                    //    return x.Name == key.Raw && !x.IsStatic;
                    //return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
                    return x.Name == key.Raw && !x.IsStatic;
                });
                if (field != null)
                    return field.GetValue(this.Instance);

                return null;
            }
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        {
            PropertyInfo prop = this.Class.Type.GetProperties().FirstOrDefault(x =>
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
                    prop.SetValue(this.Instance, value);
                return;
            }

            FieldInfo field = this.Class.Type.GetFields().FirstOrDefault(x =>
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
                    field.SetValue(this.Instance, value);
                return;
            }

            this.Scope.Set(key, value, type);
        }
    }
    public static class Extensions
    {
        public static bool IsStatic(this PropertyInfo source, bool nonPublic = false)
            => source.GetAccessors(nonPublic).Any(x => x.IsStatic);
    }
}
