using Ream.Lexing;

namespace Ream.Interpreting
{
    public class Pointer : IPropable
    {
        private static Dictionary<int, object> Memory = new();
        private static int nextKey = 0;
        private List<ICallable> hooks = new(16);

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
            this.hooks = null;
        }
        public void Hook(ICallable callable)
        {
            if (this.hooks.Count >= 16)
            {
                if (Program.Interpreter.raiseErrors)
                    throw new RuntimeError("Maximum number of hooks reached for this pointer");
                return;
            }
            this.hooks.Add(callable);
        }
        public object Get()
        {
            if (this.hooks != null)
                foreach (ICallable func in this.hooks)
                    func.Call(Program.Interpreter, new() { 0 });
            
            return Memory.ContainsKey(this.key) ? Memory[this.key] : null;
        }
        public void Set(object obj)
        {
            if (this.hooks != null)
                foreach (ICallable func in this.hooks)
                    func.Call(Program.Interpreter, new() { 1, obj });
            
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
        public bool HasParent => this.Parent != null;
        public Scope Global => this.HasParent ? this.Parent.Global : this;
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
            this.Values[key] = new(value);
            this.VariableData[key] = new(type, this);
        }
        public VariableType AutoDetectType(Token key, VariableType manualType)
        {
            VariableType type = manualType.HasFlag(VariableType.Normal)
                ? this.GetData(key)?.Type ?? manualType : manualType;

            return type;
        }
        public void FreeMemory()
        {
            foreach (Pointer pointer in this.Values.Values)
                pointer.Dispose();
        }
        public void Set(Token key, object value, VariableType manualType = VariableType.Normal)
        {
            string keyName = key.Raw;

            VariableType type = this.AutoDetectType(key, manualType);

            // Variable is not null and is readonly, so it cannot be changed
            if (this.Get(key) != null && this.GetData(key).Type.HasFlag(VariableType.Final))
                return;

            if (type.HasFlag(VariableType.Global))
            {
                if (this.HasParent)
                {
                    this.Global.Set(key, value, type);
                }
                else
                {
                    if (this.Values.ContainsKey(keyName))
                        this.Values[keyName].Set(value);
                    else
                        this.Values[keyName] = new(value);
                    this.VariableData[key.Raw] = new(type, this);
                }
            }
            else if (type.HasFlag(VariableType.Local))
            {
                if (this.Values.ContainsKey(keyName))
                    this.Values[keyName].Set(value);
                else
                    this.Values[keyName] = new(value);
                this.VariableData[key.Raw] = new(type, this);
            }
            else
            {
                // If it exists locally
                if (this.Has(keyName, false))
                {
                    if (this.Values.ContainsKey(keyName))
                        this.Values[keyName].Set(value);
                    else
                        this.Values[keyName] = new(value);
                    this.VariableData[key.Raw] = new(type, this);
                }
                else
                {
                    // If it exists anywhere
                    if (this.Has(keyName, true))
                    {
                        // Recall
                        this.Parent.Set(key, value, type);
                    }
                    else
                    {
                        // We need to create it
                        if (this.Values.ContainsKey(keyName))
                            this.Values[keyName].Set(value);
                        else
                            this.Values[keyName] = new(value);
                        this.VariableData[key.Raw] = new(type, this);
                    }
                }
            }
        }
        public bool Has(string key, bool canCheckParent = true)
        {
            bool has = this.Values.ContainsKey(key);

            // Can't be found locally, has a parent to check and allowed to check parent
            if (!has && this.HasParent && canCheckParent)
                has = this.Parent.Has(key);
            return has;
        }

        public object Get(Token key)
        {
            string keyName = key.Raw;

            // If can be found locally
            if (this.Values.ContainsKey(keyName))
                return this.Values[keyName].Get();

            // If can't be found locally and has a parent to check
            if (this.HasParent) return this.Parent.Get(key);

            //throw new RuntimeError(key, $"Undefined variable '{keyName}'"); // return null instead
            return null;
        }

        public object GetPointer(Token key)
        {
            string keyName = key.Raw;

            // If can be found locally
            if (this.Values.ContainsKey(keyName))
                return this.Values[keyName];

            // If can't be found locally and has a parent to check
            if (this.HasParent) return this.Parent.GetPointer(key);

            //throw new RuntimeError(key, $"Undefined variable '{keyName}'"); // return null instead
            return null;
        }

        public VariableData GetData(Token key)
        {
            string keyName = key.Raw;

            // If can be found locally
            if (this.VariableData.ContainsKey(keyName))
                return this.VariableData[keyName];

            // If can't be found locally and has a parent to check
            if (this.HasParent) return this.Parent.GetData(key);

            //throw new RuntimeError(key, $"Undefined variable '{keyName}'"); // return null instead
            return null;
        }

        public Dictionary<string, object> All()
            => this.Values.ToDictionary(x => x.Key, x => x.Value.Get());
    }

    public class VariableData
    {
        public VariableType Type;
        public Scope Scope;

        public VariableData(VariableType type, Scope scope)
        {
            this.Type = type;
            this.Scope = scope;
        }
    }
}
