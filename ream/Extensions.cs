using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Ream
{
    public static class Extensions
    {
        /// <summary>
        /// Splits an IEnumerable
        /// </summary>
        /// <typeparam name="T">The type of collection</typeparam>
        /// <param name="source">The source collection</param>
        /// <param name="separator">The function that controls separation of <paramref name="source"/> when returns true</param>
        /// <returns>A IEnumerable of IEnumerables from <paramref name="source"/></returns>
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
        /// <summary>
        /// Safely convert long to int and prevents overflow
        /// </summary>
        /// <param name="source">The long to convert</param>
        /// <returns>A safely converted interger</returns>
        public static int ToInt(this long source) 
        {
            return (int)(source % int.MaxValue);
        }
        /// <summary>
        /// Repeats a string x amount of times
        /// </summary>
        /// <param name="source">The source string</param>
        /// <param name="multiplier">The amount of times to repeat <paramref name="source"/></param>
        /// <returns><paramref name="source"/> repeated <paramref name="multiplier"/> amount of times</returns>
        public static string Multiply(this string source, int multiplier)
        {
            StringBuilder sb = new(multiplier * source.Length);
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
    } // Unused (is too slow)
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
