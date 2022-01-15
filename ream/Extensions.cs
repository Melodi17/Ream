using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ream
{
    public static class Extensions
    {
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, Func<T, bool> separator)
        {
            List<List<T>> dest = new();

            List<T> current = new();
            foreach (var chunk in source)
            {
                if (separator(chunk))
                {
                    dest.Add(current);
                    current = new();
                }
                else
                {
                    current.Add(chunk);
                }
            }

            if (current.Any())
                dest.Add(current);

            return dest;
        }
        public static int ToInt(this long l)
        {
            return (int)(l % int.MaxValue);
        }
        public static string Multiply(this string source, int multiplier)
        {
            StringBuilder sb = new StringBuilder(multiplier * source.Length);
            for (long i = 0; i < multiplier; i++)
            {
                sb.Append(source);
            }

            return sb.ToString();
        }
    }
    public static class ObjectComparerUtility
    {
        public static bool ObjectsAreEqual<T>(T obj1, T obj2)
        {
            var obj1Serialized = JsonConvert.SerializeObject(obj1);
            var obj2Serialized = JsonConvert.SerializeObject(obj2);

            return obj1Serialized == obj2Serialized;
        }
    }
    public class Reader<T>
    {
        private readonly T[] OriginalValues;
        private List<T> Values;
        public bool Has => Values.Count > 0;
        public Reader(T[] values)
        {
            OriginalValues = values;
            Reset();
        }

        public T Read()
        {
            if (Has)
            {
                T val = Values.First();
                Values.RemoveAt(0);
                return val;
            }
            return default;
        }

        public T Peek()
        {
            if (Has)
            {
                T val = Values.First();
                return val;
            }
            return default;
        }

        public T[] Rest(int padding = 0)
        {
            T[] vals = Values.SkipLast(padding).ToArray();
            Values.RemoveRange(0, Values.Count - padding);
            return vals;
        }

        public void Reset()
        {
            Values = OriginalValues.ToList();
        }
    }
}
