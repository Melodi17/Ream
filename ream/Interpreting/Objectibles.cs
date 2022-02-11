using System.Reflection;
using Ream.Lexing;

namespace Ream.Interpreting
{
    public interface IClass : ICallable { }
    public interface IClassInstance
    {
        public object Get(Token key);
        public void Set(Token key, object value, VariableType type = VariableType.Normal);
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal);
    }
    public class Class : IClass, IClassInstance, ICallable
    {
        public readonly string Name;
        public readonly Scope Scope;
        public readonly Scope StaticScope;
        private readonly Func<KeyValuePair<string, object>, bool> GetNonStaticInitializer;
        private readonly Func<KeyValuePair<string, object>, bool> GetStaticInitializer;
        public Class(string name, Interpreter interpreter, Scope scope, Scope staticScope)
        {
            this.Name = name;
            this.Scope = scope;
            this.StaticScope = staticScope;
            GetNonStaticInitializer = x =>
                Scope.GetData(x.Key).Type.HasFlag(VariableType.Initializer);

            GetStaticInitializer = x =>
            {
                VariableType type = StaticScope.GetData(x.Key).Type;

                return type.HasFlag(VariableType.Initializer)
                    && type.HasFlag(VariableType.Static);
            };

            var all = staticScope.All();
            if (all.Any(GetStaticInitializer))
            {
                Function initializer = all.First(GetStaticInitializer).Value as Function;
                initializer.Bind(this).Call(interpreter, new());
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
            => StaticScope.AutoDetectType(key, manualType);
        public object Get(Token key)
            => StaticScope.Get(key);

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
            => StaticScope.Set(key, value, type);

        public override string ToString()
            => Name;
    }
    public class ClassInstance : IClassInstance
    {
        public Class Class;
        private Scope Scope;

        public ClassInstance(Class clss, Scope scope)
        {
            Class = clss;
            Scope = scope;
        }

        public object Get(Token key)
        {
            object resp = Scope.Get(key);
            if (resp is Function c)
                return c.Bind(this);

            return resp;
        }

        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => Scope.AutoDetectType(key, manualType);

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        { Scope.Set(key, value, type); }

        public override string ToString()
        {
            return $"{Class.Name} instance";
        }
    }
    public class ExternalVariableAttribute : Attribute
    {
        public string Name;
        public VariableType Type;
        public ExternalVariableAttribute(string name = "", VariableType type = VariableType.Normal)
        {
            Name = name;
            Type = type;
        }
        public ExternalVariableAttribute Apply(PropertyInfo info)
        {
            string name = Name;
            VariableType type = Type;
            if (Name == "")
                name = info.Name;

            if (info.IsStatic() && !Type.HasFlag(VariableType.Static))
                type |= VariableType.Static;

            return new(name, type);
        }
        public ExternalVariableAttribute Apply(FieldInfo info)
        {
            string name = Name;
            VariableType type = Type;
            if (Name == "")
                name = info.Name;

            if (info.IsStatic && !Type.HasFlag(VariableType.Static))
                type |= VariableType.Static;

            return new(name, type);
        }
    }
    public class ExternalClass<T> : IClass, IClassInstance, ICallable
    {
        public Type Type;
        private Scope scope;
        private Scope staticScope;
        private readonly Func<KeyValuePair<string, object>, bool> GetNonStaticInitializer;
        private readonly Func<KeyValuePair<string, object>, bool> GetStaticInitializer;
        public ExternalClass(Interpreter interpreter)
        {
            scope = new();
            staticScope = new();

            GetNonStaticInitializer = x =>
                scope.GetData(x.Key).Type.HasFlag(VariableType.Initializer);

            GetStaticInitializer = x =>
            {
                VariableType type = staticScope.GetData(x.Key).Type;

                return type.HasFlag(VariableType.Initializer)
                    && type.HasFlag(VariableType.Static);
            };

            Type = typeof(T);
            ExternalFunction[] functions = Type.GetMethods()
                .Where(x => x.GetCustomAttribute<ExternalFunctionAttribute>() != null)
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
            return 0;
        }

        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => staticScope.AutoDetectType(key, manualType);

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            ExternalClassInstance<T> inst = new(this, scope);
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
                    var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                    if (attribute == null)
                        return false;
                    return attribute.Name == key.Raw && attribute.Type.HasFlag(VariableType.Static);
                });
                if (prop != null)
                    return prop.GetValue(null);

                FieldInfo field = Type.GetFields().FirstOrDefault(x =>
                {
                    var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                    if (attribute == null)
                        return false;
                    return attribute.Name == key.Raw && attribute.Type.HasFlag(VariableType.Static);
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
                var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                if (attribute == null)
                    return false;
                return attribute.Name == key.Raw && attribute.Type.HasFlag(VariableType.Static);
            });
            if (prop != null)
            {
                prop.SetValue(null, value);
                return;
            }

            FieldInfo field = Type.GetFields().FirstOrDefault(x =>
            {
                var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                if (attribute == null)
                    return false;
                return attribute.Name == key.Raw && attribute.Type.HasFlag(VariableType.Static);
            });
            if (field != null)
            {
                field.SetValue(null, value);
                return;
            }

            staticScope.Set(key, value, vt);
        }
    }

    public class ExternalClassInstance<T> : IClassInstance
    {
        public ExternalClass<T> Class;
        public object Instance;
        private Scope Scope;
        public ExternalClassInstance(ExternalClass<T> clss, Scope scope)
        {
            Class = clss;
            Scope = scope;
            Instance = Activator.CreateInstance(Class.Type);
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => Scope.AutoDetectType(key, manualType);

        public object Get(Token key)
        {
            if (Scope.Has(key.Raw))
            {
                object resp = Scope.Get(key);
                if (resp is ExternalFunction c)
                {
                    ExternalFunction fun = c.Bind(Instance);
                    return fun;
                }

                return resp;
            }
            else
            {
                PropertyInfo prop = Class.Type.GetProperties().FirstOrDefault(x =>
                {
                    var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                    if (attribute == null)
                        return false;
                    return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
                });
                if (prop != null)
                    return prop.GetValue(Instance);

                FieldInfo field = Class.Type.GetFields().FirstOrDefault(x =>
                {
                    var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                    if (attribute == null)
                        return false;
                    return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
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
                var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                if (attribute == null)
                    return false;
                return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
            });
            if (prop != null)
            {
                prop.SetValue(Instance, value);
                return;
            }

            FieldInfo field = Class.Type.GetFields().FirstOrDefault(x =>
            {
                var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                if (attribute == null)
                    return false;
                return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
            });
            if (field != null)
            {
                field.SetValue(Instance, value);
                return;
            }

            Scope.Set(key, value, type);
        }
    }

    public static class PropertyInfoExtensions
    {
        public static bool IsStatic(this PropertyInfo source, bool nonPublic = false)
            => source.GetAccessors(nonPublic).Any(x => x.IsStatic);
    }
}
