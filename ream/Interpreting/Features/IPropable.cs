﻿using System.Reflection;
//using Ream.Interpreting.Features;
using Ream.Lexing;
using Ream.SDK;
using Ream.Tools;

namespace Ream.Interpreting
{
    public interface IPropable
    {
        public object Get(Token key);
        public void Set(Token key, object value, VariableType type = VariableType.Normal);
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal);
    }
    public class StringPropMap : IPropable
    {
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

                Resolver.OVERRIDE_STRINGIFY => new ExternalFunction((i, j) => Value, 0),
                Resolver.OVERRIDE_INDEX => new ExternalFunction((i, j) => j[0] is double d ? Value.ElementAtOrDefault(Program.Interpreter.resolver.GetInt(d)) : null, 0),
                Resolver.OVERRIDE_ITERATOR => new ExternalFunction((i, j) => Value.ToCharArray().Select(x => (object)x.ToString()).ToList(), 0),
                Resolver.OPERATOR_ADD => new ExternalFunction((i, j) => Value + Program.Interpreter.resolver.Stringify(j[0]), 1),
                Resolver.OPERATOR_MULTIPLY => new ExternalFunction((i, j) => j[0] is double d ? string.Join("", Enumerable.Repeat(Value, Program.Interpreter.resolver.GetInt(d))) : null, 1),
                _ => null,
            };
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal) { }
    }
    public class DoublePropMap : IPropable
    {
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
                Resolver.OPERATOR_GREATER => new ExternalFunction((i, j) => j[0] is double d ? Value > d : null, 1),
                Resolver.OPERATOR_LESS => new ExternalFunction((i, j) => j[0] is double d ? Value < d : null, 1),
                _ => null,
            };
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal) { }
    }
    public class BoolPropMap : IPropable
    {
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
                _ => null,
            };
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal) { }
    }
    public class ListPropMap : IPropable
    {
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
                _ => null,
            };
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal) { }
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
                case Resolver.OVERRIDE_ITERATOR: return new ExternalFunction((i, j) => InternalValue is IEnumerable<object> l ? l.ToList() : null, 0);
            }

            PropertyInfo prop = Type.GetProperties().FirstOrDefault(x =>
            {
                var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                if (attribute == null)
                    return false;
                return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
            });
            if (prop != null)
                return prop.GetValue(InternalValue);

            FieldInfo field = Type.GetFields().FirstOrDefault(x =>
            {
                var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                if (attribute == null)
                    return false;
                return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
            });
            if (field != null)
                return field.GetValue(InternalValue);

            return null;
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal)
        {
            PropertyInfo prop = Type.GetProperties().FirstOrDefault(x =>
            {
                var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                if (attribute == null)
                    return false;
                return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
            });
            if (prop != null)
            {
                prop.SetValue(InternalValue, value);
                return;
            }

            FieldInfo field = Type.GetFields().FirstOrDefault(x =>
            {
                var attribute = x.GetCustomAttribute<ExternalVariableAttribute>()?.Apply(x);
                if (attribute == null)
                    return false;
                return attribute.Name == key.Raw && !attribute.Type.HasFlag(VariableType.Static);
            });
            if (field != null)
            {
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
    }
}
