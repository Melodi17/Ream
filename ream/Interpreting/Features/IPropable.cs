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
                _ => null,
            };
        }

        public void Set(Token key, object value, VariableType type = VariableType.Normal) { }
    }
}
