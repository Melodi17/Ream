using System.Reflection;
//using Ream.Interpreting.Features;
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
                Extensions.Add(id, new Dictionary<string, object> { { key.Raw, value } });
        }
        public ObjectType(string id)
        {
            this.ID = id;
            if (!Extensions.ContainsKey(id))
                Extensions.Add(id, new Dictionary<string, object>());
        }

        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal) => manualType;
        public object Get(Token key)
        {
            return Extensions[ID].ContainsKey(key.Raw) ? Extensions[ID][key.Raw] : null;
        }
        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        {
            Extensions[ID][key.Raw] = value;
        }
    }
    public class StringPropMap : IPropable
    {
        public const string TypeID = "string";
        public readonly string Value;
        public StringPropMap(string value)
        {
            Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                "Length" => (double)Value.Length,
                "Contains" => new ExternalFunction((i, j) => Value.Contains(j[0].ToString()), 1),
                "Replace" => new ExternalFunction((i, j) => Value.Replace(j[0].ToString(), j[1].ToString()), 2),
                "Starts" => new ExternalFunction((i, j) => Value.StartsWith(j[0].ToString()), 1),
                "Ends" => new ExternalFunction((i, j) => Value.EndsWith(j[0].ToString()), 1),
                "Lower" => new ExternalFunction((i, j) => Value.ToLower(), 0),
                "Upper" => new ExternalFunction((i, j) => Value.ToUpper(), 0),
                "Split" => new ExternalFunction((i, j) => Value.Split(j[0].ToString()).ToList<object>(), 1),

                Resolver.OVERRIDE_STRINGIFY => new ExternalFunction((i, j) => $"\'{Value}\'", 0),
                Resolver.OVERRIDE_INDEX => new ExternalFunction((i, j) => j[0] is double d ? Value.ElementAtOrDefault(Program.Interpreter.resolver.GetInt(d)) : null, 0),
                Resolver.OVERRIDE_ITERATOR => new ExternalFunction((i, j) => Value.ToCharArray().Select(x => (object)x.ToString()).ToList(), 0),
                Resolver.OPERATOR_ADD => new ExternalFunction((i, j) => Value + (j[0] is string s ? s : Program.Interpreter.resolver.Stringify(j[0])), 1),
                Resolver.OPERATOR_MULTIPLY => new ExternalFunction((i, j) => j[0] is double d ? string.Join("", Enumerable.Repeat(Value, Program.Interpreter.resolver.GetInt(d))) : null, 1),
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
            Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                Resolver.OVERRIDE_STRINGIFY => new ExternalFunction((i, j) =>
                {
                    string text = Value.ToString();
                    if (text.EndsWith(".0"))
                        text = text[..^2];
                    return text;
                }, 0),
                Resolver.OVERRIDE_ITERATOR => new ExternalFunction((i, j) => Enumerable.Range(0, Program.Interpreter.resolver.GetInt(Value)).Select(x => (object)Convert.ToDouble(x)).ToList(), 0),
                Resolver.OVERRIDE_INDEX => new ExternalFunction((i, j) => j[0], 1),
                Resolver.OPERATOR_ADD => new ExternalFunction((i, j) => j[0] is double d ? Value + d : null, 1),
                Resolver.OPERATOR_SUBTRACT => new ExternalFunction((i, j) => j[0] is double d ? Value - d : null, 1),
                Resolver.OPERATOR_MULTIPLY => new ExternalFunction((i, j) => j[0] is double d ? Value * d : null, 1),
                Resolver.OPERATOR_DIVIDE => new ExternalFunction((i, j) => j[0] is double d
                ? d == 0
                    ? 0D
                    : Value / d
                : null, 1),
                Resolver.OPERATOR_MODULUS => new ExternalFunction((i, j) => j[0] is double d ? Value % d : null, 1),
                Resolver.OPERATOR_GREATER => new ExternalFunction((i, j) => j[0] is double d ? Value > d : null, 1),
                Resolver.OPERATOR_LESS => new ExternalFunction((i, j) => j[0] is double d ? Value < d : null, 1),
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
            Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                "Alt" => !Value,
                Resolver.OVERRIDE_STRINGIFY => new ExternalFunction((i, j) => Value ? "true" : "false", 0),
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
            Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                "Length" => (double)Value.Count,
                "Contains" => new ExternalFunction((i, j) => Value.Contains(j[0]), 1),
                "Add" => new ExternalFunction((i, j) => { Value.Add(j[0]); return j[0]; }, 1),
                "Insert" => new ExternalFunction((i, j) => { Value.Insert(((double)j[0]).ToInt(), j[1]); return null; }, 1),
                "Remove" => new ExternalFunction((i, j) => Value.Remove(j[0]), 1),
                "Delete" => new ExternalFunction((i, j) => { Value.RemoveAt(((double)j[0]).ToInt()); return null; }, 1),
                "Index" => new ExternalFunction((i, j) => (double)Value.IndexOf(j[0]), 1),
                "Join" => new ExternalFunction((i, j) => string.Join(j[0].ToString(), Value), 1),
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
                Resolver.OVERRIDE_STRINGIFY => new ExternalFunction((i, j) => "[" + string.Join(", ", Value.Select(x => Program.Interpreter.resolver.Stringify(x))) + "]", 0),
                Resolver.OVERRIDE_INDEX => new ExternalFunction((i, j) => j[0] is double d ? Value.ElementAtOrDefault(Program.Interpreter.resolver.GetInt(d)) : null, 0),
                Resolver.OVERRIDE_MIX => new ExternalFunction((i, j) =>
                {
                    if (j[0] is double d)
                    {
                        int index = Program.Interpreter.resolver.GetInt(d);
                        if (index >= 0 && index < Value.Count)
                            return Value[index] = j[1];
                        else
                            return null;
                    }
                    else
                        return null;
                }, 0),
                Resolver.OVERRIDE_ITERATOR => new ExternalFunction((i, j) => Value, 0),
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
            Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                "Length" => (double)Value.Count,
                "Contains" => new ExternalFunction((i, j) => Value.ContainsKey(j[0]), 1),
                "Add" => new ExternalFunction((i, j) => { Value.Add(j[0], j[1]); return j[1]; }, 2),
                "Remove" => new ExternalFunction((i, j) => Value.Remove(j[0]), 1),
                "Keys" => Value.Keys.ToList(),
                "Values" => Value.Values.ToList(),
                Resolver.OVERRIDE_STRINGIFY => new ExternalFunction((i, j) => "{" + string.Join(", ", Value.Select(x => Program.Interpreter.resolver.Stringify(x.Key) + ": " + Program.Interpreter.resolver.Stringify(x.Value))) + "}", 0),
                Resolver.OVERRIDE_INDEX => new ExternalFunction((i, j) => Value.ContainsKey(j[0]) ? Value[j[0]] : null, 0),
                Resolver.OVERRIDE_MIX => new ExternalFunction((i, j) =>
                {
                    Value[j[0]] = j[1];
                    return j[1];
                }, 0),
                Resolver.OVERRIDE_ITERATOR => new ExternalFunction((i, j) => Value.ToList(), 0),
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
            Key = key;
            Value = value;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            return key.Raw switch
            {
                "Key" => Key,
                "Value" => Value,
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
            InternalValue = value;
            Type = value.GetType();
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
            => manualType;

        public object Get(Token key)
        {
            switch (key.Raw)
            {
                case Resolver.OVERRIDE_EQUAL: return new ExternalFunction((i, j) => InternalValue.Equals(j[0]), 1);
                case Resolver.OVERRIDE_STRINGIFY: return new ExternalFunction((i, j) => InternalValue.ToString(), 0);
                case Resolver.OVERRIDE_ITERATOR: return new ExternalFunction((i, j) => InternalValue is List<object> l ? l : null, 0);
            }

            PropertyInfo prop = Type.GetProperties().FirstOrDefault(x =>
            {
                return x.Name == key.Raw && !x.IsStatic();
            });
            if (prop != null)
                if (prop.CanRead)
                    return prop.GetValue(InternalValue);

            FieldInfo field = Type.GetFields().FirstOrDefault(x =>
            {
                return x.Name == key.Raw && !x.IsStatic;
            });
            if (field != null)
                return field.GetValue(InternalValue);

            return null;
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        {
            PropertyInfo prop = Type.GetProperties().FirstOrDefault(x =>
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
                    prop.SetValue(InternalValue, value);
                return;
            }

            FieldInfo field = Type.GetFields().FirstOrDefault(x =>
            {
                return x.Name == key.Raw && !x.IsStatic;
            });
            if (field != null)
            {
                if (!field.Attributes.HasFlag(FieldAttributes.InitOnly))
                    field.SetValue(InternalValue, value);
                return;
            }
        }
    }

    public class Prototype : IPropable
    {
        private readonly Dictionary<string, object> Values;
        public Prototype()
        {
            Values = new Dictionary<string, object>();
        }
        public void Set(string key, object value)
        {
            Values[key] = value;
        }

        public object Get(string key)
        {
            return Values.ContainsKey(key) ? Values[key] : null;
        }

        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal)
        {
            return VariableType.Normal;
        }

        public object Get(Token key)
        {
            return Get(key.Raw);
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        {
            Set(key.Raw, value);
        }

        public override string ToString() => $"prototype with {Values.Count}";
    }
}
