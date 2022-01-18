using Ream.Lexing;

namespace Ream.Interpreting
{
    public class Scope
    {
        public bool HasParent => Parent != null;
        public Scope Global => HasParent ? Parent.Global : this;
        public readonly Scope Parent;
        private readonly Dictionary<string, object> Values = new();

        public Scope()
        {
            this.Parent = null;
        }
        public Scope(Scope parent)
        {
            this.Parent = parent;
        }

        public void Define(string key, object value)
        {
            Values.Add(key, value);
        }

        public void Set(Token key, object value, bool globalCreate = false)
        {
            string keyName = key.Raw;

            // If it exists locally
            if (Has(keyName, false))
                Values[keyName] = value;
            else
                // If it exists anywhere
                if (Has(keyName, true))
                    Parent.Set(key, value);
                else
                    // We need to create it
                    // Create it globally
                    if (globalCreate)
                        Global.Set(key, value);
                    else
                        // Create it locally
                        Values[keyName] = value;
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
    }
}
