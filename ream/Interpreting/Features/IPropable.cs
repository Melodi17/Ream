using System.Reflection;
using Ream.Lexing;
using Ream.Tools;

namespace Ream.Interpreting
{
    public interface IPropable
    {
        public object Get(Token key);
        public void Set(Token key, object value, VariableType type = VariableType.Normal);
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal);
    }
    public class ObjectType : IPropable
    {
        public static Dictionary<string, Dictionary<string, object>> Extensions = new();
        public string ID;

        public static object Get(Token key, string id, IPropable prop)
        {
            if (Extensions.ContainsKey(id))
                if (Extensions[id].ContainsKey(key.Raw))
                {
                    object item = Extensions[id][key.Raw];
                    if (item is Function func)
                        return func.Bind(prop);

                    return item;
                }

            return null;
        }
        public static void Set(Token key, string id, object value)
        {
            if (Extensions.ContainsKey(id))
                Extensions[id][key.Raw] = value;
            else
                Extensions.Add(id, new() { { key.Raw, value } });
        }
        public ObjectType(string id)
        {
            this.ID = id;
            if (!Extensions.ContainsKey(id))
                Extensions.Add(id, new());
        }

        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal) => manualType;
        public object Get(Token key)
        {
            return Extensions[this.ID].ContainsKey(key.Raw) ? Extensions[this.ID][key.Raw] : null;
        }
        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        {
            Extensions[this.ID][key.Raw] = value;
        }
    }
    public class StringPropMap : IPropable
    {
        public const string TypeID = "string";
        public readonly string Value;
        public StringPropMap(string value)
        {
            this.Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                "Length" => (double)this.Value.Length,
                "Contains" => new ExternalFunction((i, j) => this.Value.Contains(j[0].ToString()), 1),
                "Replace" => new ExternalFunction((i, j) => this.Value.Replace(j[0].ToString(), j[1].ToString()), 2),
                "Starts" => new ExternalFunction((i, j) => this.Value.StartsWith(j[0].ToString()), 1),
                "Ends" => new ExternalFunction((i, j) => this.Value.EndsWith(j[0].ToString()), 1),
                "Lower" => new ExternalFunction((i, j) => this.Value.ToLower(), 0),
                "Upper" => new ExternalFunction((i, j) => this.Value.ToUpper(), 0),
                "Split" => new ExternalFunction((i, j) => this.Value.Split(j[0].ToString()).ToList<object>(), 1),

                Resolver.OVERRIDE_STRINGIFY => new ExternalFunction((i, j) => $"\'{this.Value}\'", 0),
                Resolver.OVERRIDE_INDEX => new ExternalFunction((i, j) => j[0] is double d ? this.Value.ElementAtOrDefault(Program.Interpreter.resolver.GetInt(d)) : null, 0),
                Resolver.OVERRIDE_ITERATOR => new ExternalFunction((i, j) => this.Value.ToCharArray().Select(x => (object)x.ToString()).ToList(), 0),
                Resolver.OPERATOR_ADD => new ExternalFunction((i, j) => this.Value + (j[0] is string s ? s : Program.Interpreter.resolver.Stringify(j[0])), 1),
                Resolver.OPERATOR_MULTIPLY => new ExternalFunction((i, j) => j[0] is double d ? string.Join("", Enumerable.Repeat(this.Value, Program.Interpreter.resolver.GetInt(d))) : null, 1),
                _ => ObjectType.Get(key, TypeID, this),
            };
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal) { ObjectType.Set(key, TypeID, value); }
    }
    public class DoublePropMap : IPropable
    {
        public const string TypeID = "number";
        public readonly double Value;
        public DoublePropMap(double value)
        {
            this.Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                Resolver.OVERRIDE_STRINGIFY => new ExternalFunction((i, j) =>
                {
                    string text = this.Value.ToString();
                    if (text.EndsWith(".0"))
                        text = text[..^2];
                    return text;
                }, 0),
                Resolver.OVERRIDE_ITERATOR => new ExternalFunction((i, j) => Enumerable.Range(0, Program.Interpreter.resolver.GetInt(this.Value)).Select(x => (object)Convert.ToDouble(x)).ToList(), 0),
                Resolver.OVERRIDE_INDEX => new ExternalFunction((i, j) => j[0], 1),
                Resolver.OPERATOR_ADD => new ExternalFunction((i, j) => j[0] is double d ? this.Value + d : null, 1),
                Resolver.OPERATOR_SUBTRACT => new ExternalFunction((i, j) => j[0] is double d ? this.Value - d : null, 1),
                Resolver.OPERATOR_MULTIPLY => new ExternalFunction((i, j) => j[0] is double d ? this.Value * d : null, 1),
                Resolver.OPERATOR_DIVIDE => new ExternalFunction((i, j) => j[0] is double d
                ? d == 0
                    ? 0D
                    : this.Value / d
                : null, 1),
                Resolver.OPERATOR_MODULUS => new ExternalFunction((i, j) => j[0] is double d ? this.Value % d : null, 1),
                Resolver.OPERATOR_GREATER => new ExternalFunction((i, j) => j[0] is double d ? this.Value > d : null, 1),
                Resolver.OPERATOR_LESS => new ExternalFunction((i, j) => j[0] is double d ? this.Value < d : null, 1),
                _ => ObjectType.Get(key, TypeID, this),
            };
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal) { ObjectType.Set(key, TypeID, value); }
    }
    public class BoolPropMap : IPropable
    {
        public const string TypeID = "boolean";
        public readonly bool Value;
        public BoolPropMap(bool value)
        {
            this.Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                "Alt" => !this.Value,
                Resolver.OVERRIDE_STRINGIFY => new ExternalFunction((i, j) => this.Value ? "true" : "false", 0),
                _ => ObjectType.Get(key, TypeID, this),
            };
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal) { ObjectType.Set(key, TypeID, value); }
    }
    public class ListPropMap : IPropable
    {
        public const string TypeID = "sequence";
        public readonly List<object> Value;
        public ListPropMap(List<object> value)
        {
            this.Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                "Length" => (double)this.Value.Count,
                "Contains" => new ExternalFunction((i, j) => this.Value.Contains(j[0]), 1),
                "Add" => new ExternalFunction((i, j) => {
                    this.Value.Add(j[0]); return j[0]; }, 1),
                "Insert" => new ExternalFunction((i, j) => {
                    this.Value.Insert(((double)j[0]).ToInt(), j[1]); return null; }, 1),
                "Remove" => new ExternalFunction((i, j) => this.Value.Remove(j[0]), 1),
                "Delete" => new ExternalFunction((i, j) => {
                    this.Value.RemoveAt(((double)j[0]).ToInt()); return null; }, 1),
                "Index" => new ExternalFunction((i, j) => (double)this.Value.IndexOf(j[0]), 1),
                "Join" => new ExternalFunction((i, j) => string.Join(j[0].ToString(), this.Value), 1),
                Resolver.OPERATOR_ADD => new ExternalFunction((i, j) =>
                {
                    if (j[0] is List<object> l)
                    {
                        List<object> res = new();
                        res.AddRange(this.Value);
                        res.AddRange(l);
                        return res;
                    }
                    return null;
                }, 1),
                Resolver.OVERRIDE_STRINGIFY => new ExternalFunction((i, j) => "[" + string.Join(", ", this.Value.Select(x => Program.Interpreter.resolver.Stringify(x))) + "]", 0),
                Resolver.OVERRIDE_INDEX => new ExternalFunction((i, j) => j[0] is double d ? this.Value.ElementAtOrDefault(Program.Interpreter.resolver.GetInt(d)) : null, 0),
                Resolver.OVERRIDE_MIX => new ExternalFunction((i, j) =>
                {
                    if (j[0] is double d)
                    {
                        int index = Program.Interpreter.resolver.GetInt(d);
                        if (index >= 0 && index < this.Value.Count)
                            return this.Value[index] = j[1];
                        else
                            return null;
                    }
                    else
                        return null;
                }, 0),
                Resolver.OVERRIDE_ITERATOR => new ExternalFunction((i, j) => this.Value, 0),
                _ => ObjectType.Get(key, TypeID, this),
            };
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal) { ObjectType.Set(key, TypeID, value); }
    }
    public class DictPropMap : IPropable
    {
        public const string TypeID = "dictionary";
        public readonly Dictionary<object, object> Value;
        public DictPropMap(Dictionary<object, object> value)
        {
            this.Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                "Length" => (double)this.Value.Count,
                "Contains" => new ExternalFunction((i, j) => this.Value.ContainsKey(j[0]), 1),
                "Add" => new ExternalFunction((i, j) => {
                    this.Value.Add(j[0], j[1]); return j[1]; }, 2),
                "Remove" => new ExternalFunction((i, j) => this.Value.Remove(j[0]), 1),
                "Keys" => this.Value.Keys.ToList(),
                "Values" => this.Value.Values.ToList(),
                Resolver.OVERRIDE_STRINGIFY => new ExternalFunction((i, j) => "{" + string.Join(", ", this.Value.Select(x => Program.Interpreter.resolver.Stringify(x.Key) + ": " + Program.Interpreter.resolver.Stringify(x.Value))) + "}", 0),
                Resolver.OVERRIDE_INDEX => new ExternalFunction((i, j) => this.Value.ContainsKey(j[0]) ? this.Value[j[0]] : null, 0),
                Resolver.OVERRIDE_MIX => new ExternalFunction((i, j) =>
                {
                    this.Value[j[0]] = j[1];
                    return j[1];
                }, 0),
                Resolver.OVERRIDE_ITERATOR => new ExternalFunction((i, j) => this.Value.ToList(), 0),
                _ => ObjectType.Get(key, TypeID, this),
            };
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal) { ObjectType.Set(key, TypeID, value); }
    }
    public class KeyValuePropMap : IPropable
    {
        public readonly string Key;
        public readonly object Value;
        public KeyValuePropMap(string key, object value)
        {
            this.Key = key;
            this.Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                "Key" => this.Key,
                "Value" => this.Value,
                _ => null,
            };
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal) { }
    }
    public class AutoPropMap : IPropable
    {
        public readonly object InternalValue;
        public readonly Type Type;
        public AutoPropMap(object value)
        {
            this.InternalValue = value;
            this.Type = value.GetType();
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            switch (key.Raw)
            {
                case Resolver.OVERRIDE_EQUAL: return new ExternalFunction((i, j) => this.InternalValue.Equals(j[0]), 1);
                case Resolver.OVERRIDE_STRINGIFY: return new ExternalFunction((i, j) => this.InternalValue.ToString(), 0);
                case Resolver.OVERRIDE_ITERATOR: return new ExternalFunction((i, j) => this.InternalValue is List<object> l ? l : null, 0);
            }

            PropertyInfo prop = this.Type.GetProperties().FirstOrDefault(x =>
            {
                return x.Name == key.Raw && !x.IsStatic();
            });
            if (prop != null)
                if (prop.CanRead)
                    return prop.GetValue(this.InternalValue);

            FieldInfo field = this.Type.GetFields().FirstOrDefault(x =>
            {
                return x.Name == key.Raw && !x.IsStatic;
            });
            if (field != null)
                return field.GetValue(this.InternalValue);

            MethodInfo method = this.Type.GetMethods().FirstOrDefault(x =>
            {
                return x.Name == key.Raw && !x.IsStatic;
            });
            if (method != null)
                return new ExternalFunction(method).Bind(this.InternalValue);

            return null;
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        {
            PropertyInfo prop = this.Type.GetProperties().FirstOrDefault(x =>
            {
                //var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                //if (attribute == null)
                //    return false;
                //return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
                return x.Name == key.Raw && !x.IsStatic();
            });
            if (prop != null)
            {
                if (prop.CanWrite)
                    prop.SetValue(this.InternalValue, value);
                return;
            }

            FieldInfo field = this.Type.GetFields().FirstOrDefault(x =>
            {
                return x.Name == key.Raw && !x.IsStatic;
            });
            if (field != null)
            {
                if (!field.Attributes.HasFlag(FieldAttributes.InitOnly))
                    field.SetValue(this.InternalValue, value);
                return;
            }
        }
    }

    public class Prototype : IPropable
    {
        private readonly Dictionary<string, object> Values;
        public Prototype()
        {
            this.Values = new();
        }
        public void Set(string key, object value)
        {
            this.Values[key] = value;
        }

        public object Get(string key)
        {
            return this.Values.ContainsKey(key) ? this.Values[key] : null;
        }

        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
        {
            return VariableType.Normal;
        }

        public object Get(Token key)
        {
            return this.Get(key.Raw);
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        {
            this.Set(key.Raw, value);
        }

        public bool Truthy()
        {
            return this.Values.Count > 0;
        }

        public override string ToString() => $"prototype with {this.Values.Count}";
    }
}
