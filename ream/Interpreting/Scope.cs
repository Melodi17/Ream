using Ream.Lexing;
using Ream.SDK;

namespace Ream.Interpreting
{
    public class Pointer : IPropable
    {
        private static Dictionary<int, object> Memory = new();
        private static int nextKey = 0;

        private int key;
        public Pointer(int key)
        {
            this.key = key;
        }
        public Pointer(object value)
        {
            this.key = nextKey++;
            Memory[this.key] = value;
        }
        public void Dispose()
        {
            Memory.Remove(this.key);
        }
        public object Get()
        {
            return Memory.ContainsKey(this.key) ? Memory[this.key] : null;
        }
        public void Set(object obj)
        {
            Memory[this.key] = obj;
        }
        public VariableType AutoDetectType(Token key, VariableType manualType = VariableType.Normal) => manualType;
        public object Get(Token key) => null;
        public void Set(Token key, object value, VariableType type = VariableType.Normal) { }
        public static int GetPointerCount() => Memory.Count;

        public override string ToString() => $"pointer to {this.key}";
    }
    public class Scope
    {
        public bool HasParent => Parent != null;
        public Scope Global => HasParent ? Parent.Global : this;
        public readonly Scope Parent;
        private readonly Dictionary<string, Pointer> Values = new();
        private readonly Dictionary<string, VariableData> VariableData = new();

        public Scope()
        {
            this.Parent = null;
        }
        public Scope(Scope parent)
        {
            this.Parent = parent;
        }

        public void Define(string key, object value, VariableType type = VariableType.Normal)
        {
            // Shouldn't already exist
            Values[key] = new(value);
            VariableData[key] = new(type, this);
        }
        public VariableType AutoDetectType(Token key, VariableType manualType)
        {
            VariableType type = manualType.HasFlag(VariableType.Normal)
                ? GetData(key)?.Type ?? manualType : manualType;

            return type;
        }
        public void FreeMemory()
        {
            foreach (Pointer pointer in Values.Values)
                pointer.Dispose();
        }
        public void Set(Token key, object value, VariableType manualType = VariableType.Normal)
        {
            string keyName = key.Raw;

            VariableType type = AutoDetectType(key, manualType);

            // Variable is not null and is readonly, so it cannot be changed
            if (Get(key) != null && GetData(key).Type.HasFlag(VariableType.Final))
                return;

            if (type.HasFlag(VariableType.Global))
            {
                if (HasParent)
                {
                    Global.Set(key, value, type);
                }
                else
                {
                    if (Values.ContainsKey(keyName)) Values[keyName].Set(value);
                    else Values[keyName] = new(value);
                    VariableData[key.Raw] = new(type, this);
                }
            }
            else if (type.HasFlag(VariableType.Local))
            {
                if (Values.ContainsKey(keyName)) Values[keyName].Set(value); 
                else Values[keyName] = new(value);
                VariableData[key.Raw] = new(type, this);
            }
            else
            {
                // If it exists locally
                if (Has(keyName, false))
                {
                    if (Values.ContainsKey(keyName)) Values[keyName].Set(value);
                    else Values[keyName] = new(value);
                    VariableData[key.Raw] = new(type, this);
                }
                else
                {
                    // If it exists anywhere
                    if (Has(keyName, true))
                    {
                        // Recall
                        Parent.Set(key, value, type);
                    }
                    else
                    {
                        // We need to create it
                        if (Values.ContainsKey(keyName)) Values[keyName].Set(value);
                        else Values[keyName] = new(value);
                        VariableData[key.Raw] = new(type, this);
                    }
                }
            }
        }
        public bool Has(string key, bool canCheckParent = true)
        {
            bool has = Values.ContainsKey(key);

            // Can't be found locally, has a parent to check and allowed to check parent
            if (!has && HasParent && canCheckParent)
                has = Parent.Has(key);
            return has;
        }

        public object Get(Token key)
        {
            string keyName = key.Raw;

            // If can be found locally
            if (Values.ContainsKey(keyName))
                return Values[keyName].Get();

            // If can't be found locally and has a parent to check
            if (HasParent) return Parent.Get(key);

            //throw new RuntimeError(key, $"Undefined variable '{keyName}'"); // return null instead
            return null;
        }

        public object GetPointer(Token key)
        {
            string keyName = key.Raw;

            // If can be found locally
            if (Values.ContainsKey(keyName))
                return Values[keyName];

            // If can't be found locally and has a parent to check
            if (HasParent) return Parent.GetPointer(key);

            //throw new RuntimeError(key, $"Undefined variable '{keyName}'"); // return null instead
            return null;
        }

        public VariableData GetData(Token key)
        {
            string keyName = key.Raw;

            // If can be found locally
            if (VariableData.ContainsKey(keyName))
                return VariableData[keyName];

            // If can't be found locally and has a parent to check
            if (HasParent) return Parent.GetData(key);

            //throw new RuntimeError(key, $"Undefined variable '{keyName}'"); // return null instead
            return null;
        }

        public Dictionary<string, object> All()
            => Values.ToDictionary(x => x.Key, x => x.Value.Get());
    }

    public class VariableData
    {
        public VariableType Type;
        public Scope Scope;

        public VariableData(VariableType type, Scope scope)
        {
            Type = type;
            Scope = scope;
        }
    }
}
