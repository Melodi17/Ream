using Ream.Lexing;
using Ream.SDK;

namespace Ream.Interpreting
{
    public class Scope
    {
        public bool HasParent => Parent != null;
        public Scope Global => HasParent ? Parent.Global : this;
        public readonly Scope Parent;
        private readonly Dictionary<string, object> Values = new();
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
            Values[key] = value;
            VariableData[key] = new(type, this);
        }
        public VariableType AutoDetectType(Token key, VariableType manualType)
        {
            VariableType type = manualType.HasFlag(VariableType.Normal)
                ? GetData(key)?.Type ?? manualType : manualType;
            
            return type;
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
                    Values[keyName] = value;
                    VariableData[key.Raw] = new(type, this);
                }
            }
            else if (type.HasFlag(VariableType.Local))
            {
                Values[keyName] = value;
                VariableData[key.Raw] = new(type, this);
            }
            else
            {
                // If it exists locally
                if (Has(keyName, false))
                {
                    Values[keyName] = value;
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
                        Values[keyName] = value;
                        VariableData[key.Raw] = new(type, this);
                    }
                }
            }
        }
        public void Dispose(string key, VariableType manualType = VariableType.Normal)
        {
            VariableType type = AutoDetectType(key, manualType);

            // Variable is not null and is readonly, so it cannot be changed
            if (Get(key) != null && GetData(key).Type.HasFlag(VariableType.Final))
                return;

            if (type.HasFlag(VariableType.Global))
            {
                if (HasParent)
                {
                    Global.Dispose(key, type);
                }
                else
                {
                    Values.Remove(key);
                    VariableData.Remove(key);
                }
            }
            else if (type.HasFlag(VariableType.Local))
            {
                Values.Remove(key);
                VariableData.Remove(key);
            }
            else
            {
                // If it exists locally
                if (Has(key, false))
                {
                    Values.Remove(key);
                    VariableData.Remove(key);
                }
                else
                {
                    // If it exists anywhere
                    if (Has(key, true))
                    {
                        // Recall
                        Parent.Dispose(key, type);
                    }
                    else
                    {
                        // We need to report this
                        throw new RuntimeError("", "Specified key was not present, unable to be disposed");
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
                return Values[keyName];

            // If can't be found locally and has a parent to check
            if (HasParent) return Parent.Get(key);

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

        //public void SetAt(int dist, Token name, object value)
        //{
        //    Ancestor(dist).Values[name.Raw] = value;
        //}
        //public object GetAt(int dist, string name)
        //{
        //    return Ancestor(dist).Values[name];
        //}

        //public Scope Ancestor(int dist)
        //{
        //    Scope scope = this;
        //    for (int i = 0; i < dist; i++)
        //    {
        //        scope = scope.Parent;
        //    }

        //    return scope;
        //}

        public Dictionary<string, object> All()
            => Values;
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
