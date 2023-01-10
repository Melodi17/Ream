using System.Collections;

namespace Ream.Utils;

public class LinqDictionary<TKey, TValue> : IEnumerable<TValue>
{
    private Func<TValue, TKey> _func;
    private List<TValue> _list;

    public LinqDictionary(Func<TValue, TKey> func)
    {
        this._func = func;
        this._list = new();
    }

    public void Add(TValue value)
    {
        this._list.Add(value);
    }

    public void Remove(TKey key)
    {
        this._list.RemoveAll(x => this._func(x).Equals(key));
    }

    public void Clear()
    {
        this._list.Clear();
    }

    public TValue this[TKey key]
    {
        get => this._list.FirstOrDefault(x => this._func(x).Equals(key));
        set
        {
            int index = this._list.FindIndex(x => this._func(x).Equals(key));
            if (index == -1)
                this._list.Add(value);
            else
                this._list[index] = value;
        }
    }
        
    public bool ContainsKey(TKey key)
    {
        return this._list.Any(x => this._func(x).Equals(key));
    }
        
    public IEnumerable<TValue> Values => this._list;
    public IEnumerable<TKey> Keys => this._list.Select(x => this._func(x));
    public IEnumerator<TValue> GetEnumerator()
    {
        return this._list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
