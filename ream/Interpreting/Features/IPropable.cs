using System.Reflection;
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
                "Add" => new ExternalFunction((i, j) => { Value.Add(j[0]); return null; }, 1),
                "Insert" => new ExternalFunction((i, j) => { Value.Insert(((double)j[0]).ToInt(), j[1]); return null; }, 1),
                "Remove" => new ExternalFunction((i, j) => Value.Remove(j[0]), 1),
                "Delete" => new ExternalFunction((i, j) => { Value.RemoveAt(((double)j[0]).ToInt()); return null; }, 1),
                "Index" => new ExternalFunction((i, j) => (double)Value.IndexOf(((double)j[0]).ToInt()), 1),
                "Join" => new ExternalFunction((i, j) => string.Join(j[0].ToString(), Value), 1),
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
}
